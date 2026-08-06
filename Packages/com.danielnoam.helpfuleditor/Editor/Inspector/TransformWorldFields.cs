using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNExtensions.HelpfulEditor.Inspector
{
    /// <summary>
    /// The world position, rotation and scale block drawn under the local values, shared by the
    /// Transform and RectTransform inspectors.
    ///
    /// Plain Vector3 rows rather than the linked field the local ones use. These are a read-out with
    /// editing attached, not the object's primary controls, and giving them the same copy/paste/reset
    /// chrome would make the two sets look interchangeable.
    ///
    /// Held per inspector rather than statically: the rotation row remembers the angles last typed
    /// into it, and that memory belongs to the object being looked at.
    /// </summary>
    internal class TransformWorldFields
    {
        private Vector3 _eulerDisplay;
        private Quaternion _eulerSource;
        private bool _eulerValid;

        /// <summary>
        /// Drops the remembered angles. Editors are pooled and handed a new target, and the angles are
        /// keyed only on the rotation that produced them — so without this, typing 370 on one object and
        /// then selecting another at the same world rotation shows 370 for that one too. Identity makes
        /// it easy to hit: every object that has never been rotated shares it.
        /// </summary>
        public void Forget() => _eulerValid = false;

        /// <summary>
        /// Whether the block is worth drawing at all. Any target, not every one — a mixed selection
        /// still has world values worth showing, while a root object's local values already are its
        /// world values, so the second set would be an exact copy of the first sitting beneath it.
        /// </summary>
        public static bool AnyParented(Object[] targets)
        {
            foreach (Object obj in targets)
            {
                if (obj is Transform transform && transform.parent) return true;
            }

            return false;
        }

        public void Draw(SerializedObject serializedObject, Transform main, Object[] targets)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("World", EditorStyles.miniBoldLabel);

            DrawPosition(serializedObject, targets);
            DrawRotation(serializedObject, main, targets);
            DrawScale(targets);
        }

        private static void DrawPosition(SerializedObject serializedObject, Object[] targets)
        {
            Vector3 displayValue = GetCommonValue(targets, t => t.position, out bool mixed);
            bool unusedLock = false;

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = mixed;

            Vector3 newValue = LinkedVector3Field.Draw("Position", displayValue, Vector3.zero, false, ref unusedLock,
                extraResetItems: HelpfulEditorSettings.Inspector.resetMenuItemsEnabled
                    ? menu => TransformResetMenu.Build(menu, targets, (t, v) => t.position = v, Vector3.zero)
                    : null);

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
        /// jump about while editing. The angles last typed here are kept and redisplayed for as long as
        /// the rotation they produced is still the one on the object, which is what
        /// m_LocalEulerAnglesHint does for the local field.
        /// </summary>
        private void DrawRotation(SerializedObject serializedObject, Transform main, Object[] targets)
        {
            if (!main) return;

            Vector3 displayEuler = GetCommonValue(targets, t => t.eulerAngles, out bool mixed);

            if (!mixed && _eulerValid && main.rotation == _eulerSource) displayEuler = _eulerDisplay;

            Quaternion quaternion = main.rotation;
            bool unusedLock = false;

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = mixed;

            Vector3 newEuler = LinkedVector3Field.Draw("Rotation", displayEuler, Vector3.zero, false, ref unusedLock,
                extraContextItems: menu => menu.AddItem(new GUIContent("Copy Quaternion"), false, () =>
                    EditorGUIUtility.systemCopyBuffer = $"{quaternion.x},{quaternion.y},{quaternion.z},{quaternion.w}"),
                extraResetItems: HelpfulEditorSettings.Inspector.resetMenuItemsEnabled
                    ? menu => TransformResetMenu.Build(menu, targets, (t, v) => t.eulerAngles = v, Vector3.zero)
                    : null);

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

            _eulerDisplay = newEuler;
            _eulerSource = main.rotation;
            _eulerValid = true;
        }

        /// <summary>
        /// Read-only, because lossyScale has no setter and cannot be given a correct one: under a parent
        /// that is both rotated and unevenly scaled the world scale is a sheared matrix that no single
        /// local scale reproduces, so a writable field would quietly store something other than what was
        /// typed. Shown anyway — knowing what an object ended up at is most of why the world values are
        /// wanted — and copyable, which is the part that was actually being asked of it.
        /// </summary>
        private static void DrawScale(Object[] targets)
        {
            Vector3 displayValue = GetCommonValue(targets, t => t.lossyScale, out bool mixed);

            EditorGUI.showMixedValue = mixed;
            LinkedVector3Field.DrawReadOnly("Scale", displayValue);
            EditorGUI.showMixedValue = false;
        }

        private static Vector3 GetCommonValue(Object[] targets, Func<Transform, Vector3> selector, out bool mixed)
        {
            Vector3 first = targets.Length > 0 && targets[0] is Transform firstTransform ? selector(firstTransform) : Vector3.zero;
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
