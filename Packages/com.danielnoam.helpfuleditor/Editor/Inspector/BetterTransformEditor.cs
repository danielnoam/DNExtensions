using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNExtensions.HelpfulEditor.Inspector
{
    [CustomEditor(typeof(Transform))]
    [CanEditMultipleObjects]
    internal class BetterTransformEditor : Editor
    {
        private const string EulerHintProperty = "m_LocalEulerAnglesHint";

        private static readonly Type NativeEditorType = typeof(Editor).Assembly.GetType("UnityEditor.TransformInspector");

        private static bool _scaleLocked;
        private static bool _scaleLockInitialized;
        private static bool _scaleLockDefault;

        private Editor _nativeEditor;
        private Vector3 _lastScale;

        private Vector3 _worldEulerDisplay;
        private Quaternion _worldEulerSource;
        private bool _worldEulerValid;

        private void OnEnable()
        {
            if (target is Transform transform) _lastScale = transform.localScale;

            // Editors are pooled and handed a new target, and the cached angles are keyed only on the
            // rotation they produced — so without this, typing 370 on one object and then selecting
            // another at the same world rotation shows 370 for that one too. Identity makes it easy
            // to hit: every object that has never been rotated shares it.
            _worldEulerValid = false;

            // The lock is deliberately shared across inspectors and kept across selections, so it is
            // only seeded from the setting once. Comparing against the last seeded value is what
            // makes changing that setting take effect without waiting for a recompile.
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

        public override void OnInspectorGUI()
        {
            InspectorSettings settings = HelpfulEditorSettings.Inspector;

            if (!settings.moduleEnabled || !settings.betterTransformEnabled)
            {
                DrawNativeInspector();
                return;
            }

            serializedObject.Update();

            // The Local header only earns its place once there is a World group to tell it apart
            // from. On a root object it would label the only three rows there are.
            bool showWorld = ShowWorldFields(settings);
            if (showWorld) EditorGUILayout.LabelField("Local", EditorStyles.miniBoldLabel);

            DrawPosition(settings);
            DrawRotation(settings);
            DrawScale(settings);

            if (showWorld) DrawWorld();

            if (serializedObject.ApplyModifiedProperties())
            {
                foreach (Object obj in targets)
                {
                    if (obj is Transform transform) EditorUtility.SetDirty(transform);
                }
            }
        }

        private void DrawNativeInspector()
        {
            if (!_nativeEditor && NativeEditorType != null) _nativeEditor = CreateEditor(targets, NativeEditorType);

            if (_nativeEditor) _nativeEditor.OnInspectorGUI();
            else DrawDefaultInspector();
        }

        private void DrawPosition(InspectorSettings settings)
        {
            if (serializedObject.FindProperty("m_LocalPosition") == null) return;

            Vector3 displayValue = GetCommonValue(t => t.localPosition, out bool mixed);
            bool unusedLock = false;

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = mixed;

            Vector3 newValue = LinkedVector3Field.Draw("Position", displayValue, Vector3.zero, false, ref unusedLock,
                extraResetItems: settings.resetMenuItemsEnabled
                    ? menu => BuildResetMenu(menu, (t, v) => t.localPosition = v, Vector3.zero)
                    : null);

            EditorGUI.showMixedValue = false;

            if (!EditorGUI.EndChangeCheck()) return;

            Vector3 delta = newValue - displayValue;
            Undo.RecordObjects(targets, "Position Changed");

            foreach (Object obj in targets)
            {
                if (obj is Transform transform) transform.localPosition = mixed ? transform.localPosition + delta : newValue;
            }

            serializedObject.Update();
        }

        /// <summary>
        /// Rotation is shown from Unity's own euler hint rather than from localEulerAngles.
        ///
        /// localEulerAngles is derived from the quaternion, so it is one of several equivalent
        /// answers — type 370 and it reads back 10, and values jump about while editing near gimbal
        /// configurations. Unity's Transform inspector keeps m_LocalEulerAnglesHint for exactly this
        /// reason, and replacing that inspector without carrying the hint forward loses the property
        /// most people would notice first.
        /// </summary>
        private void DrawRotation(InspectorSettings settings)
        {
            if (!(target is Transform main)) return;

            SerializedProperty hint = serializedObject.FindProperty(EulerHintProperty);

            Vector3 displayEuler;
            bool mixed;

            if (hint != null)
            {
                displayEuler = hint.vector3Value;
                mixed = hint.hasMultipleDifferentValues;
            }
            else
            {
                displayEuler = GetCommonValue(t => t.localEulerAngles, out mixed);
            }

            Quaternion quaternion = main.localRotation;
            bool unusedLock = false;

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = mixed;

            Vector3 newEuler = LinkedVector3Field.Draw("Rotation", displayEuler, Vector3.zero, false, ref unusedLock,
                extraContextItems: menu => menu.AddItem(new GUIContent("Copy Quaternion"), false, () =>
                    EditorGUIUtility.systemCopyBuffer = $"{quaternion.x},{quaternion.y},{quaternion.z},{quaternion.w}"),
                extraResetItems: settings.resetMenuItemsEnabled
                    ? menu => BuildResetMenu(menu, (t, v) => t.localEulerAngles = v, Vector3.zero)
                    : null);

            EditorGUI.showMixedValue = false;

            if (!EditorGUI.EndChangeCheck()) return;

            Vector3 delta = newEuler - displayEuler;
            Undo.RecordObjects(targets, "Rotation Changed");

            foreach (Object obj in targets)
            {
                if (obj is Transform transform) transform.localEulerAngles = mixed ? transform.localEulerAngles + delta : newEuler;
            }

            serializedObject.Update();

            // Written back explicitly rather than trusting the setter to have maintained it, so the
            // number just typed is the number that reads back on the next repaint.
            if (hint != null && !mixed)
            {
                hint.vector3Value = newEuler;
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void DrawScale(InspectorSettings settings)
        {
            if (serializedObject.FindProperty("m_LocalScale") == null) return;

            Vector3 displayValue = GetCommonValue(t => t.localScale, out bool mixed);

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = mixed;

            Vector3 newValue = LinkedVector3Field.Draw("Scale", displayValue, Vector3.one, true, ref _scaleLocked,
                extraResetItems: settings.resetMenuItemsEnabled
                    ? menu => BuildResetMenu(menu, (t, v) => t.localScale = v, Vector3.one)
                    : null);

            EditorGUI.showMixedValue = false;

            if (!EditorGUI.EndChangeCheck()) return;

            if (_scaleLocked) newValue = LinkedVector3Field.ApplyLock(displayValue, newValue, _lastScale);

            Vector3 delta = newValue - displayValue;
            Undo.RecordObjects(targets, "Scale Changed");

            foreach (Object obj in targets)
            {
                if (obj is Transform transform) transform.localScale = mixed ? transform.localScale + delta : newValue;
            }

            _lastScale = newValue;
            serializedObject.Update();
        }

        /// <summary>
        /// The same three values in world space, below the local ones. Only drawn under a parent:
        /// a root object's local values already are its world values, so the second set would be an
        /// exact copy of the first sitting directly beneath it.
        ///
        /// Plain Vector3 rows rather than the linked field the local ones use. These are a read-out
        /// with editing attached, not the object's primary controls, and giving them the same
        /// copy/paste/reset chrome would make the two sets look interchangeable.
        /// </summary>
        private void DrawWorld()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("World", EditorStyles.miniBoldLabel);

            DrawWorldPosition();
            DrawWorldRotation();
            DrawWorldScale();
        }

        private bool ShowWorldFields(InspectorSettings settings) => settings.worldFieldsEnabled && HasParent();

        /// <summary>Any target, not every one — a mixed selection still has world values worth showing.</summary>
        private bool HasParent()
        {
            foreach (Object obj in targets)
            {
                if (obj is Transform transform && transform.parent) return true;
            }

            return false;
        }

        private void DrawWorldPosition()
        {
            Vector3 displayValue = GetCommonValue(t => t.position, out bool mixed);

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = mixed;

            Vector3 newValue = EditorGUILayout.Vector3Field("Position", displayValue);

            EditorGUI.showMixedValue = false;

            if (!EditorGUI.EndChangeCheck()) return;

            Vector3 delta = newValue - displayValue;
            Undo.RecordObjects(targets, "World Position Changed");

            foreach (Object obj in targets)
            {
                if (obj is Transform transform) transform.position = mixed ? transform.position + delta : newValue;
            }

            serializedObject.Update();
        }

        /// <summary>
        /// Carries the same trap the local rotation does, without a serialized hint to lean on: world
        /// euler is read back off the quaternion, so typing 370 would return 10 and the numbers would
        /// jump about while editing. The angles last typed here are kept and redisplayed for as long
        /// as the rotation they produced is still the one on the object, which is what
        /// m_LocalEulerAnglesHint does for the local field.
        /// </summary>
        private void DrawWorldRotation()
        {
            if (!(target is Transform main)) return;

            Vector3 displayEuler = GetCommonValue(t => t.eulerAngles, out bool mixed);

            if (!mixed && _worldEulerValid && main.rotation == _worldEulerSource) displayEuler = _worldEulerDisplay;

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = mixed;

            Vector3 newEuler = EditorGUILayout.Vector3Field("Rotation", displayEuler);

            EditorGUI.showMixedValue = false;

            if (!EditorGUI.EndChangeCheck()) return;

            Vector3 delta = newEuler - displayEuler;
            Undo.RecordObjects(targets, "World Rotation Changed");

            foreach (Object obj in targets)
            {
                if (obj is Transform transform) transform.eulerAngles = mixed ? transform.eulerAngles + delta : newEuler;
            }

            serializedObject.Update();

            if (mixed) return;

            _worldEulerDisplay = newEuler;
            _worldEulerSource = main.rotation;
            _worldEulerValid = true;
        }

        /// <summary>
        /// Read-only, because lossyScale has no setter and cannot be given a correct one: under a
        /// rotated parent the world scale is a sheared matrix that no single local scale reproduces,
        /// so a writable field would quietly store something other than what was typed. Shown anyway
        /// — knowing what an object ended up at is most of why the world values are wanted.
        /// </summary>
        private void DrawWorldScale()
        {
            Vector3 displayValue = GetCommonValue(t => t.lossyScale, out bool mixed);

            EditorGUI.showMixedValue = mixed;

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Vector3Field("Scale", displayValue);
            }

            EditorGUI.showMixedValue = false;
        }

        private void BuildResetMenu(GenericMenu menu, Action<Transform, Vector3> setter, Vector3 resetValue)
        {
            menu.AddItem(new GUIContent("Reset"), false, () =>
            {
                foreach (Object obj in targets)
                {
                    if (!(obj is Transform transform)) continue;

                    Undo.RecordObject(transform, "Reset");
                    setter(transform, resetValue);
                }
            });

            menu.AddItem(new GUIContent("Reset Without Children"), false, () =>
            {
                foreach (Object obj in targets)
                {
                    if (obj is Transform transform) ResetWithoutChildren(transform, setter, resetValue);
                }
            });

            menu.AddItem(new GUIContent("Reset Only Children"), false, () =>
            {
                foreach (Object obj in targets)
                {
                    if (obj is Transform transform) ResetOnlyChildren(transform, setter, resetValue);
                }
            });
        }

        private static void ResetWithoutChildren(Transform transform, Action<Transform, Vector3> setter, Vector3 resetValue)
        {
            int childCount = transform.childCount;
            Vector3[] worldPositions = new Vector3[childCount];
            Quaternion[] worldRotations = new Quaternion[childCount];
            Vector3[] worldScales = new Vector3[childCount];

            for (int i = 0; i < childCount; i++)
            {
                Transform child = transform.GetChild(i);
                worldPositions[i] = child.position;
                worldRotations[i] = child.rotation;
                worldScales[i] = child.lossyScale;
            }

            Undo.RecordObject(transform, "Reset Without Children");
            for (int i = 0; i < childCount; i++)
            {
                Undo.RecordObject(transform.GetChild(i), "Reset Without Children");
            }

            setter(transform, resetValue);

            for (int i = 0; i < childCount; i++)
            {
                Transform child = transform.GetChild(i);
                child.position = worldPositions[i];
                child.rotation = worldRotations[i];
                SetLossyScale(child, worldScales[i]);
            }
        }

        private static void ResetOnlyChildren(Transform transform, Action<Transform, Vector3> setter, Vector3 resetValue)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                Undo.RecordObject(child, "Reset Only Children");
                setter(child, resetValue);
            }
        }

        private static void SetLossyScale(Transform transform, Vector3 targetLossyScale)
        {
            Transform parent = transform.parent;
            Vector3 parentScale = parent ? parent.lossyScale : Vector3.one;

            transform.localScale = new Vector3(
                parentScale.x != 0f ? targetLossyScale.x / parentScale.x : 1f,
                parentScale.y != 0f ? targetLossyScale.y / parentScale.y : 1f,
                parentScale.z != 0f ? targetLossyScale.z / parentScale.z : 1f
            );
        }

        private Vector3 GetCommonValue(Func<Transform, Vector3> selector, out bool mixed)
        {
            Vector3 first = target is Transform firstTransform ? selector(firstTransform) : Vector3.zero;
            mixed = false;

            foreach (Object obj in targets)
            {
                if (!(obj is Transform transform)) continue;

                if (selector(transform) != first)
                {
                    mixed = true;
                    break;
                }
            }

            return first;
        }
    }
}
