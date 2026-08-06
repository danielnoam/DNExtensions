using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNExtensions.HelpfulEditor.Inspector
{
    /// <summary>
    /// The RectTransform inspector, rebuilt so the rotation and scale rows carry the same copy, paste
    /// and reset buttons the Transform rows do. There is no way to add those to Unity's own inspector,
    /// and a CustomEditor for RectTransform replaces it outright — so the rest of what it draws is
    /// reproduced here rather than lost.
    ///
    /// Two parts stay Unity's. The anchor preset grid is drawn and popped by LayoutDropdownWindow, and
    /// the Scene view handles come from a hidden native editor this one forwards OnSceneGUI to. Both
    /// are large, purely visual, and would be reproduced only to look identical.
    /// </summary>
    [CustomEditor(typeof(RectTransform))]
    [CanEditMultipleObjects]
    internal class BetterRectTransformEditor : Editor
    {
        /// <summary>Unity's own keys, so raw edit and the foldout do not reset when this inspector takes over.</summary>
        private const string RawEditPref = "RectTransformEditor.lockRect";

        private const string AnchorFoldoutPref = "RectTransformEditor.showAnchorProperties";

        private const string EulerHintProperty = "m_LocalEulerAnglesHint";
        private const string UndoName = "Modified RectTransform Values";

        private const float DropdownSize = 49f;
        private const float DropdownOffsetX = 2f;
        private const float DropdownOffsetY = 17f;
        private const float AxisLabelWidth = 13f;
        private const float ModeButtonWidth = 20f;
        private const float ButtonWidth = 20f;
        private const float ButtonsTotal = ButtonWidth * 3f;

        // DrivenTransformProperties values. RectTransform.drivenProperties is internal, so what comes
        // back from reflection is matched numerically rather than against the enum itself.
        private const int DrivenAnchoredPositionX = 2;
        private const int DrivenAnchoredPositionY = 4;
        private const int DrivenAnchoredPositionZ = 8;
        private const int DrivenRotation = 16;
        private const int DrivenScaleX = 32;
        private const int DrivenScaleY = 64;
        private const int DrivenScaleZ = 128;
        private const int DrivenAnchorMinX = 256;
        private const int DrivenAnchorMinY = 512;
        private const int DrivenAnchorMaxX = 1024;
        private const int DrivenAnchorMaxY = 2048;
        private const int DrivenSizeDeltaX = 4096;
        private const int DrivenSizeDeltaY = 8192;
        private const int DrivenPivotX = 16384;
        private const int DrivenPivotY = 32768;

        private static readonly int FloatFieldHash = "HelpfulEditorRectFloatField".GetHashCode();
        private static readonly Type NativeEditorType = typeof(Editor).Assembly.GetType("UnityEditor.RectTransformEditor");

        private static readonly GUIContent AnchorsLabel = new GUIContent("Anchors");
        private static readonly GUIContent MinLabel = new GUIContent("Min", "The normalized position in the parent rectangle that the lower left corner is anchored to.");
        private static readonly GUIContent MaxLabel = new GUIContent("Max", "The normalized position in the parent rectangle that the upper right corner is anchored to.");
        private static readonly GUIContent PivotLabel = new GUIContent("Pivot", "The pivot point specified in normalized values between 0 and 1. The pivot point is the origin of this rectangle. Rotation and scaling are around this point.");
        private const string RawTooltip = "Raw edit mode. When enabled, editing pivot and anchor values will not counter-adjust the position and size of the rectangle in order to make it stay in place.";
        private const string BlueprintTooltip = "Blueprint mode. Edit RectTransforms as if they were not rotated and scaled. This enables snapping too.";

        private static GUIContent _rawContent;
        private static GUIContent _blueprintContent;

        private static MethodInfo _nativeSceneGui;
        private static PropertyInfo _drivenProperties;
        private static Type _layoutDropdownType;
        private static MethodInfo _drawLayoutMode;
        private static MethodInfo _drawLayoutHeaders;
        private static bool _reflectionResolved;

        private static bool _scaleLocked;
        private static bool _scaleLockInitialized;
        private static bool _scaleLockDefault;

        private Editor _nativeEditor;
        private Vector3 _lastScale;

        private readonly TransformWorldFields _worldFields = new TransformWorldFields();

        private SerializedProperty _anchorMin;
        private SerializedProperty _anchorMax;
        private SerializedProperty _anchoredPosition;
        private SerializedProperty _sizeDelta;
        private SerializedProperty _pivot;
        private SerializedProperty _localScale;
        private SerializedProperty _localPositionZ;
        private SerializedProperty _anchoredPositionX;
        private SerializedProperty _anchoredPositionY;
        private SerializedProperty _sizeDeltaX;
        private SerializedProperty _sizeDeltaY;
        private SerializedProperty _eulerHint;

        private void OnEnable()
        {
            _anchorMin = serializedObject.FindProperty("m_AnchorMin");
            _anchorMax = serializedObject.FindProperty("m_AnchorMax");
            _anchoredPosition = serializedObject.FindProperty("m_AnchoredPosition");
            _sizeDelta = serializedObject.FindProperty("m_SizeDelta");
            _pivot = serializedObject.FindProperty("m_Pivot");
            _localScale = serializedObject.FindProperty("m_LocalScale");
            _localPositionZ = serializedObject.FindProperty("m_LocalPosition.z");

            _anchoredPositionX = _anchoredPosition?.FindPropertyRelative("x");
            _anchoredPositionY = _anchoredPosition?.FindPropertyRelative("y");
            _sizeDeltaX = _sizeDelta?.FindPropertyRelative("x");
            _sizeDeltaY = _sizeDelta?.FindPropertyRelative("y");
            _eulerHint = serializedObject.FindProperty(EulerHintProperty);

            if (target is RectTransform rect) _lastScale = rect.localScale;

            _worldFields.Forget();

            // Built up front rather than on demand: OnSceneGUI needs it, and that is the one place it
            // cannot be created. Its own OnEnable is what puts the anchor handles in the Scene view.
            EnsureNativeEditor();

            bool setting = HelpfulEditorSettings.Inspector.scaleLockDefaultOn;
            if (_scaleLockInitialized && setting == _scaleLockDefault) return;

            _scaleLocked = setting;
            _scaleLockDefault = setting;
            _scaleLockInitialized = true;
        }

        private void OnDisable()
        {
            if (_nativeEditor) DestroyImmediate(_nativeEditor);
        }

        /// <summary>
        /// Handed straight to Unity's editor. The anchor and pivot handles, their distance readouts and
        /// the parent rect preview are several hundred lines of Scene drawing that this inspector has no
        /// reason to differ from — and losing them is the first thing anyone would notice.
        /// </summary>
        private void OnSceneGUI()
        {
            // Only ever invoked here, never created: building an editor reads targets and the
            // serialized object, and Unity will not have either touched from inside OnSceneGUI.
            if (_nativeEditor && _nativeSceneGui != null) _nativeSceneGui.Invoke(_nativeEditor, null);
        }

        public override void OnInspectorGUI()
        {
            InspectorSettings settings = HelpfulEditorSettings.Inspector;

            if (!settings.moduleEnabled || !settings.betterTransformEnabled)
            {
                DrawNativeInspector();
                return;
            }

            ResolveReflection();
            serializedObject.Update();

            bool wideMode = EditorGUIUtility.wideMode;
            EditorGUIUtility.wideMode = true;

            DrawDrivenWarning();

            bool raw = EditorPrefs.GetBool(RawEditPref, false);
            bool stretchX = !raw && IsStretched(0);
            bool stretchY = !raw && IsStretched(1);

            DrawAnchorPresetButton();
            DrawPositionAndSize(stretchX, stretchY, raw);
            DrawAnchors(raw);
            DrawPivot(raw);

            EditorGUILayout.Space();

            DrawRotation(settings);
            DrawScale(settings);

            // Under a Canvas the world values are where the rect actually landed once the layout has
            // had its say, which is the reading the rect fields above cannot give on their own.
            if (settings.worldFieldsEnabled && TransformWorldFields.AnyParented(targets))
            {
                _worldFields.Draw(serializedObject, target as Transform, targets);
            }

            EditorGUIUtility.wideMode = wideMode;

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawNativeInspector()
        {
            EnsureNativeEditor();

            if (_nativeEditor) _nativeEditor.OnInspectorGUI();
            else DrawDefaultInspector();
        }

        private void EnsureNativeEditor()
        {
            ResolveReflection();

            if (!_nativeEditor && NativeEditorType != null) _nativeEditor = CreateEditor(targets, NativeEditorType);
        }

        /// <summary>Which values a layout component has taken over, so the fields for them read as locked.</summary>
        private int DrivenFlags()
        {
            if (_drivenProperties == null) return 0;

            int flags = 0;

            foreach (Object obj in targets)
            {
                if (!(obj is RectTransform rect)) continue;

                try
                {
                    flags |= Convert.ToInt32(_drivenProperties.GetValue(rect, null));
                }
                catch (Exception)
                {
                    // Falls through: an unreadable value is treated as nothing being driven, which
                    // leaves the fields editable rather than locking an inspector for no reason.
                }
            }

            return flags;
        }

        private bool IsDriven(int flag) => (DrivenFlags() & flag) != 0;

        private void DrawDrivenWarning()
        {
            RectTransform driven = null;

            foreach (Object obj in targets)
            {
                if (obj is RectTransform rect && rect.drivenByObject) driven = rect;
            }

            if (!driven) return;

            if (targets.Length == 1)
            {
                EditorGUILayout.HelpBox($"Some values driven by {driven.drivenByObject.GetType().Name}.", MessageType.None);
                return;
            }

            EditorGUILayout.HelpBox("Some values in some or all objects are driven.", MessageType.None);
        }

        private bool IsStretched(int axis)
        {
            foreach (Object obj in targets)
            {
                if (!(obj is RectTransform rect)) continue;

                float min = axis == 0 ? rect.anchorMin.x : rect.anchorMin.y;
                float max = axis == 0 ? rect.anchorMax.x : rect.anchorMax.y;

                if (!Mathf.Approximately(min, max)) return true;
            }

            return false;
        }

        /// <summary>
        /// The preset square, drawn into the label column beside the position rows. It takes a zero-high
        /// layout rect and then paints outside it, which is how Unity gets it to sit alongside rows that
        /// have not been laid out yet.
        /// </summary>
        private void DrawAnchorPresetButton()
        {
            if (_layoutDropdownType == null) return;

            bool anyWithoutParent = false;

            foreach (Object obj in targets)
            {
                if (obj is RectTransform rect && !rect.parent) anyWithoutParent = true;
            }

            Rect rect2 = GUILayoutUtility.GetRect(0f, 0f);
            rect2.x += DropdownOffsetX;
            rect2.y += DropdownOffsetY;
            rect2.width = DropdownSize;
            rect2.height = DropdownSize;

            using (new EditorGUI.DisabledScope(anyWithoutParent))
            {
                if (EditorGUI.DropdownButton(rect2, GUIContent.none, FocusType.Passive, "label"))
                {
                    GUIUtility.keyboardControl = 0;
                    ShowPresetWindow(rect2);
                }
            }

            Rect inner = new RectOffset(7, 7, 7, 7).Remove(rect2);

            InvokeLayoutDraw(_drawLayoutMode, inner);
            InvokeLayoutDraw(_drawLayoutHeaders, inner);
        }

        /// <summary>Falls back to a letter, so a missing icon leaves a button that still says what it is.</summary>
        private static GUIContent ModeIcon(string iconName, string fallback, string tooltip)
        {
            try
            {
                GUIContent icon = EditorGUIUtility.IconContent(iconName);
                if (icon?.image) return new GUIContent(icon.image, tooltip);
            }
            catch (Exception)
            {
                // Falls through to the letter below.
            }

            return new GUIContent(fallback, tooltip);
        }

        private void ShowPresetWindow(Rect buttonRect)
        {
            try
            {
                object window = Activator.CreateInstance(_layoutDropdownType,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                    null, new object[] { serializedObject }, null);

                if (window is PopupWindowContent content) PopupWindow.Show(buttonRect, content);
            }
            catch (Exception)
            {
                // Falls through: without the popup the presets are still reachable from the Scene view
                // handles, and the fields below are all still editable by hand.
            }
        }

        private void InvokeLayoutDraw(MethodInfo method, Rect rect)
        {
            if (method == null) return;

            try
            {
                method.Invoke(null, new object[] { rect, _anchorMin, _anchorMax, _anchoredPosition, _sizeDelta });
            }
            catch (Exception)
            {
                // Falls through: the preview is decoration, and the button still opens the presets.
            }
        }

        /// <summary>
        /// Two rows of three columns, labels above the fields. Which values those are depends on the
        /// anchors: an axis whose anchors are apart is edited as edge distances — Left/Right rather than
        /// Pos X/Width — because that is what the rect is actually pinned by. Raw edit mode opts out and
        /// shows the stored values, where the size is a delta against the anchor span rather than a width.
        /// </summary>
        private void DrawPositionAndSize(bool stretchX, bool stretchY, bool raw)
        {
            float line = EditorGUIUtility.singleLineHeight;

            Rect rect = EditorGUILayout.GetControlRect(false, line * 2f);
            Rect second = EditorGUILayout.GetControlRect(false, line * 2f);

            if (stretchX)
            {
                FloatColumn(rect, 0, new GUIContent("Left"), DrivenSizeDeltaX,
                    r => r.offsetMin.x, (r, v) => r.offsetMin = new Vector2(v, r.offsetMin.y),
                    _anchoredPositionX, _sizeDeltaX);

                FloatColumn(second, 0, new GUIContent("Right"), DrivenSizeDeltaX,
                    r => -r.offsetMax.x, (r, v) => r.offsetMax = new Vector2(-v, r.offsetMax.y),
                    _anchoredPositionX, _sizeDeltaX);
            }
            else
            {
                FloatColumn(rect, 0, new GUIContent("Pos X"), DrivenAnchoredPositionX,
                    r => r.anchoredPosition.x, (r, v) => r.anchoredPosition = new Vector2(v, r.anchoredPosition.y),
                    _anchoredPositionX);

                FloatColumn(second, 0, new GUIContent(raw && IsStretched(0) ? "W Delta" : "Width"), DrivenSizeDeltaX,
                    r => r.sizeDelta.x, (r, v) => r.sizeDelta = new Vector2(v, r.sizeDelta.y),
                    _sizeDeltaX);
            }

            if (stretchY)
            {
                FloatColumn(rect, 1, new GUIContent("Top"), DrivenSizeDeltaY,
                    r => -r.offsetMax.y, (r, v) => r.offsetMax = new Vector2(r.offsetMax.x, -v),
                    _anchoredPositionY, _sizeDeltaY);

                FloatColumn(second, 1, new GUIContent("Bottom"), DrivenSizeDeltaY,
                    r => r.offsetMin.y, (r, v) => r.offsetMin = new Vector2(r.offsetMin.x, v),
                    _anchoredPositionY, _sizeDeltaY);
            }
            else
            {
                FloatColumn(rect, 1, new GUIContent("Pos Y"), DrivenAnchoredPositionY,
                    r => r.anchoredPosition.y, (r, v) => r.anchoredPosition = new Vector2(r.anchoredPosition.x, v),
                    _anchoredPositionY);

                FloatColumn(second, 1, new GUIContent(raw && IsStretched(1) ? "H Delta" : "Height"), DrivenSizeDeltaY,
                    r => r.sizeDelta.y, (r, v) => r.sizeDelta = new Vector2(r.sizeDelta.x, v),
                    _sizeDeltaY);
            }

            FloatColumn(rect, 2, new GUIContent("Pos Z"), DrivenAnchoredPositionZ,
                r => r.anchoredPosition3D.z,
                (r, v) => r.anchoredPosition3D = new Vector3(r.anchoredPosition3D.x, r.anchoredPosition3D.y, v),
                _localPositionZ);

            DrawRectButtons(second);
            DrawModeButtons(second, raw);
        }

        /// <summary>
        /// Copy, paste and reset for the whole rect rather than for a row, because a row is not a stable
        /// thing here: Pos X becomes Left the moment the axis stretches, and pasting one into the other
        /// would be meaningless. What is copied is the rect as it ends up — the pivot's position in the
        /// parent and the size it is drawn at — and pasting rebuilds that under whatever anchors the
        /// target happens to have, so it lands where it looked rather than where its numbers said.
        ///
        /// Sits on the free line of the size row's third column, above the blueprint and raw toggles:
        /// the only room in the block that is not already spoken for at a normal inspector width.
        /// </summary>
        private void DrawRectButtons(Rect row)
        {
            Rect column = GetColumnRect(row, 2);
            Rect strip = new Rect(column.xMax - ButtonsTotal, column.y, ButtonsTotal, EditorGUIUtility.singleLineHeight);

            Rect copyRect = new Rect(strip.x, strip.y, ButtonWidth, strip.height);
            Rect pasteRect = new Rect(strip.x + ButtonWidth, strip.y, ButtonWidth, strip.height);
            Rect resetRect = new Rect(strip.x + ButtonWidth * 2f, strip.y, ButtonWidth, strip.height);

            if (GUI.Button(copyRect, new GUIContent("C", "Copy position and size"), EditorStyles.miniButtonLeft))
            {
                CopyRect();
            }

            using (new EditorGUI.DisabledScope(!TryParseRect(out _, out _, out _)))
            {
                if (GUI.Button(pasteRect, new GUIContent("P", "Paste position and size"), EditorStyles.miniButtonMid))
                {
                    if (TryParseRect(out Vector3 pivotPosition, out Vector2 size, out bool hasSize)) PasteRect(pivotPosition, size, hasSize);
                }
            }

            if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && resetRect.Contains(Event.current.mousePosition))
            {
                ShowResetMenu();
                Event.current.Use();
                return;
            }

            if (GUI.Button(resetRect, new GUIContent("R", "Reset position, and flush stretched axes to their anchors"), EditorStyles.miniButtonRight))
            {
                ResetRect(true, false);
            }
        }

        private void ShowResetMenu()
        {
            GenericMenu menu = new GenericMenu();

            menu.AddItem(new GUIContent("Reset Position"), false, () => ResetRect(true, false));
            menu.AddItem(new GUIContent("Flush To Anchors"), false, () => ResetRect(false, true));
            menu.AddItem(new GUIContent("Reset Position And Flush"), false, () => ResetRect(true, true));

            menu.ShowAsContext();
        }

        private void CopyRect()
        {
            if (!(target is RectTransform rect)) return;

            Vector3 pivotPosition = rect.localPosition;
            Vector2 size = rect.rect.size;

            EditorGUIUtility.systemCopyBuffer =
                $"{pivotPosition.x},{pivotPosition.y},{pivotPosition.z},{size.x},{size.y}";
        }

        /// <summary>
        /// Five numbers is a rect. Three is a plain Vector3, which is what the Transform rows and the
        /// rotation and scale rows here put on the clipboard — taken as a position on its own, so a
        /// position copied anywhere in the suite can be pasted onto a rect without moving its size.
        /// </summary>
        private static bool TryParseRect(out Vector3 pivotPosition, out Vector2 size, out bool hasSize)
        {
            pivotPosition = Vector3.zero;
            size = Vector2.zero;
            hasSize = false;

            string clipboard = EditorGUIUtility.systemCopyBuffer;
            if (string.IsNullOrEmpty(clipboard)) return false;

            string[] parts = clipboard.Split(',');
            if (parts.Length != 3 && parts.Length != 5) return false;

            float[] values = new float[parts.Length];

            for (int i = 0; i < parts.Length; i++)
            {
                if (!float.TryParse(parts[i], out values[i])) return false;
            }

            pivotPosition = new Vector3(values[0], values[1], values[2]);

            if (parts.Length != 5) return true;

            size = new Vector2(values[3], values[4]);
            hasSize = true;

            return true;
        }

        private void PasteRect(Vector3 pivotPosition, Vector2 size, bool hasSize)
        {
            Undo.RecordObjects(targets, "Paste RectTransform Values");

            foreach (Object obj in targets)
            {
                if (!(obj is RectTransform rect)) continue;

                Vector2 anchorRefMin = Vector2.zero;
                Vector2 anchorSpan = Vector2.zero;

                if (rect.parent is RectTransform parent)
                {
                    Rect parentRect = parent.rect;
                    anchorRefMin = parentRect.min + Vector2.Scale(parentRect.size, rect.anchorMin);
                    anchorSpan = Vector2.Scale(parentRect.size, rect.anchorMax - rect.anchorMin);
                }

                if (hasSize) rect.sizeDelta = size - anchorSpan;

                Vector2 anchored = (Vector2)pivotPosition - (anchorRefMin + Vector2.Scale(anchorSpan, rect.pivot));
                rect.anchoredPosition3D = new Vector3(anchored.x, anchored.y, pivotPosition.z);
            }

            serializedObject.Update();
        }

        /// <summary>
        /// Size is only ever zeroed on an axis that stretches, where zero means flush to the anchors.
        /// On an axis that does not, zero is a rect with no width — a reset that leaves nothing to see
        /// is not a reset anyone wants, so that axis keeps the size it had.
        /// </summary>
        private void ResetRect(bool position, bool size)
        {
            Undo.RecordObjects(targets, "Reset RectTransform Values");

            foreach (Object obj in targets)
            {
                if (!(obj is RectTransform rect)) continue;

                if (position) rect.anchoredPosition3D = Vector3.zero;
                if (!size) continue;

                Vector2 delta = rect.sizeDelta;

                if (!Mathf.Approximately(rect.anchorMin.x, rect.anchorMax.x)) delta.x = 0f;
                if (!Mathf.Approximately(rect.anchorMin.y, rect.anchorMax.y)) delta.y = 0f;

                rect.sizeDelta = delta;
            }

            serializedObject.Update();
        }

        /// <summary>
        /// Blueprint and raw edit, in the empty third column of the size row — where Unity keeps them.
        /// Both drive Unity's own state rather than a copy: raw edit shares the editor's preference key
        /// and blueprint is a Tools mode the Scene view handles read, so the two inspectors agree.
        /// </summary>
        private void DrawModeButtons(Rect row, bool raw)
        {
            _blueprintContent ??= ModeIcon("RectTransformBlueprint", "B", BlueprintTooltip);
            _rawContent ??= ModeIcon("RectTransformRaw", "R", RawTooltip);

            Rect column = GetColumnRect(row, 2);
            float line = EditorGUIUtility.singleLineHeight;

            Rect left = new Rect(column.xMax - ModeButtonWidth * 2f, column.y + line, ModeButtonWidth, line);
            Rect right = new Rect(column.xMax - ModeButtonWidth, column.y + line, ModeButtonWidth, line);

            EditorGUI.BeginChangeCheck();
            bool blueprint = GUI.Toggle(left, Tools.rectBlueprintMode, _blueprintContent, "ButtonLeft");
            if (EditorGUI.EndChangeCheck())
            {
                Tools.rectBlueprintMode = blueprint;

                // Unity calls its own Tools.RepaintAllToolViews here, which is internal — the Scene
                // views are the ones that need to hear about it, and this reaches all of them.
                SceneView.RepaintAll();
            }

            EditorGUI.BeginChangeCheck();
            bool newRaw = GUI.Toggle(right, raw, _rawContent, "ButtonRight");
            if (EditorGUI.EndChangeCheck()) EditorPrefs.SetBool(RawEditPref, newRaw);
        }

        private void DrawAnchors(bool raw)
        {
            Rect header = EditorGUILayout.GetControlRect();
            bool expanded = EditorPrefs.GetBool(AnchorFoldoutPref, false);

            EditorGUI.BeginChangeCheck();
            bool newExpanded = EditorGUI.Foldout(header, expanded, AnchorsLabel, true);
            if (EditorGUI.EndChangeCheck()) EditorPrefs.SetBool(AnchorFoldoutPref, newExpanded);

            if (!newExpanded) return;

            EditorGUI.indentLevel++;

            Vector2Row(MinLabel, _anchorMin, DrivenAnchorMinX, DrivenAnchorMinY,
                r => r.anchorMin, (r, v) => SetAnchorSmart(r, v, r.anchorMax, raw));

            Vector2Row(MaxLabel, _anchorMax, DrivenAnchorMaxX, DrivenAnchorMaxY,
                r => r.anchorMax, (r, v) => SetAnchorSmart(r, r.anchorMin, v, raw));

            EditorGUI.indentLevel--;
        }

        private void DrawPivot(bool raw)
        {
            Vector2Row(PivotLabel, _pivot, DrivenPivotX, DrivenPivotY,
                r => r.pivot, (r, v) => SetPivotSmart(r, v, raw));
        }

        /// <summary>
        /// Same rotation handling the Transform inspector uses: the value shown is Unity's own euler
        /// hint rather than localEulerAngles, so typing 370 reads back as 370 instead of 10.
        /// </summary>
        private void DrawRotation(InspectorSettings settings)
        {
            if (!(target is RectTransform main)) return;

            Vector3 displayEuler;
            bool mixed;

            if (_eulerHint != null)
            {
                displayEuler = _eulerHint.vector3Value;
                mixed = _eulerHint.hasMultipleDifferentValues;
            }
            else
            {
                displayEuler = GetCommonValue(t => t.localEulerAngles, out mixed);
            }

            Quaternion quaternion = main.localRotation;
            bool unusedLock = false;

            using (new EditorGUI.DisabledScope(IsDriven(DrivenRotation)))
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = mixed;

                Vector3 newEuler = LinkedVector3Field.Draw("Rotation", displayEuler, Vector3.zero, false, ref unusedLock,
                    extraContextItems: menu => menu.AddItem(new GUIContent("Copy Quaternion"), false, () =>
                        EditorGUIUtility.systemCopyBuffer = $"{quaternion.x},{quaternion.y},{quaternion.z},{quaternion.w}"),
                    extraResetItems: settings.resetMenuItemsEnabled
                        ? menu => TransformResetMenu.Build(menu, targets, (t, v) => t.localEulerAngles = v, Vector3.zero)
                        : null,
                    property: serializedObject.FindProperty("m_LocalRotation"));

                EditorGUI.showMixedValue = false;

                if (!EditorGUI.EndChangeCheck()) return;

                Vector3 delta = newEuler - displayEuler;
                Undo.RecordObjects(targets, "Rotation Changed");

                foreach (Object obj in targets)
                {
                    if (obj is RectTransform rect) rect.localEulerAngles = mixed ? rect.localEulerAngles + delta : newEuler;
                }

                serializedObject.Update();

                if (_eulerHint != null && !mixed)
                {
                    _eulerHint.vector3Value = newEuler;
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }

        private void DrawScale(InspectorSettings settings)
        {
            Vector3 displayValue = GetCommonValue(t => t.localScale, out bool mixed);
            bool driven = IsDriven(DrivenScaleX | DrivenScaleY | DrivenScaleZ);

            using (new EditorGUI.DisabledScope(driven))
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = mixed;

                Vector3 newValue = LinkedVector3Field.Draw("Scale", displayValue, Vector3.one, true, ref _scaleLocked,
                    extraResetItems: settings.resetMenuItemsEnabled
                        ? menu => TransformResetMenu.Build(menu, targets, (t, v) => t.localScale = v, Vector3.one)
                        : null,
                    property: _localScale);

                EditorGUI.showMixedValue = false;

                if (!EditorGUI.EndChangeCheck()) return;

                if (_scaleLocked) newValue = LinkedVector3Field.ApplyLock(displayValue, newValue, _lastScale);

                Vector3 delta = newValue - displayValue;
                Undo.RecordObjects(targets, "Scale Changed");

                foreach (Object obj in targets)
                {
                    if (obj is RectTransform rect) rect.localScale = mixed ? rect.localScale + delta : newValue;
                }

                _lastScale = newValue;
                serializedObject.Update();
            }
        }

        /// <summary>
        /// Moves the anchors without moving the rect: the anchored position and size are re-derived so
        /// the corners land back where they were. Raw edit mode skips that, which is the whole point of
        /// the mode — it writes the stored values and lets the rect go where the numbers put it.
        /// </summary>
        private static void SetAnchorSmart(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, bool raw)
        {
            if (raw || !(rect.parent is RectTransform parent))
            {
                rect.anchorMin = anchorMin;
                rect.anchorMax = anchorMax;
                return;
            }

            Vector2 size = rect.rect.size;
            Vector2 pivotPosition = rect.localPosition;

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;

            Rect parentRect = parent.rect;
            Vector2 anchorRefMin = parentRect.min + Vector2.Scale(parentRect.size, anchorMin);
            Vector2 anchorRefMax = parentRect.min + Vector2.Scale(parentRect.size, anchorMax);

            rect.sizeDelta = size - (anchorRefMax - anchorRefMin);
            rect.anchoredPosition = pivotPosition - (anchorRefMin + Vector2.Scale(anchorRefMax - anchorRefMin, rect.pivot));
        }

        /// <summary>Moves the pivot without moving the rect, by walking the anchored position the same way.</summary>
        private static void SetPivotSmart(RectTransform rect, Vector2 pivot, bool raw)
        {
            if (raw)
            {
                rect.pivot = pivot;
                return;
            }

            Vector2 size = rect.rect.size;
            Vector2 delta = pivot - rect.pivot;

            rect.pivot = pivot;
            rect.anchoredPosition += Vector2.Scale(delta, size);
        }

        /// <param name="property">Backing property, for the override bar and the revert menu.</param>
        /// <param name="secondary">
        /// A second property the field also writes. The edge distances are stored as neither one thing
        /// nor the other — Left moves the anchored position and the size together — so both are opened,
        /// and reverting either from the row is honest about what the number came from.
        /// </param>
        private void FloatColumn(Rect row, int column, GUIContent label, int drivenFlag,
            Func<RectTransform, float> getter, Action<RectTransform, float> setter,
            SerializedProperty property, SerializedProperty secondary = null)
        {
            Rect position = GetColumnRect(row, column);
            float line = EditorGUIUtility.singleLineHeight;

            Rect labelRect = new Rect(position.x, position.y, position.width, line);
            Rect fieldRect = new Rect(position.x, position.y + line, position.width, line);

            float value = target is RectTransform first ? getter(first) : 0f;
            bool mixed = false;

            foreach (Object obj in targets)
            {
                if (obj is RectTransform rect && !Mathf.Approximately(getter(rect), value)) mixed = true;
            }

            if (property != null) EditorGUI.BeginProperty(position, label, property);
            if (secondary != null) EditorGUI.BeginProperty(position, label, secondary);

            using (new EditorGUI.DisabledScope(IsDriven(drivenFlag)))
            {
                int id = GUIUtility.GetControlID(FloatFieldHash, FocusType.Keyboard, fieldRect);
                EditorGUI.HandlePrefixLabel(position, labelRect, label, id);

                EditorGUI.showMixedValue = mixed;
                EditorGUI.BeginChangeCheck();

                float newValue = EditorGUI.FloatField(fieldRect, DragScrub(labelRect, id, value));

                EditorGUI.showMixedValue = false;

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObjects(targets, UndoName);

                    foreach (Object obj in targets)
                    {
                        if (obj is RectTransform rect) setter(rect, newValue);
                    }

                    serializedObject.Update();
                }
            }

            if (secondary != null) EditorGUI.EndProperty();
            if (property != null) EditorGUI.EndProperty();
        }

        /// <summary>
        /// One labelled row carrying an X and a Y in the first two columns of the same grid the position
        /// rows use, so the numbers line up down the inspector. The row is opened as a property so the
        /// prefab override bar and the revert menu land on it the way they do on Unity's own rows.
        /// </summary>
        /// <summary>
        /// Dragging a label sideways to change its number. Every other row here gets it for free from
        /// the field it is drawn with — these two rows put their label above rather than beside, and
        /// the public float field has no way to be told that the label above is its drag zone, so the
        /// drag is run by hand: same sensitivity curve Unity uses, so it feels like the rows around it.
        /// </summary>
        private static float DragScrub(Rect labelRect, int id, float value)
        {
            Event evt = Event.current;
            EditorGUIUtility.AddCursorRect(labelRect, MouseCursor.SlideArrow);

            switch (evt.GetTypeForControl(id))
            {
                case EventType.MouseDown when evt.button == 0 && labelRect.Contains(evt.mousePosition):
                    GUIUtility.hotControl = id;
                    EditorGUIUtility.SetWantsMouseJumping(1);
                    evt.Use();
                    break;

                case EventType.MouseDrag when GUIUtility.hotControl == id:
                    value += HandleUtility.niceMouseDelta * DragSensitivity(value);
                    GUI.changed = true;
                    evt.Use();
                    break;

                case EventType.MouseUp when GUIUtility.hotControl == id:
                    GUIUtility.hotControl = 0;
                    EditorGUIUtility.SetWantsMouseJumping(0);
                    evt.Use();
                    break;
            }

            return value;
        }

        /// <summary>Steps grow with the number being dragged, so 1000 moves as readily as 1.</summary>
        private static float DragSensitivity(float value) => Mathf.Max(1f, Mathf.Pow(Mathf.Abs(value), 0.5f)) * 0.03f;

        private void Vector2Row(GUIContent label, SerializedProperty property, int drivenX, int drivenY,
            Func<RectTransform, Vector2> getter, Action<RectTransform, Vector2> setter)
        {
            Rect row = EditorGUILayout.GetControlRect();
            GUIContent shown = property != null ? EditorGUI.BeginProperty(row, label, property) : label;

            // Taken before the label is drawn: the prefix hands its clicks and its drag to this id, so
            // the two have to agree or the label goes dead.
            int id = GUIUtility.GetControlID(FloatFieldHash, FocusType.Keyboard, row);

            // The rect it hands back is deliberately dropped: the columns are measured off the whole
            // row, the same as the position fields above, which is what keeps the two sets aligned.
            EditorGUI.PrefixLabel(row, id, shown);

            Vector2 value = target is RectTransform first ? getter(first) : Vector2.zero;
            Vector2 newValue = value;

            // Measured before the label width is narrowed for the X and Y prefixes: the columns are
            // derived from that same width, so narrowing first would slide them left over the label.
            Rect columnX = GetColumnRect(row, 0);
            Rect columnY = GetColumnRect(row, 1);

            int indent = EditorGUI.indentLevel;
            float labelWidth = EditorGUIUtility.labelWidth;

            EditorGUI.indentLevel = 0;
            EditorGUIUtility.labelWidth = AxisLabelWidth;

            EditorGUI.BeginChangeCheck();

            using (new EditorGUI.DisabledScope(IsDriven(drivenX)))
            {
                EditorGUI.showMixedValue = HasMixed(getter, v => v.x);
                newValue.x = EditorGUI.FloatField(columnX, "X", value.x);
            }

            using (new EditorGUI.DisabledScope(IsDriven(drivenY)))
            {
                EditorGUI.showMixedValue = HasMixed(getter, v => v.y);
                newValue.y = EditorGUI.FloatField(columnY, "Y", value.y);
            }

            EditorGUI.showMixedValue = false;

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObjects(targets, UndoName);

                foreach (Object obj in targets)
                {
                    if (obj is RectTransform rect) setter(rect, newValue);
                }

                serializedObject.Update();
            }

            EditorGUI.indentLevel = indent;
            EditorGUIUtility.labelWidth = labelWidth;

            if (property != null) EditorGUI.EndProperty();
        }

        private bool HasMixed(Func<RectTransform, Vector2> getter, Func<Vector2, float> axis)
        {
            if (!(target is RectTransform first)) return false;

            float value = axis(getter(first));

            foreach (Object obj in targets)
            {
                if (obj is RectTransform rect && !Mathf.Approximately(axis(getter(rect)), value)) return true;
            }

            return false;
        }

        /// <summary>The three-column grid the position and size rows sit in, matching Unity's spacing.</summary>
        private static Rect GetColumnRect(Rect totalRect, int column)
        {
            totalRect.xMin += EditorGUIUtility.labelWidth - 1f;

            Rect rect = totalRect;
            rect.xMin += (totalRect.width - 4f) * (column / 3f) + column * 2f;
            rect.width = (totalRect.width - 4f) / 3f;

            return rect;
        }

        private Vector3 GetCommonValue(Func<RectTransform, Vector3> selector, out bool mixed)
        {
            Vector3 first = target is RectTransform firstRect ? selector(firstRect) : Vector3.zero;
            mixed = false;

            foreach (Object obj in targets)
            {
                if (!(obj is RectTransform rect)) continue;

                if (selector(rect) != first)
                {
                    mixed = true;
                    break;
                }
            }

            return first;
        }

        private static void ResolveReflection()
        {
            if (_reflectionResolved) return;
            _reflectionResolved = true;

            _nativeSceneGui = NativeEditorType?.GetMethod("OnSceneGUI", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            _drivenProperties = typeof(RectTransform).GetProperty("drivenProperties", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            _layoutDropdownType = typeof(Editor).Assembly.GetType("UnityEditor.LayoutDropdownWindow");
            if (_layoutDropdownType == null) return;

            Type[] signature = { typeof(Rect), typeof(SerializedProperty), typeof(SerializedProperty), typeof(SerializedProperty), typeof(SerializedProperty) };
            const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

            _drawLayoutMode = _layoutDropdownType.GetMethod("DrawLayoutMode", flags, null, signature, null);
            _drawLayoutHeaders = _layoutDropdownType.GetMethod("DrawLayoutModeHeadersOutsideRect", flags, null, signature, null);
        }
    }
}
