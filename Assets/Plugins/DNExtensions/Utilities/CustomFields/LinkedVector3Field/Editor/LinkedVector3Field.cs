#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// A reusable editor utility that draws a Vector3 field with copy, paste, reset buttons,
    /// an optional proportional lock, and a context menu for per-axis copying.
    /// </summary>
    public static class LinkedVector3Field
    {
        private const float ButtonWidth = 20f;
        private const float LockWidth   = 20f;
        private const float ButtonsTotal = ButtonWidth * 3f;
        private const float Spacing  = 5f;

        /// <summary>
        /// Draws a Vector3 row with optional proportional lock and C/P/R buttons.
        /// </summary>
        /// <param name="label">Label displayed on the left.</param>
        /// <param name="value">Current Vector3 value.</param>
        /// <param name="resetValue">Value used when reset is pressed.</param>
        /// <param name="showLock">Whether to show the proportional lock toggle.</param>
        /// <param name="locked">Current lock state. Passed by ref — updated when toggled.</param>
        /// <param name="extraContextItems">Optional extra items appended to the right-click context menu.</param>
        /// <returns>The new Vector3 value after any edits.</returns>
        public static Vector3 Draw(string label, Vector3 value, Vector3 resetValue, bool showLock, ref bool locked, Action<GenericMenu> extraContextItems = null)
        {
            Vector3 newValue = value;
            float lockW = showLock ? LockWidth : 0f;

            if (EditorGUIUtility.wideMode)
            {
                // Single Rect row — no BeginHorizontal so no extra spacing
                Rect rowRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                newValue = DrawRow(rowRect, label, value, resetValue, showLock, lockW, ref locked, extraContextItems);
            }
            else
            {
                // Row 1: label | lock
                EditorGUILayout.BeginHorizontal();
                float labelW = EditorGUIUtility.labelWidth - lockW;
                Rect labelRect = GUILayoutUtility.GetRect(new GUIContent(label), GUI.skin.label, GUILayout.Width(labelW));
                HandleContextClick(labelRect, value, label, extraContextItems);
                GUI.Label(labelRect, label);

                if (showLock)
                {
                    Rect lockRect = GUILayoutUtility.GetRect(lockW, EditorGUIUtility.singleLineHeight, GUILayout.Width(lockW));
                    EditorGUIUtility.AddCursorRect(lockRect, MouseCursor.Link);
                    locked = GUI.Toggle(lockRect, locked,
                        EditorGUIUtility.IconContent(locked ? "Linked" : "Unlinked"),
                        EditorStyles.label);
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                // Row 2: field | buttons (force wideMode so Vector3Field renders single-line)
                Rect fieldRow = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                Rect fieldRect   = new Rect(fieldRow.x, fieldRow.y, fieldRow.width - ButtonsTotal - Spacing, fieldRow.height);
                Rect buttonsRect = new Rect(fieldRect.xMax + Spacing, fieldRow.y, ButtonsTotal, fieldRow.height);

                HandleContextClick(fieldRect, value, label, extraContextItems);

                bool prevWide = EditorGUIUtility.wideMode;
                EditorGUIUtility.wideMode = true;
                EditorGUI.BeginChangeCheck();
                int oldIndent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
                newValue = EditorGUI.Vector3Field(fieldRect, GUIContent.none, value);
                EditorGUI.indentLevel = oldIndent;
                EditorGUIUtility.wideMode = prevWide;
                if (EditorGUI.EndChangeCheck() && showLock && locked)
                    newValue = ApplyLock(value, newValue, value);

                DrawButtonsRect(buttonsRect, value, resetValue, label,
                    onCopy:  () => CopyToClipboard(value),
                    onPaste: pasted => newValue = pasted,
                    onReset: () => newValue = resetValue);
            }

            return newValue;
        }

        /// <summary>
        /// Layout-free version for use inside custom Rect-based drawers.
        /// Always renders as a single line. Returns height consumed.
        /// </summary>
        public static Vector3 Draw(Rect position, string label, Vector3 value, Vector3 resetValue, bool showLock, ref bool locked, out float heightUsed, Action<GenericMenu> extraContextItems = null)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            Rect rowRect = new Rect(position.x, position.y, position.width, lineHeight);
            heightUsed = lineHeight;
            bool dummy = locked;
            Vector3 result = DrawRow(rowRect, label, value, resetValue, showLock, showLock ? LockWidth : 0f, ref dummy, extraContextItems);
            locked = dummy;
            return result;
        }

        /// <summary>Applies proportional scaling based on which axis changed.</summary>
        public static Vector3 ApplyLock(Vector3 previous, Vector3 next, Vector3 reference)
        {
            if (reference == Vector3.zero) return next;

            int changedAxis = -1;
            if (!Mathf.Approximately(next.x, previous.x)) changedAxis = 0;
            else if (!Mathf.Approximately(next.y, previous.y)) changedAxis = 1;
            else if (!Mathf.Approximately(next.z, previous.z)) changedAxis = 2;

            if (changedAxis == -1) return next;

            float ratio = changedAxis switch
            {
                0 when !Mathf.Approximately(reference.x, 0f) => next.x / reference.x,
                1 when !Mathf.Approximately(reference.y, 0f) => next.y / reference.y,
                2 when !Mathf.Approximately(reference.z, 0f) => next.z / reference.z,
                _ => 1f
            };

            return reference * ratio;
        }

        private static Vector3 DrawRow(Rect rowRect, string label, Vector3 value, Vector3 resetValue, bool showLock, float lockW, ref bool locked, Action<GenericMenu> extraContextItems)
        {
            Vector3 newValue = value;
            float labelW   = EditorGUIUtility.labelWidth - lockW;
            float fieldW   = rowRect.width - EditorGUIUtility.labelWidth - ButtonsTotal - Spacing;

            Rect labelRect   = new Rect(rowRect.x, rowRect.y, labelW, rowRect.height);
            Rect lockRect    = new Rect(labelRect.xMax, rowRect.y, lockW, rowRect.height);
            Rect fieldRect   = new Rect(rowRect.x + EditorGUIUtility.labelWidth, rowRect.y, fieldW, rowRect.height);
            Rect buttonsRect = new Rect(fieldRect.xMax + Spacing, rowRect.y, ButtonsTotal, rowRect.height);

            HandleContextClick(labelRect, value, label, extraContextItems);
            EditorGUI.LabelField(labelRect, label);

            if (showLock)
            {
                EditorGUIUtility.AddCursorRect(lockRect, MouseCursor.Link);
                locked = GUI.Toggle(lockRect, locked,
                    EditorGUIUtility.IconContent(locked ? "Linked" : "Unlinked"),
                    EditorStyles.label);
            }

            HandleContextClick(fieldRect, value, label, extraContextItems);

            bool prevWide = EditorGUIUtility.wideMode;
            EditorGUIUtility.wideMode = true;
            EditorGUI.BeginChangeCheck();
            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            newValue = EditorGUI.Vector3Field(fieldRect, GUIContent.none, value);
            EditorGUI.indentLevel = oldIndent;
            EditorGUIUtility.wideMode = prevWide;
            if (EditorGUI.EndChangeCheck() && showLock && locked)
                newValue = ApplyLock(value, newValue, value);

            DrawButtonsRect(buttonsRect, value, resetValue, label,
                onCopy:  () => CopyToClipboard(value),
                onPaste: pasted => newValue = pasted,
                onReset: () => newValue = resetValue);

            return newValue;
        }

        private static void DrawButtonsRect(Rect rect, Vector3 current, Vector3 resetValue, string label,
            Action onCopy, Action<Vector3> onPaste, Action onReset)
        {
            Rect copyRect  = new Rect(rect.x,                rect.y, ButtonWidth, rect.height);
            Rect pasteRect = new Rect(rect.x + ButtonWidth,  rect.y, ButtonWidth, rect.height);
            Rect resetRect = new Rect(rect.x + ButtonWidth * 2f, rect.y, ButtonWidth, rect.height);

            if (GUI.Button(copyRect, new GUIContent("C", "Copy"), EditorStyles.miniButtonLeft))
                onCopy?.Invoke();

            EditorGUI.BeginDisabledGroup(!CanPaste());
            if (GUI.Button(pasteRect, new GUIContent("P", "Paste"), EditorStyles.miniButtonMid))
                if (TryParseClipboard(out Vector3 parsed)) onPaste?.Invoke(parsed);
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(current == resetValue);
            if (GUI.Button(resetRect, new GUIContent("R", $"Reset {label.ToLower()}"), EditorStyles.miniButtonRight))
                onReset?.Invoke();
            EditorGUI.EndDisabledGroup();
        }

        private static void HandleContextClick(Rect rect, Vector3 value, string label, Action<GenericMenu> extraContextItems)
        {
            if (Event.current.type != EventType.ContextClick) return;
            if (!rect.Contains(Event.current.mousePosition)) return;

            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Copy"), false, () => CopyToClipboard(value));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Copy X"), false, () => EditorGUIUtility.systemCopyBuffer = value.x.ToString());
            menu.AddItem(new GUIContent("Copy Y"), false, () => EditorGUIUtility.systemCopyBuffer = value.y.ToString());
            menu.AddItem(new GUIContent("Copy Z"), false, () => EditorGUIUtility.systemCopyBuffer = value.z.ToString());

            if (extraContextItems != null)
            {
                menu.AddSeparator("");
                extraContextItems(menu);
            }

            menu.ShowAsContext();
            Event.current.Use();
        }

        private static void CopyToClipboard(Vector3 value) =>
            EditorGUIUtility.systemCopyBuffer = $"{value.x},{value.y},{value.z}";

        private static bool CanPaste() => TryParseClipboard(out _);

        private static bool TryParseClipboard(out Vector3 result)
        {
            result = Vector3.zero;
            string clipboard = EditorGUIUtility.systemCopyBuffer;
            if (string.IsNullOrEmpty(clipboard)) return false;

            string[] parts = clipboard.Split(',');
            if (parts.Length != 3) return false;

            if (float.TryParse(parts[0], out float x) &&
                float.TryParse(parts[1], out float y) &&
                float.TryParse(parts[2], out float z))
            {
                result = new Vector3(x, y, z);
                return true;
            }

            return false;
        }
    }
}
#endif