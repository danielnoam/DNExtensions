using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// Draws an inspector's top-level fields with [Foldout]/[EndFoldout] groups and
    /// [ShowIf]/[HideIf]/[EnableIf]/[DisableIf] blocks closed by [EndIf].
    /// Types that use none of these attributes fall back to the default inspector.
    /// </summary>
    public static class InspectorGroupDrawer
    {
        private const BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        private static readonly Dictionary<Type, bool> UsesGroupingCache = new();
        private static GUIStyle _foldoutStyle;

        private static GUIStyle FoldoutStyle => _foldoutStyle ??= new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold };

        private readonly struct Entry
        {
            public readonly SerializedProperty Property;
            public readonly FieldInfo Field;

            public Entry(SerializedProperty property, FieldInfo field)
            {
                Property = property;
                Field = field;
            }
        }

        public static void Draw(Editor editor)
        {
            var targetType = editor.target ? editor.target.GetType() : null;
            if (targetType == null || !UsesGrouping(targetType))
            {
                editor.DrawDefaultInspector();
                return;
            }

            var serializedObject = editor.serializedObject;
            serializedObject.UpdateIfRequiredOrScript();

            var entries = CollectEntries(serializedObject);
            int lastEndIf = -1;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Field?.GetCustomAttribute<EndIfAttribute>() != null) lastEndIf = i;
            }

            int baseIndent = EditorGUI.indentLevel;
            var openPath = new List<string>();
            int visibleDepth = 0;
            IfAttribute visibilityBlock = null;
            IfAttribute enableBlock = null;
            SerializedProperty visibilityOwner = null;
            SerializedProperty enableOwner = null;

            for (int i = 0; i < entries.Count; i++)
            {
                var property = entries[i].Property;
                var field = entries[i].Field;

                if (field != null)
                {
                    if (field.GetCustomAttribute<EndIfAttribute>() != null)
                    {
                        visibilityBlock = null;
                        enableBlock = null;
                    }

                    var foldout = field.GetCustomAttribute<FoldoutAttribute>();
                    if (foldout != null)
                        visibleDepth = ChangePath(openPath, SplitPath(foldout.Name), visibleDepth, baseIndent, targetType);
                    else if (field.GetCustomAttribute<EndFoldoutAttribute>() != null)
                        visibleDepth = ChangePath(openPath, Array.Empty<string>(), visibleDepth, baseIndent, targetType);
                }

                var ownConditions = field != null ? field.GetCustomAttributes<IfAttribute>(true) : Array.Empty<IfAttribute>();
                if (i < lastEndIf)
                {
                    foreach (var condition in ownConditions)
                    {
                        if (IsVisibilityCondition(condition))
                        {
                            visibilityBlock = condition;
                            visibilityOwner = property;
                        }
                        else
                        {
                            enableBlock = condition;
                            enableOwner = property;
                        }
                    }
                }

                if (visibleDepth < openPath.Count) continue;

                bool hidden = IsHidden(visibilityBlock, visibilityOwner);
                bool disabled = property.propertyPath == "m_Script" || IsDisabled(enableBlock, enableOwner);
                foreach (var condition in ownConditions)
                {
                    hidden |= IsHidden(condition, property);
                    disabled |= IsDisabled(condition, property);
                }

                if (hidden) continue;

                EditorGUI.indentLevel = baseIndent + openPath.Count;
                using (new EditorGUI.DisabledScope(disabled))
                {
                    EditorGUILayout.PropertyField(property, true);
                }
            }

            EditorGUI.indentLevel = baseIndent;
            serializedObject.ApplyModifiedProperties();
        }

        private static List<Entry> CollectEntries(SerializedObject serializedObject)
        {
            var entries = new List<Entry>();
            var iterator = serializedObject.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                var property = iterator.Copy();
                entries.Add(new Entry(property, ConditionalAttributeEvaluator.GetFieldInfo(property)));
            }

            return entries;
        }

        /// <summary>
        /// Closes and opens foldout headers so the open path matches <paramref name="newPath"/>.
        /// Returns how many levels of the new path are expanded and drawn.
        /// </summary>
        private static int ChangePath(List<string> openPath, IReadOnlyList<string> newPath, int visibleDepth, int baseIndent, Type targetType)
        {
            int shared = 0;
            while (shared < openPath.Count && shared < newPath.Count && openPath[shared] == newPath[shared]) shared++;

            if (shared == openPath.Count && shared == newPath.Count) return visibleDepth;

            openPath.RemoveRange(shared, openPath.Count - shared);
            visibleDepth = Mathf.Min(visibleDepth, shared);

            for (int depth = shared; depth < newPath.Count; depth++)
            {
                openPath.Add(newPath[depth]);
                if (visibleDepth < depth) continue;

                string key = $"DNExtensions.Foldout.{targetType.FullName}.{string.Join("/", openPath)}";
                bool expanded = SessionState.GetBool(key, false);

                EditorGUI.indentLevel = baseIndent + depth;
                bool newExpanded = EditorGUILayout.Foldout(expanded, newPath[depth], true, FoldoutStyle);
                if (newExpanded != expanded) SessionState.SetBool(key, newExpanded);

                if (newExpanded) visibleDepth = depth + 1;
            }

            return visibleDepth;
        }

        private static string[] SplitPath(string path)
        {
            return string.IsNullOrEmpty(path)
                ? Array.Empty<string>()
                : path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static bool IsVisibilityCondition(IfAttribute condition)
        {
            return condition is ShowIfAttribute || condition is HideIfAttribute;
        }

        private static bool IsHidden(IfAttribute condition, SerializedProperty owner)
        {
            if (condition == null || owner == null) return false;

            bool result = ConditionalAttributeEvaluator.Evaluate(condition.VariableName, condition.VariableValue, owner);
            return condition switch
            {
                ShowIfAttribute => !result,
                HideIfAttribute => result,
                _ => false
            };
        }

        private static bool IsDisabled(IfAttribute condition, SerializedProperty owner)
        {
            if (condition == null || owner == null) return false;

            bool result = ConditionalAttributeEvaluator.Evaluate(condition.VariableName, condition.VariableValue, owner);
            return condition switch
            {
                EnableIfAttribute => !result,
                DisableIfAttribute => result,
                _ => false
            };
        }

        private static bool UsesGrouping(Type type)
        {
            if (UsesGroupingCache.TryGetValue(type, out bool uses)) return uses;

            uses = false;
            for (var current = type; current != null && !uses; current = current.BaseType)
            {
                foreach (var field in current.GetFields(FieldFlags))
                {
                    if (!field.IsDefined(typeof(FoldoutAttribute), false) &&
                        !field.IsDefined(typeof(EndFoldoutAttribute), false) &&
                        !field.IsDefined(typeof(EndIfAttribute), false) &&
                        !field.IsDefined(typeof(IfAttribute), true)) continue;

                    uses = true;
                    break;
                }
            }

            UsesGroupingCache[type] = uses;
            return uses;
        }
    }
}
