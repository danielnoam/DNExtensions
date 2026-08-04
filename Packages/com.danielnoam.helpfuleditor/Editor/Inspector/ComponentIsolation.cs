using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DNExtensions.HelpfulEditor.Inspector
{
    /// <summary>
    /// Display-only isolation of components in the Inspector. No component data changes — the
    /// non-isolated editors are simply marked invisible on the shared ActiveEditorTracker, which is
    /// the only supported way to collapse a component body we do not own the OnInspectorGUI of.
    /// </summary>
    [InitializeOnLoad]
    internal static class ComponentIsolation
    {
        private static readonly List<Component> Isolated = new List<Component>();
        private static readonly List<Component> Stashed = new List<Component>();
        private static readonly List<Component> Hidden = new List<Component>();

        private static int _lastClickedIndex = -1;

        public static bool IsIsolating
        {
            get
            {
                Prune();
                return Isolated.Count > 0;
            }
        }

        /// <summary>
        /// Drops components that have since been destroyed. Without this a deleted component would
        /// keep the isolated set non-empty forever, leaving every other component's body collapsed
        /// with no icon left to click to undo it.
        /// </summary>
        private static void Prune()
        {
            Isolated.RemoveAll(component => !component);
            Stashed.RemoveAll(component => !component);
        }

        static ComponentIsolation()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            Selection.selectionChanged += OnSelectionChanged;

            EditorSceneManager.sceneSaving -= RestoreForSave;
            EditorSceneManager.sceneSaving += RestoreForSave;

            AssemblyReloadEvents.beforeAssemblyReload -= RestoreHidden;
            AssemblyReloadEvents.beforeAssemblyReload += RestoreHidden;

            EditorApplication.quitting -= RestoreHidden;
            EditorApplication.quitting += RestoreHidden;

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        /// <summary>
        /// The flags come off on the way into play mode. Leaving them set would put HideInInspector
        /// into the snapshot Unity takes of the scene, which is the same snapshot it restores from on
        /// exit — so the hidden state would outlive the isolation.
        /// </summary>
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode) RestoreAndRepaint();
        }

        public static bool Contains(Component component) => component && Isolated.Contains(component);

        /// <summary>
        /// The isolated set when the given component is part of it, otherwise just that component —
        /// the same rule the Hierarchy uses for acting on a selection versus a single hovered row.
        /// </summary>
        public static List<Component> GetSelection(Component component)
        {
            Prune();

            if (Isolated.Count > 1 && Isolated.Contains(component)) return new List<Component>(Isolated);

            return new List<Component> { component };
        }

        /// <summary>Ctrl+Click behaviour: add or remove a single component from the isolated set.</summary>
        public static void Toggle(Component component, int index)
        {
            if (!component) return;

            if (!Isolated.Remove(component)) Isolated.Add(component);
            _lastClickedIndex = index;
            Apply();
        }

        /// <summary>Plain click behaviour: isolate exactly this component, or clear if it already is.</summary>
        public static void Solo(Component component, int index)
        {
            if (!component) return;

            bool alreadySolo = Isolated.Count == 1 && Isolated[0] == component;
            Isolated.Clear();
            if (!alreadySolo) Isolated.Add(component);

            _lastClickedIndex = index;
            Apply();
        }

        /// <summary>Shift+Click behaviour: extend the isolated set to the range since the last click.</summary>
        public static void SelectRange(IReadOnlyList<Component> components, int index)
        {
            if (components == null || index < 0 || index >= components.Count) return;

            if (_lastClickedIndex < 0 || _lastClickedIndex >= components.Count)
            {
                Solo(components[index], index);
                return;
            }

            int from = Mathf.Min(_lastClickedIndex, index);
            int to = Mathf.Max(_lastClickedIndex, index);

            for (int i = from; i <= to; i++)
            {
                Component component = components[i];
                if (component && !Isolated.Contains(component)) Isolated.Add(component);
            }

            Apply();
        }

        /// <summary>Keybind behaviour: drop the isolated view without losing which components were in it.</summary>
        public static void ToggleActive()
        {
            if (Isolated.Count > 0)
            {
                Stashed.Clear();
                Stashed.AddRange(Isolated);
                Isolated.Clear();
            }
            else
            {
                foreach (Component component in Stashed)
                {
                    if (component) Isolated.Add(component);
                }
            }

            Apply();
        }

        public static void Clear()
        {
            if (Isolated.Count == 0) return;

            Isolated.Clear();
            _lastClickedIndex = -1;
            Apply();
        }

        /// <summary>
        /// Deferred because hiding rebuilds the Inspector's editor list, which is not safe to do
        /// from inside the header GUI that most of these calls originate in.
        /// </summary>
        public static void Apply()
        {
            EditorApplication.delayCall -= ApplyNow;
            EditorApplication.delayCall += ApplyNow;
        }

        private static void ApplyNow()
        {
            Prune();
            RestoreHidden();

            ActiveEditorTracker tracker = ActiveEditorTracker.sharedTracker;

            // Isolation only counts when at least one isolated component belongs to what is being
            // inspected. Without this, isolating on one object and then selecting another would
            // hide every component of the new object, since none of them are in the set.
            bool isolating = false;
            foreach (Editor editor in tracker.activeEditors)
            {
                if (!editor || !(editor.target is Component component) || !Isolated.Contains(component)) continue;

                isolating = true;
                break;
            }

            if (isolating)
            {
                foreach (Editor editor in tracker.activeEditors)
                {
                    if (!editor || !(editor.target is Component component)) continue;
                    if (Isolated.Contains(component)) continue;

                    component.hideFlags |= HideFlags.HideInInspector;
                    Hidden.Add(component);
                }

                tracker.ForceRebuild();
            }

            RepaintInspectors();
        }

        /// <summary>
        /// HideInInspector is serialized, so a component left flagged would persist into the scene
        /// file. Every exit point restores first — including immediately before a save or a domain
        /// reload, either of which would otherwise bake the flag in or strand it.
        /// </summary>
        internal static void RestoreHidden()
        {
            if (Hidden.Count == 0) return;

            foreach (Component component in Hidden)
            {
                if (component) component.hideFlags &= ~HideFlags.HideInInspector;
            }

            Hidden.Clear();
            ActiveEditorTracker.sharedTracker.ForceRebuild();
        }

        private static void RestoreForSave(Scene scene, string path) => RestoreForExternalWrite();

        /// <summary>
        /// Un-hides for the duration of a write, then puts the isolated view back on the next tick.
        /// Restoring alone would silently drop isolation on every save while the eye button carried
        /// on reporting that components were hidden.
        /// </summary>
        internal static void RestoreForExternalWrite()
        {
            if (Hidden.Count == 0) return;

            RestoreHidden();
            RepaintInspectors();
            Apply();
        }

        private static void RestoreAndRepaint()
        {
            RestoreHidden();
            RepaintInspectors();
        }

        private static void RepaintInspectors()
        {
            foreach (EditorWindow window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (window && window.GetType().Name == "InspectorWindow") window.Repaint();
            }
        }

        private static void OnSelectionChanged()
        {
            if (HelpfulEditorSettings.Inspector.isolationPersistsAcrossSelection)
            {
                Apply();
                return;
            }

            Isolated.Clear();
            _lastClickedIndex = -1;
            Apply();
        }
    }
}
