using UnityEditor;
using UnityEngine;

namespace DNExtensions.HelpfulEditor.Viewport
{
    /// <summary>
    /// The Snap submenu on the Scene View's right-click menu.
    ///
    /// Unity 6 builds that menu in ContextMenuUtility.CreateActionMenu, which harvests menu items
    /// twice over. The component pass hangs "CONTEXT/&lt;Component&gt;/..." items under the component's
    /// own name, which is why anything registered against Transform lands a level down. The pass
    /// after it collects "CONTEXT/&lt;ActiveToolContextType&gt;/..." with an empty submenu instead, so
    /// those items sit at the top of the menu beside Grid and Isolate — that is the one channel that
    /// reaches the top level, and it needs nothing but the public MenuItem attribute.
    ///
    /// The context type is the active one, so these show under Unity's default GameObject context
    /// and stand aside if a custom tool context takes over, which is the behaviour Unity's own
    /// entries have. Editors before 6000.3 have no Scene View context menu at all, so there the
    /// entries fall back to Transform and appear in its gear menu.
    ///
    /// Unity invokes the item once per selected object, so each call snaps only its own transform
    /// and a multi-object selection falls out for free.
    /// </summary>
    internal static class SceneViewSnapMenu
    {
#if UNITY_6000_3_OR_NEWER
        private const string Root = "CONTEXT/GameObjectToolContext/Snap/";
#else
        private const string Root = "CONTEXT/Transform/Snap/";
#endif

        private const string FloorItem = Root + "Snap To Floor";
        private const string CeilingItem = Root + "Snap To Ceiling";
        private const string WallItem = Root + "Snap To Nearest Wall";

        private const string FloorPivotItem = Root + "Snap To Floor (Pivot)";
        private const string CeilingPivotItem = Root + "Snap To Ceiling (Pivot)";
        private const string WallPivotItem = Root + "Snap To Nearest Wall (Pivot)";

        // The gap is what puts a separator between the two groups: Unity draws one wherever
        // consecutive priorities differ by more than ten.
        private const int BoundsPriority = 0;
        private const int PivotPriority = 20;

        private static readonly Vector3[] Down = { Vector3.down };
        private static readonly Vector3[] Up = { Vector3.up };

        // A wall has no single direction to it, so all four are cast and the one needing the least
        // movement wins. World axes rather than the object's own: a rotated object should still
        // travel along the room, not along whatever way it happens to be facing.
        private static readonly Vector3[] Horizontal = { Vector3.left, Vector3.right, Vector3.forward, Vector3.back };

        /// <summary>
        /// What the snap puts on the surface. Bounds lands the object itself, which is what makes it
        /// sit on a floor. Pivot lands its origin, which is what places a pivot deliberately set at
        /// a contact point — a character's feet, or a prop's base — and is the only one that behaves
        /// for objects whose pivot sits outside their bounds entirely.
        /// </summary>
        private enum SnapAnchor
        {
            Bounds,
            Pivot
        }

        [MenuItem(FloorItem, true, BoundsPriority)]
        private static bool ValidateSnapToFloor() => IsEnabled();

        [MenuItem(FloorItem, false, BoundsPriority)]
        private static void SnapToFloor(MenuCommand command) => Snap(command, Down, SnapAnchor.Bounds, "Snap To Floor");

        [MenuItem(CeilingItem, true, BoundsPriority + 1)]
        private static bool ValidateSnapToCeiling() => IsEnabled();

        [MenuItem(CeilingItem, false, BoundsPriority + 1)]
        private static void SnapToCeiling(MenuCommand command) => Snap(command, Up, SnapAnchor.Bounds, "Snap To Ceiling");

        [MenuItem(WallItem, true, BoundsPriority + 2)]
        private static bool ValidateSnapToWall() => IsEnabled();

        [MenuItem(WallItem, false, BoundsPriority + 2)]
        private static void SnapToWall(MenuCommand command) => Snap(command, Horizontal, SnapAnchor.Bounds, "Snap To Nearest Wall");

        [MenuItem(FloorPivotItem, true, PivotPriority)]
        private static bool ValidateSnapPivotToFloor() => IsEnabled();

        [MenuItem(FloorPivotItem, false, PivotPriority)]
        private static void SnapPivotToFloor(MenuCommand command) => Snap(command, Down, SnapAnchor.Pivot, "Snap To Floor (Pivot)");

        [MenuItem(CeilingPivotItem, true, PivotPriority + 1)]
        private static bool ValidateSnapPivotToCeiling() => IsEnabled();

        [MenuItem(CeilingPivotItem, false, PivotPriority + 1)]
        private static void SnapPivotToCeiling(MenuCommand command) => Snap(command, Up, SnapAnchor.Pivot, "Snap To Ceiling (Pivot)");

        [MenuItem(WallPivotItem, true, PivotPriority + 2)]
        private static bool ValidateSnapPivotToWall() => IsEnabled();

        [MenuItem(WallPivotItem, false, PivotPriority + 2)]
        private static void SnapPivotToWall(MenuCommand command) => Snap(command, Horizontal, SnapAnchor.Pivot, "Snap To Nearest Wall (Pivot)");

        private static bool IsEnabled()
        {
            SceneViewSettings settings = HelpfulEditorSettings.SceneView;
            return settings.moduleEnabled && settings.snapMenuEnabled;
        }

        /// <summary>
        /// What the menu was invoked on. The tool context hands over its own targets rather than a
        /// component editor's, so this arrives as a GameObject as readily as a Transform, and as
        /// nothing at all if the menu was raised without one.
        /// </summary>
        private static Transform ResolveTarget(MenuCommand command)
        {
            return command.context switch
            {
                Transform transform => transform,
                GameObject gameObject => gameObject.transform,
                Component component => component.transform,
                _ => Selection.activeTransform
            };
        }

        /// <summary>
        /// Snaps to the first surface found across the candidate directions, ranked by how far the
        /// object has to move rather than by how far the ray travelled — the two differ because the
        /// bounds reach further along some axes than others, and it is the movement that is being
        /// minimised. Ranked on magnitude so an object already sunk into a wall is pushed back out by
        /// the shortest correction instead of being sent across the room.
        /// </summary>
        private static void Snap(MenuCommand command, Vector3[] directions, SnapAnchor anchor, string label)
        {
            Transform target = ResolveTarget(command);
            if (!target) return;

            SceneViewSettings settings = HelpfulEditorSettings.SceneView;
            Bounds bounds = WorldBounds(target);

            bool found = false;
            float shortest = 0f;
            Vector3 position = target.position;

            foreach (Vector3 direction in directions)
            {
                if (!TryResolveContact(target, bounds, anchor, direction, settings.snapMaxDistance, out Vector3 candidate, out float travel)) continue;

                travel = Mathf.Abs(travel);
                if (found && travel >= shortest) continue;

                shortest = travel;
                position = candidate;
                found = true;
            }

            if (!found)
            {
                Debug.LogWarning($"[HelpfulEditor] {label} found nothing to land {target.name} on within {settings.snapMaxDistance} units.", target);
                return;
            }

            Undo.RecordObject(target, label);
            target.position = position;
        }

        /// <summary>
        /// Where the transform has to sit for its leading edge to rest on the first surface in that
        /// direction. Which edge that is comes from the anchor: the far face of the bounds, or the
        /// pivot itself. In bounds mode the pivot keeps whatever offset it had from the centre, so an
        /// off-centre pivot lands the object correctly rather than burying or floating it.
        /// </summary>
        /// <param name="travel">How far the object moves to get there, negative when it backs out of a surface it is already inside.</param>
        private static bool TryResolveContact(Transform target, Bounds bounds, SnapAnchor anchor, Vector3 direction,
            float maxDistance, out Vector3 position, out float travel)
        {
            position = target.position;
            travel = 0f;

            // In bounds mode the ray starts at the centre rather than the pivot: a pivot at the base
            // of a mesh starts it on the very surface being looked for, which finds the object it is
            // already standing on. Pivot mode wants exactly that origin, since the pivot is the thing
            // being placed.
            Vector3 origin = anchor == SnapAnchor.Pivot ? target.position : bounds.center;
            float reach = anchor == SnapAnchor.Pivot ? 0f : Vector3.Dot(bounds.extents, Abs(direction));

            RaycastHit[] hits = Physics.RaycastAll(origin, direction, maxDistance, ~0, QueryTriggerInteraction.Ignore);
            if (hits.Length == 0) return false;

            bool found = false;
            RaycastHit nearest = default;

            foreach (RaycastHit hit in hits)
            {
                // Its own colliders are not something to land on, and neither are its children's.
                if (!hit.collider || hit.collider.transform.IsChildOf(target)) continue;
                if (found && hit.distance >= nearest.distance) continue;

                nearest = hit;
                found = true;
            }

            if (!found) return false;

            travel = nearest.distance - reach;
            position = target.position + (nearest.point - direction * reach - origin);
            return true;
        }

        /// <summary>
        /// Renderers first, since that is the shape being looked at. Colliders are the fallback for
        /// objects with nothing to draw, and a bare transform snaps by its pivot alone.
        /// </summary>
        private static Bounds WorldBounds(Transform target)
        {
            bool found = false;
            Bounds bounds = new Bounds(target.position, Vector3.zero);

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

            if (found) return bounds;

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

            return found ? bounds : new Bounds(target.position, Vector3.zero);
        }

        private static Vector3 Abs(Vector3 value)
        {
            return new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
        }
    }
}
