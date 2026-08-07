using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DNExtensions.HelpfulEditor.Viewport
{
    /// <summary>
    /// The overlapping-object picker. Gathers everything under the cursor on the bound chord and
    /// hands it to a window to choose from, taking that gesture away from whatever menu the editor
    /// would otherwise raise on it. The window is a window rather than a GenericMenu so a row can
    /// preview itself in the Scene View while the cursor is over it, which a menu has no way to do.
    /// </summary>
    internal static class SceneViewPicker
    {
        private static readonly List<GameObject> Candidates = new List<GameObject>();
        private static readonly Vector3[] CornerBuffer = new Vector3[4];
        private static readonly Vector3[] LoopBuffer = new Vector3[5];

        private static bool _swallowClick;
        private static int _swallowButton;

        /// <summary>The row the picker window is currently hovering, previewed in every Scene View.</summary>
        public static GameObject HoverTarget { get; private set; }

        public static void SetHoverTarget(GameObject target)
        {
            if (HoverTarget == target) return;

            HoverTarget = target;
            SceneView.RepaintAll();
        }

        public static void Process(SceneView sceneView, SceneViewSettings settings)
        {
            if (settings.pickerHighlightEnabled) DrawHighlight(settings);
            if (!settings.pickerEnabled) return;

            HandleTrigger(sceneView, settings);
        }

        private static void HandleTrigger(SceneView sceneView, SceneViewSettings settings)
        {
            Event evt = Event.current;
            if (evt == null) return;

            if (_swallowClick && ConsumeSwallowed(evt)) return;
            if (!settings.pickerKey.Matches(evt)) return;

            SceneViewPicking.Gather(evt.mousePosition, settings.pickerMaxResults, Candidates);
            if (Candidates.Count == 0) return;

            // Copied out because the shared buffer is refilled by the next pick, and the window
            // outlives this event by design.
            List<GameObject> picked = new List<GameObject>(Candidates);
            Vector2 screenPosition = GUIUtility.GUIToScreenPoint(evt.mousePosition);

            // Always suppressed: the picker answers the same question the editor's own menu does, and
            // two menus for one click is never what was wanted.
            _swallowClick = true;
            _swallowButton = evt.button;
            evt.Use();

            // ShowAsDropDown tears down and rebuilds focus, which is not safe to do from inside the
            // Scene View's own GUI pass — the same reason the Hierarchy's quick edit defers.
            EditorApplication.delayCall += () => SceneViewPickerWindow.Open(picked, screenPosition, sceneView);
        }

        /// <summary>
        /// Unity selects on the release rather than the press, so consuming only the MouseDown would
        /// still leave the click selecting whatever happened to be on top. The rest of the gesture is
        /// eaten here, including the drag, which would otherwise start a box select.
        /// </summary>
        private static bool ConsumeSwallowed(Event evt)
        {
            // A press arriving while a previous gesture is still being swallowed means its release
            // went somewhere else entirely. Dropping the stale flag keeps it from eating this one.
            if (evt.type == EventType.MouseDown)
            {
                _swallowClick = false;
                return false;
            }

            // The right-click menu is raised as its own event rather than as part of the click, so
            // eating the button alone still leaves it to open behind the picker. Its button field is
            // not dependable across platforms, hence no check against the one being swallowed.
            if (evt.type == EventType.ContextClick)
            {
                evt.Use();
                return true;
            }

            if (evt.button != _swallowButton) return false;

            switch (evt.type)
            {
                case EventType.MouseDrag:
                    evt.Use();
                    return true;

                case EventType.MouseUp:
                    _swallowClick = false;
                    evt.Use();
                    return true;

                default:
                    return false;
            }
        }

        private static void DrawHighlight(SceneViewSettings settings)
        {
            if (Event.current == null || Event.current.type != EventType.Repaint) return;

            GameObject target = HoverTarget;
            if (!target) return;

            Color previousColor = Handles.color;
            CompareFunction previousZTest = Handles.zTest;

            Handles.color = settings.pickerHighlightColor;

            // Drawn through geometry on purpose: the whole point is telling apart objects that are
            // stacked, and the ones worth previewing are exactly the ones hidden behind something.
            Handles.zTest = CompareFunction.Always;

            if (target.TryGetComponent(out RectTransform rectTransform)) DrawRectOutline(rectTransform);
            else if (TryGetBounds(target, out Bounds bounds)) Handles.DrawWireCube(bounds.center, bounds.size);
            else DrawPoint(target.transform.position);

            Handles.color = previousColor;
            Handles.zTest = previousZTest;
        }

        /// <summary>UI objects have no renderer bounds worth drawing, but their rect is exactly the shape wanted.</summary>
        private static void DrawRectOutline(RectTransform rectTransform)
        {
            rectTransform.GetWorldCorners(CornerBuffer);

            for (int i = 0; i < CornerBuffer.Length; i++) LoopBuffer[i] = CornerBuffer[i];
            LoopBuffer[4] = CornerBuffer[0];

            Handles.DrawPolyLine(LoopBuffer);
        }

        /// <summary>
        /// Children are included: picking lands on whichever renderer was hit, which is often a leaf
        /// of the thing the row names, and a box around only that leaf reads as the wrong object.
        /// </summary>
        private static bool TryGetBounds(GameObject target, out Bounds bounds)
        {
            bounds = default;
            bool found = false;

            foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>())
            {
                if (!renderer || !renderer.enabled) continue;

                if (found) bounds.Encapsulate(renderer.bounds);
                else
                {
                    bounds = renderer.bounds;
                    found = true;
                }
            }

            if (found) return true;

            foreach (Collider collider in target.GetComponentsInChildren<Collider>())
            {
                if (!collider) continue;

                if (found) bounds.Encapsulate(collider.bounds);
                else
                {
                    bounds = collider.bounds;
                    found = true;
                }
            }

            return found;
        }

        /// <summary>Marker for objects with nothing to measure — an empty transform still needs to be findable.</summary>
        private static void DrawPoint(Vector3 position)
        {
            Handles.SphereHandleCap(0, position, Quaternion.identity, HandleUtility.GetHandleSize(position) * 0.15f, EventType.Repaint);
        }
    }
}
