using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNExtensions.HelpfulEditor.Inspector
{
    /// <summary>
    /// RectTransform inspector in the same visual language as the Transform one. The field set is
    /// conditional per axis because anchoring changes what is actually editable: a stretched axis
    /// exposes its two offsets, a non-stretched axis exposes position and size.
    /// </summary>
    [CustomEditor(typeof(RectTransform))]
    [CanEditMultipleObjects]
    internal class BetterRectTransformEditor : Editor
    {
        private static readonly Type NativeEditorType = typeof(Editor).Assembly.GetType("UnityEditor.RectTransformEditor");

        private static bool _rawEditMode;
        private static bool _scaleLocked;
        private static bool _scaleLockInitialized;
        private static bool _anchorPickerUnavailable;

        private Editor _nativeEditor;
        private Vector3 _lastScale;

        private void OnEnable()
        {
            if (target is RectTransform rectTransform) _lastScale = rectTransform.localScale;

            if (!_scaleLockInitialized)
            {
                _scaleLocked = HelpfulEditorSettings.Inspector.scaleLockDefaultOn;
                _scaleLockInitialized = true;
            }
        }

        private void OnDisable()
        {
            if (_nativeEditor) DestroyImmediate(_nativeEditor);
        }

        public override void OnInspectorGUI()
        {
            InspectorSettings settings = HelpfulEditorSettings.Inspector;

            if (!settings.moduleEnabled || !settings.betterRectTransformEnabled)
            {
                DrawNativeInspector();
                return;
            }

            if (!(target is RectTransform main)) return;

            DrawTopRow();

            if (_rawEditMode) DrawRawFields();
            else DrawComputedFields(main);

            DrawPivot();
            DrawRotation(settings);
            DrawScale(settings);
        }

        private void DrawNativeInspector()
        {
            Editor native = GetNativeEditor();

            if (native) native.OnInspectorGUI();
            else DrawDefaultInspector();
        }

        private Editor GetNativeEditor()
        {
            if (!_nativeEditor && NativeEditorType != null) _nativeEditor = CreateEditor(targets, NativeEditorType);
            return _nativeEditor;
        }

        private void DrawTopRow()
        {
            EditorGUILayout.BeginHorizontal();

            DrawAnchorPresetPicker();

            GUILayout.FlexibleSpace();

            _rawEditMode = GUILayout.Toggle(_rawEditMode, new GUIContent("R", "Raw edit mode — show anchors and offsets as stored values"),
                EditorStyles.miniButton, GUILayout.Width(22f));

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Unity's anchor preset widget is not reproducible from public API, so the real one is
        /// borrowed by reflecting into RectTransformEditor. If that ever breaks, only this button
        /// disappears — the rest of the inspector keeps working.
        /// </summary>
        private void DrawAnchorPresetPicker()
        {
            if (_anchorPickerUnavailable || NativeEditorType == null) return;

            try
            {
                Editor native = GetNativeEditor();
                if (!native) return;

                MethodInfo method = null;
                foreach (MethodInfo candidate in NativeEditorType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (candidate.Name != "LayoutDropdownButton") continue;
                    method = candidate;
                    break;
                }

                if (method == null)
                {
                    _anchorPickerUnavailable = true;
                    return;
                }

                ParameterInfo[] parameters = method.GetParameters();
                object[] args = parameters.Length == 0
                    ? Array.Empty<object>()
                    : new object[] { AnyTargetWithoutParent() };

                method.Invoke(native, args);
            }
            catch (Exception e)
            {
                _anchorPickerUnavailable = true;
                Debug.LogWarning($"[HelpfulEditor] Anchor preset picker unavailable on this Unity version, hiding it. ({e.Message})");
            }
        }

        private bool AnyTargetWithoutParent()
        {
            foreach (Object obj in targets)
            {
                if (obj is RectTransform rectTransform && !rectTransform.parent) return true;
            }

            return false;
        }

        private void DrawComputedFields(RectTransform main)
        {
            bool stretchedX = !Mathf.Approximately(main.anchorMin.x, main.anchorMax.x);
            bool stretchedY = !Mathf.Approximately(main.anchorMin.y, main.anchorMax.y);

            if (stretchedX)
            {
                DrawFloatPair("Left", main.offsetMin.x, "Right", -main.offsetMax.x,
                    (rect, value) => SetOffsetMin(rect, value, rect.offsetMin.y),
                    (rect, value) => SetOffsetMax(rect, -value, rect.offsetMax.y),
                    "Edit Rect");
            }
            else
            {
                DrawFloatPair("Pos X", main.anchoredPosition.x, "Width", main.sizeDelta.x,
                    (rect, value) => rect.anchoredPosition = new Vector2(value, rect.anchoredPosition.y),
                    (rect, value) => rect.sizeDelta = new Vector2(value, rect.sizeDelta.y),
                    "Edit Rect");
            }

            if (stretchedY)
            {
                DrawFloatPair("Top", -main.offsetMax.y, "Bottom", main.offsetMin.y,
                    (rect, value) => SetOffsetMax(rect, rect.offsetMax.x, -value),
                    (rect, value) => SetOffsetMin(rect, rect.offsetMin.x, value),
                    "Edit Rect");
            }
            else
            {
                DrawFloatPair("Pos Y", main.anchoredPosition.y, "Height", main.sizeDelta.y,
                    (rect, value) => rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, value),
                    (rect, value) => rect.sizeDelta = new Vector2(rect.sizeDelta.x, value),
                    "Edit Rect");
            }

            DrawSingleFloat("Pos Z", main.localPosition.z,
                (rect, value) => rect.localPosition = new Vector3(rect.localPosition.x, rect.localPosition.y, value),
                "Edit Pos Z");

            DrawVector2("Anchor Min", main.anchorMin, (rect, value) => rect.anchorMin = value, "Edit Anchors");
            DrawVector2("Anchor Max", main.anchorMax, (rect, value) => rect.anchorMax = value, "Edit Anchors");
        }

        private void DrawRawFields()
        {
            if (!(target is RectTransform main)) return;

            DrawVector2("Anchor Min", main.anchorMin, (rect, value) => rect.anchorMin = value, "Edit Anchors");
            DrawVector2("Anchor Max", main.anchorMax, (rect, value) => rect.anchorMax = value, "Edit Anchors");
            DrawVector2("Offset Min", main.offsetMin, (rect, value) => rect.offsetMin = value, "Edit Offsets");
            DrawVector2("Offset Max", main.offsetMax, (rect, value) => rect.offsetMax = value, "Edit Offsets");
        }

        private void DrawPivot()
        {
            if (!(target is RectTransform main)) return;

            DrawVector2("Pivot", main.pivot, (rect, value) => rect.pivot = value, "Edit Pivot");
        }

        private void DrawRotation(InspectorSettings settings)
        {
            Vector3 displayEuler = GetCommonValue(t => t.localEulerAngles, out bool mixed);
            bool unusedLock = false;

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = mixed;

            Vector3 newEuler = LinkedVector3Field.Draw("Rotation", displayEuler, Vector3.zero, false, ref unusedLock,
                extraResetItems: settings.resetMenuItemsEnabled
                    ? menu => BuildResetMenu(menu, (rect, value) => rect.localEulerAngles = value, Vector3.zero)
                    : null);

            EditorGUI.showMixedValue = false;

            if (!EditorGUI.EndChangeCheck()) return;

            Undo.RecordObjects(targets, "Rotation Changed");
            foreach (Object obj in targets)
            {
                if (obj is RectTransform rectTransform) rectTransform.localEulerAngles = newEuler;
            }
        }

        private void DrawScale(InspectorSettings settings)
        {
            Vector3 displayValue = GetCommonValue(t => t.localScale, out bool mixed);

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = mixed;

            Vector3 newValue = LinkedVector3Field.Draw("Scale", displayValue, Vector3.one, true, ref _scaleLocked,
                extraResetItems: settings.resetMenuItemsEnabled
                    ? menu => BuildResetMenu(menu, (rect, value) => rect.localScale = value, Vector3.one)
                    : null);

            EditorGUI.showMixedValue = false;

            if (!EditorGUI.EndChangeCheck()) return;

            if (_scaleLocked) newValue = LinkedVector3Field.ApplyLock(displayValue, newValue, _lastScale);

            Undo.RecordObjects(targets, "Scale Changed");
            foreach (Object obj in targets)
            {
                if (obj is RectTransform rectTransform) rectTransform.localScale = newValue;
            }

            _lastScale = newValue;
        }

        private void BuildResetMenu(GenericMenu menu, Action<RectTransform, Vector3> setter, Vector3 resetValue)
        {
            menu.AddItem(new GUIContent("Reset"), false, () =>
            {
                Undo.RecordObjects(targets, "Reset");
                foreach (Object obj in targets)
                {
                    if (obj is RectTransform rectTransform) setter(rectTransform, resetValue);
                }
            });

            menu.AddItem(new GUIContent("Reset Only Children"), false, () =>
            {
                foreach (Object obj in targets)
                {
                    if (!(obj is RectTransform rectTransform)) continue;

                    for (int i = 0; i < rectTransform.childCount; i++)
                    {
                        if (!(rectTransform.GetChild(i) is RectTransform child)) continue;

                        Undo.RecordObject(child, "Reset Only Children");
                        setter(child, resetValue);
                    }
                }
            });
        }

        private void DrawFloatPair(string labelA, float valueA, string labelB, float valueB,
            Action<RectTransform, float> setterA, Action<RectTransform, float> setterB, string undoName)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();
            float newA = EditorGUILayout.FloatField(labelA, valueA);
            bool changedA = EditorGUI.EndChangeCheck();

            float previousLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 52f;

            EditorGUI.BeginChangeCheck();
            float newB = EditorGUILayout.FloatField(labelB, valueB);
            bool changedB = EditorGUI.EndChangeCheck();

            EditorGUIUtility.labelWidth = previousLabelWidth;
            EditorGUILayout.EndHorizontal();

            if (!changedA && !changedB) return;

            Undo.RecordObjects(targets, undoName);
            foreach (Object obj in targets)
            {
                if (!(obj is RectTransform rectTransform)) continue;

                if (changedA) setterA(rectTransform, newA);
                if (changedB) setterB(rectTransform, newB);
            }
        }

        private void DrawSingleFloat(string label, float value, Action<RectTransform, float> setter, string undoName)
        {
            EditorGUI.BeginChangeCheck();
            float newValue = EditorGUILayout.FloatField(label, value);
            if (!EditorGUI.EndChangeCheck()) return;

            Undo.RecordObjects(targets, undoName);
            foreach (Object obj in targets)
            {
                if (obj is RectTransform rectTransform) setter(rectTransform, newValue);
            }
        }

        private void DrawVector2(string label, Vector2 value, Action<RectTransform, Vector2> setter, string undoName)
        {
            EditorGUI.BeginChangeCheck();
            Vector2 newValue = EditorGUILayout.Vector2Field(label, value);
            if (!EditorGUI.EndChangeCheck()) return;

            Undo.RecordObjects(targets, undoName);
            foreach (Object obj in targets)
            {
                if (obj is RectTransform rectTransform) setter(rectTransform, newValue);
            }
        }

        private static void SetOffsetMin(RectTransform rectTransform, float x, float y)
        {
            rectTransform.offsetMin = new Vector2(x, y);
        }

        private static void SetOffsetMax(RectTransform rectTransform, float x, float y)
        {
            rectTransform.offsetMax = new Vector2(x, y);
        }

        private Vector3 GetCommonValue(Func<RectTransform, Vector3> selector, out bool mixed)
        {
            Vector3 first = target is RectTransform firstRect ? selector(firstRect) : Vector3.zero;
            mixed = false;

            foreach (Object obj in targets)
            {
                if (!(obj is RectTransform rectTransform)) continue;

                if (selector(rectTransform) != first)
                {
                    mixed = true;
                    break;
                }
            }

            return first;
        }
    }
}
