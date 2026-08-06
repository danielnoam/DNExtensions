using UnityEditor;
using UnityEngine;

namespace DNExtensions.HelpfulEditor.Hierarchy
{
    /// <summary>
    /// Hover keybinds for the Hierarchy. Events arrive through GlobalKeyCapture so they fire even
    /// when another window holds focus; the cached hover from HierarchyModule decides the target.
    /// </summary>
    [InitializeOnLoad]
    internal static class HierarchyKeybinds
    {
        private static readonly Vector3[] WorldCorners = new Vector3[4];

        private static Tool _toolBeforeKey;
        private static bool _restoreTool;

        private static Bounds _pendingFocus;
        private static bool _hasPendingFocus;

        static HierarchyKeybinds()
        {
            GlobalKeyCapture.KeyEvent -= OnKeyEvent;
            GlobalKeyCapture.KeyEvent += OnKeyEvent;

            EditorApplication.update -= AfterKeyEvent;
            EditorApplication.update += AfterKeyEvent;
        }

        private static void OnKeyEvent()
        {
            HierarchySettings settings = HelpfulEditorSettings.Hierarchy;
            if (!settings.moduleEnabled) return;
            if (!HelpfulEditorWindows.MouseOverHierarchy) return;
            if (EditorGUIUtility.editingTextField) return;

            Event evt = Event.current;
            if (evt == null || evt.type != EventType.KeyDown) return;

            Tool toolBefore = Tools.current;

            if (!Handle(settings, evt)) return;

            // Unity's own shortcuts act on the key whether or not it is consumed here, and E is the
            // Rotate tool's. Expanding a row would otherwise change the Scene View tool as a side
            // effect. Compared afterwards rather than gated on a version, since which keys collide
            // is not something to hardcode.
            _toolBeforeKey = toolBefore;
            _restoreTool = true;
        }

        /// <summary>Returns whether the event was consumed.</summary>
        private static bool Handle(HierarchySettings settings, Event evt)
        {
            GameObject target = HierarchyModule.HoveredObject ? HierarchyModule.HoveredObject : Selection.activeGameObject;

            if (settings.collapseAllKey.Matches(evt))
            {
                HierarchyExpandQueue.CollapseAll();
                evt.Use();
                return true;
            }

            if (settings.toggleActiveKey.Matches(evt))
            {
                ToggleActive(target);
                evt.Use();
                return true;
            }

            if (settings.focusKey.Matches(evt))
            {
                RequestFocus(target);
                evt.Use();
                return true;
            }

            if (settings.isolateKey.Matches(evt))
            {
                HierarchyIsolation.Toggle(target);
                evt.Use();
                return true;
            }

            if (settings.expandCollapseRecursiveKey.Matches(evt))
            {
                HelpfulEditorTreeReflection.ToggleHierarchyExpanded(ResolveRowId(target), true);
                evt.Use();
                return true;
            }

            if (settings.expandCollapseKey.Matches(evt))
            {
                HelpfulEditorTreeReflection.ToggleHierarchyExpanded(ResolveRowId(target), false);
                evt.Use();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Runs on the tick after a keybind fired, which is where anything that has to outlast
        /// Unity's own handling of the same key belongs. Consuming the event is not enough — the
        /// editor's shortcuts act on it either way.
        /// </summary>
        private static void AfterKeyEvent()
        {
            RestoreTransformTool();
            ApplyPendingFocus();
        }

        /// <summary>Only puts the tool back if something else actually changed it, so a deliberate change is left alone.</summary>
        private static void RestoreTransformTool()
        {
            if (!_restoreTool) return;

            _restoreTool = false;

            if (Tools.current != _toolBeforeKey) Tools.current = _toolBeforeKey;
        }

        /// <summary>
        /// Frames whatever the cursor was over. Deliberately does not select it or focus the Scene
        /// View: the point of a hover shortcut is to look at something without disturbing what you
        /// had selected or where the keyboard was pointing.
        /// </summary>
        private static void RequestFocus(GameObject target)
        {
            GameObject[] targets = ResolveTargets(target);
            if (targets.Length == 0) return;

            if (!TryGetBounds(targets, out Bounds bounds)) return;

            _pendingFocus = bounds;
            _hasPendingFocus = true;
        }

        private static void ApplyPendingFocus()
        {
            if (!_hasPendingFocus) return;

            _hasPendingFocus = false;

            SceneView view = HelpfulEditorWindows.ResolveSceneView(out bool needsFocus);
            if (!view) return;

            // Brought to front and given the keyboard when it did not already have it, so the camera
            // is usable the moment it arrives — framing a Scene View that is behind another tab, or
            // that cannot be orbited without clicking it first, is only half the gesture.
            if (needsFocus) view.Focus();

            view.Frame(_pendingFocus, false);
            view.Repaint();
        }

        private static bool TryGetBounds(GameObject[] targets, out Bounds bounds)
        {
            bounds = default;
            bool found = false;

            foreach (GameObject gameObject in targets)
            {
                if (!gameObject) continue;

                Bounds objectBounds = BoundsOf(gameObject);

                if (found)
                {
                    bounds.Encapsulate(objectBounds);
                }
                else
                {
                    bounds = objectBounds;
                    found = true;
                }
            }

            return found;
        }

        private static Bounds BoundsOf(GameObject gameObject)
        {
            Bounds bounds = default;
            bool found = false;

            foreach (Renderer renderer in gameObject.GetComponentsInChildren<Renderer>())
            {
                if (!renderer || !renderer.enabled) continue;

                if (found)
                {
                    bounds.Encapsulate(renderer.bounds);
                }
                else
                {
                    bounds = renderer.bounds;
                    found = true;
                }
            }

            if (found) return bounds;

            // UI draws through a CanvasRenderer, which is not a Renderer, so a canvas object has no
            // bounds to find that way — its rect is what framing means for it.
            if (gameObject.transform is RectTransform rectTransform)
            {
                rectTransform.GetWorldCorners(WorldCorners);

                bounds = new Bounds(WorldCorners[0], Vector3.zero);
                for (int i = 1; i < WorldCorners.Length; i++) bounds.Encapsulate(WorldCorners[i]);

                return bounds;
            }

            // An empty object still has somewhere to look, and framing nothing at all reads as the
            // key having done nothing.
            return new Bounds(gameObject.transform.position, Vector3.one);
        }

        /// <summary>
        /// Prefers the hovered row's own id so scene headers expand and collapse like any other row,
        /// falling back to the selected object when the cursor is not on a row.
        /// </summary>
        private static object ResolveRowId(GameObject target)
        {
            if (HierarchyModule.HoveredRawId != null) return HierarchyModule.HoveredRawId;

            return target ? HelpfulEditorObjectId.Raw(target) : null;
        }

        private static void ToggleActive(GameObject target)
        {
            GameObject[] targets = ResolveTargets(target);
            if (targets.Length == 0) return;

            bool newState = !targets[0].activeSelf;

            foreach (GameObject gameObject in targets)
            {
                Undo.RecordObject(gameObject, "Toggle Active");
                gameObject.SetActive(newState);
                EditorUtility.SetDirty(gameObject);
            }

            EditorApplication.RepaintHierarchyWindow();
        }

        /// <summary>Acts on the whole selection when the hovered object is part of it, otherwise just that object.</summary>
        private static GameObject[] ResolveTargets(GameObject target)
        {
            GameObject[] selected = Selection.gameObjects;

            if (!target) return selected;

            foreach (GameObject gameObject in selected)
            {
                if (gameObject == target) return selected;
            }

            return new[] { target };
        }
    }
}
