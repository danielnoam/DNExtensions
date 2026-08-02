using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine.UIElements;

namespace DNExtensions.HelpfulEditor.Inspector
{
    /// <summary>
    /// Finds the parts of the Inspector's UI Toolkit tree that the suite needs: the list of editor
    /// elements, and the Editor behind any element in it.
    ///
    /// The element types involved are internal and have been renamed across versions, so the Editor
    /// is located by shape — any member on the element that hands one back — rather than by a
    /// hardcoded field name.
    /// </summary>
    internal static class InspectorElementLookup
    {
        private static readonly Dictionary<Type, MemberInfo> EditorMembers = new Dictionary<Type, MemberInfo>();

        /// <summary>Walks up from an element until an ancestor turns out to own an Editor.</summary>
        public static Editor FindEditorInAncestors(VisualElement element)
        {
            for (VisualElement current = element; current != null; current = current.parent)
            {
                Editor editor = GetEditor(current);
                if (editor) return editor;
            }

            return null;
        }

        public static Editor GetEditor(VisualElement element)
        {
            if (element == null) return null;

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
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

            for (Type current = type; current != null && current != typeof(VisualElement); current = current.BaseType)
            {
                foreach (PropertyInfo property in current.GetProperties(flags))
                {
                    if (typeof(Editor).IsAssignableFrom(property.PropertyType) && property.CanRead) return property;
                }

                foreach (FieldInfo field in current.GetFields(flags))
                {
                    if (typeof(Editor).IsAssignableFrom(field.FieldType)) return field;
                }
            }

            return null;
        }
    }
}
