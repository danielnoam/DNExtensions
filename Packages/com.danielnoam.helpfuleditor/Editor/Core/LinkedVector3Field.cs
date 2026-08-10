using System;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.HelpfulEditor
{
    /// <summary>
    /// Draws a Vector3 row with copy/paste/reset buttons, an optional proportional lock, and a
    /// context menu for per-axis copying. Kept inside the Helpful Editor assembly so the suite stays
    /// self-contained — an Editor asmdef cannot reference Assembly-CSharp-Editor.
    /// </summary>
    internal static class LinkedVector3Field
    {
        private const float ButtonWidth = 20f;
        private const float LockWidth = 20f;
        private const float ButtonsTotal = ButtonWidth * 3f;
        private const float Spacing = 5f;

        /// <param name="locked">Current lock state. Passed by ref — updated when toggled.</param>
        /// <param name="extraContextItems">Extra items appended to the label/field right-click menu.</param>
        /// <param name="extraResetItems">Extra items shown when right-clicking the reset button.</param>
        /// <param name="property">
        /// The row's backing property, if it has one. Only used to mark the row as a property, which is
        /// what puts the prefab override bar beside it, bolds the label and offers Revert on right-click
        /// — the value itself is still read and written by the caller.
        /// </param>
        public static Vector3 Draw(string label, Vector3 value, Vector3 resetValue, bool showLock, ref bool locked,
            Action<GenericMenu> extraContextItems = null, Action<GenericMenu> extraResetItems = null,
            SerializedProperty property = null)
        {
            Vector3 newValue;
            float lockW = showLock ? LockWidth : 0f;

            if (EditorGUIUtility.wideMode)
            {
                Rect rowRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);

                if (property != null) EditorGUI.BeginProperty(rowRect, GUIContent.none, property);
                newValue = DrawRow(rowRect, label, value, resetValue, showLock, lockW, ref locked, extraContextItems, extraResetItems);
                if (property != null) EditorGUI.EndProperty();
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                float labelW = EditorGUIUtility.labelWidth - lockW;
                Rect labelRect = GUILayoutUtility.GetRect(new GUIContent(label), GUI.skin.label, GUILayout.Width(labelW));
                HandleContextClick(labelRect, value, extraContextItems);
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

                Rect fieldRow = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                Rect fieldRect = new Rect(fieldRow.x, fieldRow.y, fieldRow.width - ButtonsTotal - Spacing, fieldRow.height);
                Rect buttonsRect = new Rect(fieldRect.xMax + Spacing, fieldRow.y, ButtonsTotal, fieldRow.height);

                // Opened around the fields rather than the label above them: stacked, the label has
                // already been drawn by the time there is a rect to hang the override bar on.
                if (property != null) EditorGUI.BeginProperty(fieldRect, GUIContent.none, property);

                HandleContextClick(fieldRect, value, extraContextItems);

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

                Vector3 buttonResult = newValue;
                DrawButtonsRect(buttonsRect, value, resetValue, label,
                    onCopy: () => CopyToClipboard(value),
                    onPaste: pasted => buttonResult = pasted,
                    onReset: () => buttonResult = resetValue,
                    extraResetItems: extraResetItems);
                newValue = buttonResult;

                if (property != null) EditorGUI.EndProperty();
            }

            return newValue;
        }

        /// <summary>Layout-free variant for Rect-based drawers. Always renders as a single line.</summary>
        public static Vector3 Draw(Rect position, string label, Vector3 value, Vector3 resetValue, bool showLock, ref bool locked, out float heightUsed,
            Action<GenericMenu> extraContextItems = null, Action<GenericMenu> extraResetItems = null)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            Rect rowRect = new Rect(position.x, position.y, position.width, lineHeight);
            heightUsed = lineHeight;
            return DrawRow(rowRect, label, value, resetValue, showLock, showLock ? LockWidth : 0f, ref locked, extraContextItems, extraResetItems);
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

        private static Vector3 DrawRow(Rect rowRect, string label, Vector3 value, Vector3 resetValue, bool showLock, float lockW, ref bool locked,
            Action<GenericMenu> extraContextItems, Action<GenericMenu> extraResetItems)
        {
            float labelW = EditorGUIUtility.labelWidth - lockW;
            float fieldW = rowRect.width - EditorGUIUtility.labelWidth - ButtonsTotal - Spacing;

            Rect labelRect = new Rect(rowRect.x, rowRect.y, labelW, rowRect.height);
            Rect lockRect = new Rect(labelRect.xMax, rowRect.y, lockW, rowRect.height);
            Rect fieldRect = new Rect(rowRect.x + EditorGUIUtility.labelWidth, rowRect.y, fieldW, rowRect.height);
            Rect buttonsRect = new Rect(fieldRect.xMax + Spacing, rowRect.y, ButtonsTotal, rowRect.height);

            HandleContextClick(labelRect, value, extraContextItems);
            EditorGUI.LabelField(labelRect, label);

            if (showLock)
            {
                EditorGUIUtility.AddCursorRect(lockRect, MouseCursor.Link);
                locked = GUI.Toggle(lockRect, locked,
                    EditorGUIUtility.IconContent(locked ? "Linked" : "Unlinked"),
                    EditorStyles.label);
            }

            HandleContextClick(fieldRect, value, extraContextItems);

            bool prevWide = EditorGUIUtility.wideMode;
            EditorGUIUtility.wideMode = true;
            EditorGUI.BeginChangeCheck();
            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            Vector3 newValue = EditorGUI.Vector3Field(fieldRect, GUIContent.none, value);
            EditorGUI.indentLevel = oldIndent;
            EditorGUIUtility.wideMode = prevWide;
            if (EditorGUI.EndChangeCheck() && showLock && locked)
                newValue = ApplyLock(value, newValue, value);

            Vector3 buttonResult = newValue;
            DrawButtonsRect(buttonsRect, value, resetValue, label,
                onCopy: () => CopyToClipboard(value),
                onPaste: pasted => buttonResult = pasted,
                onReset: () => buttonResult = resetValue,
                extraResetItems: extraResetItems);

            return buttonResult;
        }

        private static void DrawButtonsRect(Rect rect, Vector3 current, Vector3 resetValue, string label,
            Action onCopy, Action<Vector3> onPaste, Action onReset, Action<GenericMenu> extraResetItems)
        {
            Rect copyRect = new Rect(rect.x, rect.y, ButtonWidth, rect.height);
            Rect pasteRect = new Rect(rect.x + ButtonWidth, rect.y, ButtonWidth, rect.height);
            Rect resetRect = new Rect(rect.x + ButtonWidth * 2f, rect.y, ButtonWidth, rect.height);

            if (GUI.Button(copyRect, new GUIContent("C", "Copy"), EditorStyles.miniButtonLeft))
            {
                onCopy?.Invoke();
            }

            EditorGUI.BeginDisabledGroup(!CanPaste());
            if (GUI.Button(pasteRect, new GUIContent("P", "Paste"), EditorStyles.miniButtonMid))
            {
                if (TryParseClipboard(out Vector3 parsed)) onPaste?.Invoke(parsed);
            }
            EditorGUI.EndDisabledGroup();

            if (extraResetItems != null && Event.current.type == EventType.MouseDown && Event.current.button == 1 && resetRect.Contains(Event.current.mousePosition))
            {
                GenericMenu menu = new GenericMenu();
                extraResetItems(menu);
                menu.ShowAsContext();
                Event.current.Use();
                return;
            }

            EditorGUI.BeginDisabledGroup(current == resetValue);
            if (GUI.Button(resetRect, new GUIContent("R", $"Reset {label.ToLower()}"), EditorStyles.miniButtonRight))
            {
                onReset?.Invoke();
            }
            EditorGUI.EndDisabledGroup();
        }

        private static void HandleContextClick(Rect rect, Vector3 value, Action<GenericMenu> extraContextItems)
        {
            if (Event.current.type != EventType.ContextClick) return;
            if (!rect.Contains(Event.current.mousePosition)) return;

            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Copy"), false, () => CopyToClipboard(value));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Copy X"), false, () => EditorGUIUtility.systemCopyBuffer = Format(value.x));
            menu.AddItem(new GUIContent("Copy Y"), false, () => EditorGUIUtility.systemCopyBuffer = Format(value.y));
            menu.AddItem(new GUIContent("Copy Z"), false, () => EditorGUIUtility.systemCopyBuffer = Format(value.z));

            if (extraContextItems != null)
            {
                menu.AddSeparator("");
                extraContextItems(menu);
            }

            menu.ShowAsContext();
            Event.current.Use();
        }

        /// <summary>
        /// Shared with the component header buttons so a value copied from a Transform row can be
        /// pasted onto a RectTransform and back.
        ///
        /// Written and read invariantly. The comma is the separator here, and under a locale that
        /// also uses it as the decimal point the current culture would write (1.5, 2.5, 3.5) as
        /// "1,5,2,5,3,5" — six fields where three were meant, which fails to parse back.
        /// </summary>
        public static void CopyToClipboard(Vector3 value) =>
            EditorGUIUtility.systemCopyBuffer = $"{Format(value.x)},{Format(value.y)},{Format(value.z)}";

        /// <summary>One axis, in the same invariant form the whole-vector copy uses.</summary>
        public static string Format(float value) => value.ToString(CultureInfo.InvariantCulture);

        /// <summary>
        /// The Copy Quaternion form, shared by the three rotation rows so all of them write the same
        /// thing — and the same thing they would if the row had copied a Vector3.
        /// </summary>
        public static string FormatQuaternion(Quaternion value) =>
            $"{Format(value.x)},{Format(value.y)},{Format(value.z)},{Format(value.w)}";

        public static bool TryParse(string text, out float value) =>
            float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);

        public static bool CanPaste() => TryParseClipboard(out _);

        public static bool TryParseClipboard(out Vector3 result)
        {
            result = Vector3.zero;
            string clipboard = EditorGUIUtility.systemCopyBuffer;
            if (string.IsNullOrEmpty(clipboard)) return false;

            string[] parts = clipboard.Split(',');
            if (parts.Length != 3) return false;

            if (TryParse(parts[0], out float x) &&
                TryParse(parts[1], out float y) &&
                TryParse(parts[2], out float z))
            {
                result = new Vector3(x, y, z);
                return true;
            }

            return false;
        }
    }
}
