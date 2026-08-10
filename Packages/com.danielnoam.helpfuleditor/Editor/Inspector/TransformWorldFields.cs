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
    /// All three rows are editable and carry the same chrome the local rows do — copy, paste, reset,
    /// and a proportional lock on the scale row — so a value can be moved between the two sets and
    /// either set can be dragged the same way.
    ///
    /// Held per inspector rather than statically: the rotation row remembers the angles last typed
    /// into it, and that memory belongs to the object being looked at.
    /// </summary>
    internal class TransformWorldFields
    {
        private Vector3 _eulerDisplay;
        private Quaternion _eulerSource;
        private bool _eulerValid;

        private Vector3 _lastScale;
        private bool _lastScaleValid;

        /// <summary>
        /// Drops what the rows remember about the object they were last drawn for.
        ///
        /// The angles are the reason this exists: editors are pooled and handed a new target, and the
        /// angles are keyed only on the rotation that produced them — so without this, typing 370 on
        /// one object and then selecting another at the same world rotation shows 370 for that one
        /// too. Identity makes it easy to hit, since every object that has never been rotated shares
        /// it. The scale row's lock reference goes for the same reason.
        /// </summary>
        public void Forget()
        {
            _eulerValid = false;
            _lastScaleValid = false;
        }

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
            DrawScale(serializedObject, targets);
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
                    EditorGUIUtility.systemCopyBuffer = LinkedVector3Field.FormatQuaternion(quaternion)),
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
        /// Written by dividing the parent's scale out of what was typed — see <see cref="SetLossyScale"/>
        /// for the one case that cannot be expressed exactly.
        ///
        /// Carries a proportional lock of its own, which holds the object's proportions as they are
        /// seen in the world rather than as they are stored. Under a non-uniformly scaled parent the
        /// two are different shapes, so holding one is not holding the other — and a locked drag here
        /// deliberately produces an unlocked-looking change in the local row below, because that is
        /// what keeping the world shape costs.
        /// </summary>
        private void DrawScale(SerializedObject serializedObject, Object[] targets)
        {
            Vector3 displayValue = GetCommonValue(targets, t => t.lossyScale, out bool mixed);

            // The lock scales against the row's own last committed value, which is what lets a
            // continuous drag compound step by step instead of every frame restarting from the
            // shape the object began the drag at. There is no such value on the first draw for a
            // target, so it starts from what is already on screen.
            if (!_lastScaleValid)
            {
                _lastScale = displayValue;
                _lastScaleValid = true;
            }

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = mixed;

            Vector3 newValue = LinkedVector3Field.Draw("Scale", displayValue, Vector3.one, true, ref ScaleLock.World.locked,
                extraResetItems: HelpfulEditorSettings.Inspector.resetMenuItemsEnabled
                    ? menu => TransformResetMenu.Build(menu, targets, SetLossyScale, Vector3.one)
                    : null);

            EditorGUI.showMixedValue = false;

            if (!EditorGUI.EndChangeCheck()) return;

            if (ScaleLock.World.locked) newValue = LinkedVector3Field.ApplyLock(displayValue, newValue, _lastScale);

            Vector3 delta = newValue - displayValue;
            Undo.RecordObjects(targets, "World Scale Changed");

            foreach (Object obj in targets)
            {
                if (obj is Transform transform) SetLossyScale(transform, mixed ? transform.lossyScale + delta : newValue);
            }

            _lastScale = newValue;

            serializedObject.Update();
        }

        /// <summary>
        /// Puts a transform at a world scale by dividing out its parent's.
        ///
        /// Exact wherever lossyScale is itself exact, which is everywhere no ancestor combines a
        /// rotation with a non-uniform scale. Under one that does, the true world scale is a sheared
        /// matrix that no single local scale reproduces — Unity's lossyScale is already an
        /// approximation of it there, and what reads back after a write is the nearest thing the
        /// object can actually hold rather than the number typed. That is the reason this row was
        /// read-only until it was asked for; the field is honest about the common case and lossy in
        /// exactly the case lossyScale is named for.
        ///
        /// An axis whose parent scale is zero keeps the local value it had: every world scale on that
        /// axis is zero regardless, so there is nothing to solve for and no reason to disturb it.
        /// </summary>
        public static void SetLossyScale(Transform transform, Vector3 lossyScale)
        {
            if (!transform) return;

            Transform parent = transform.parent;
            Vector3 parentScale = parent ? parent.lossyScale : Vector3.one;
            Vector3 local = transform.localScale;

            transform.localScale = new Vector3(
                Mathf.Approximately(parentScale.x, 0f) ? local.x : lossyScale.x / parentScale.x,
                Mathf.Approximately(parentScale.y, 0f) ? local.y : lossyScale.y / parentScale.y,
                Mathf.Approximately(parentScale.z, 0f) ? local.z : lossyScale.z / parentScale.z);
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
