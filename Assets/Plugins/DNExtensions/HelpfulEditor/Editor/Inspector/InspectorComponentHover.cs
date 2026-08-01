using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DNExtensions.HelpfulEditor.Inspector
{
    /// <summary>
    /// Tracks which component the cursor is over in the Inspector body — its header or its fields —
    /// so the hover keybinds act where you are looking rather than only on the header bar buttons.
    ///
    /// Unity exposes no callback for the inline component titlebars, and the attribute that would
    /// register one (EditorHeaderItem) is internal. The Inspector's editor list is UI Toolkit
    /// though, so each component's editor lives in its own VisualElement: the element under the
    /// pointer is picked and walked up until one of its ancestors turns out to own an Editor.
    /// </summary>
    [InitializeOnLoad]
    internal static class InspectorComponentHover
    {
        private const double ScanInterval = 0.5;

        private static readonly List<VisualElement> Registered = new List<VisualElement>();
        private static readonly Dictionary<Type, MemberInfo> EditorMembers = new Dictionary<Type, MemberInfo>();

        private static double _lastScan;

        public static Component HoveredComponent { get; private set; }

        static InspectorComponentHover()
        {
            EditorApplication.update -= Scan;
            EditorApplication.update += Scan;
        }

        /// <summary>Inspector windows come and go, so the callback is re-attached to any root that lacks it.</summary>
        private static void Scan()
        {
            if (!HelpfulEditorSettings.Inspector.moduleEnabled) return;
            if (EditorApplication.timeSinceStartup - _lastScan < ScanInterval) return;

            _lastScan = EditorApplication.timeSinceStartup;
            Registered.RemoveAll(element => element?.panel == null);

            // PointerLeave is not guaranteed — a window closing or the cursor jumping straight out
            // can skip it — so a stale hover is dropped here as a backstop. Only on positive
            // evidence though: mouseOverWindow is null whenever the editor cannot say, and treating
            // that as "not over the Inspector" would clear the hover between pointer moves.
            if (HoveredComponent && EditorWindow.mouseOverWindow && !HelpfulEditorWindows.MouseOverInspector)
            {
                HoveredComponent = null;
            }

            Type inspectorType = typeof(EditorWindow).Assembly.GetType("UnityEditor.PropertyEditor")
                                 ?? typeof(EditorWindow).Assembly.GetType("UnityEditor.InspectorWindow");
            if (inspectorType == null) return;

            foreach (UnityEngine.Object candidate in Resources.FindObjectsOfTypeAll(inspectorType))
            {
                if (candidate is not EditorWindow window) continue;

                VisualElement root = window.rootVisualElement;
                if (root == null || Registered.Contains(root)) continue;

                root.RegisterCallback<PointerMoveEvent>(OnPointerMove);
                root.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
                Registered.Add(root);
            }
        }

        private static void OnPointerLeave(PointerLeaveEvent evt) => HoveredComponent = null;

        private static void OnPointerMove(PointerMoveEvent evt)
        {
            if (evt.currentTarget is not VisualElement root || root.panel == null) return;

            HoveredComponent = FindComponentAt(root.panel, evt.position);
        }

        private static Component FindComponentAt(IPanel panel, Vector2 panelPosition)
        {
            VisualElement picked = panel.Pick(panelPosition);

            for (VisualElement element = picked; element != null; element = element.parent)
            {
                Editor editor = GetEditor(element);
                if (editor && editor.target is Component component) return component;
            }

            return null;
        }

        /// <summary>
        /// The element type that owns the Editor is internal and has been renamed across versions,
        /// so it is found by shape — any member on the element that hands back an Editor — rather
        /// than by name. The lookup is cached per element type.
        /// </summary>
        private static Editor GetEditor(VisualElement element)
        {
            Type type = element.GetType();

            if (!EditorMembers.TryGetValue(type, out MemberInfo member))
            {
                member = FindEditorMember(type);
                EditorMembers[type] = member;
            }

            if (member == null) return null;

            try
            {
                return member switch
                {
                    PropertyInfo property => property.GetValue(element) as Editor,
                    FieldInfo field => field.GetValue(element) as Editor,
                    _ => null
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static MemberInfo FindEditorMember(Type type)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            for (Type current = type; current != null && current != typeof(VisualElement); current = current.BaseType)
            {
                foreach (PropertyInfo property in current.GetProperties(flags | BindingFlags.DeclaredOnly))
                {
                    if (typeof(Editor).IsAssignableFrom(property.PropertyType) && property.CanRead) return property;
                }

                foreach (FieldInfo field in current.GetFields(flags | BindingFlags.DeclaredOnly))
                {
                    if (typeof(Editor).IsAssignableFrom(field.FieldType)) return field;
                }
            }

            return null;
        }
    }
}
