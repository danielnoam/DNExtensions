using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace DNExtensions.HelpfulEditor.Inspector
{
    /// <summary>
    /// Display-only isolation of components in the Inspector. Nothing about the components changes —
    /// the editors of the ones not isolated are simply not displayed.
    ///
    /// Done by setting the editor element's display style rather than by flagging the component with
    /// HideInInspector. That flag is serialized, so hiding a component used to mean guarding every
    /// path that could write it to disk — scene saves, asset writes, domain reloads, quitting, play
    /// mode — because any of them would otherwise bake the flag into a scene or prefab. Styling a
    /// VisualElement cannot leave anything behind, so none of that is needed.
    /// </summary>
    [InitializeOnLoad]
    internal static class ComponentIsolation
    {
        /// <summary>
        /// Unity rebuilds the editor list on its own — a component added, a script recompiled — and
        /// the fresh elements come back visible, so the styling has to be reasserted.
        /// </summary>
        private const double MaintainInterval = 0.1;

        private const string EditorListClass = "unity-inspector-editors-list";

        private static readonly List<Component> Isolated = new List<Component>();
        private static readonly List<VisualElement> Elements = new List<VisualElement>();
        private static readonly HashSet<Component> Shown = new HashSet<Component>();

        private static double _lastMaintain;
        private static int _lastClickedIndex = -1;

        public static bool IsIsolating
        {
            get
            {
                Prune();
                return Isolated.Count > 0;
            }
        }

        static ComponentIsolation()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            Selection.selectionChanged += OnSelectionChanged;

            EditorApplication.update -= Maintain;
            EditorApplication.update += Maintain;
        }

        /// <summary>
        /// Costs a count check while nothing is isolated, which is almost always. The walk only runs
        /// when there is something to keep hidden.
        /// </summary>
        private static void Maintain()
        {
            if (Isolated.Count == 0) return;
            if (EditorApplication.timeSinceStartup - _lastMaintain < MaintainInterval) return;

            _lastMaintain = EditorApplication.timeSinceStartup;
            Apply();
        }

        /// <summary>
        /// Drops components that have since been destroyed. Without this a deleted component would
        /// keep the isolated set non-empty forever, leaving every other component hidden with no icon
        /// left to click to undo it.
        /// </summary>
        private static void Prune() => Isolated.RemoveAll(component => !component);

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

            bool added = !Isolated.Remove(component);
            if (added) Isolated.Add(component);

            _lastClickedIndex = index;
            ApplyAfterExpanding(added && Expand(component));
        }

        /// <summary>Plain click behaviour: isolate exactly this component, or clear if it already is.</summary>
        public static void Solo(Component component, int index)
        {
            if (!component) return;

            bool alreadySolo = Isolated.Count == 1 && Isolated[0] == component;
            Isolated.Clear();
            if (!alreadySolo) Isolated.Add(component);

            _lastClickedIndex = index;
            ApplyAfterExpanding(!alreadySolo && Expand(component));
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
            bool expanded = false;

            for (int i = from; i <= to; i++)
            {
                Component component = components[i];
                if (!component || Isolated.Contains(component)) continue;

                Isolated.Add(component);
                expanded |= Expand(component);
            }

            ApplyAfterExpanding(expanded);
        }

        /// <summary>
        /// Opens a component's fold. A component isolated while collapsed shows nothing but its own
        /// header, which is not what the click asked to look at. Only ever expands: a click that
        /// drops a component from the set leaves its fold alone, since closing something on the way
        /// out would undo a fold the user opened by hand.
        /// </summary>
        /// <returns>True when the fold actually changed, and a rebuild is therefore owed.</returns>
        private static bool Expand(Component component)
        {
            if (!component || InternalEditorUtility.GetIsInspectorExpanded(component)) return false;

            InternalEditorUtility.SetIsInspectorExpanded(component, true);
            return true;
        }

        /// <summary>
        /// Applies the isolated view, rebuilding the editor list first when a fold was opened — the
        /// expanded state only takes effect on a rebuild. That rebuild throws away the very elements
        /// Apply styles, so the styling waits until the new ones exist. The maintain poll would catch
        /// it either way, but a tick later and visibly.
        /// </summary>
        private static void ApplyAfterExpanding(bool expanded)
        {
            if (!expanded)
            {
                Apply();
                return;
            }

            ActiveEditorTracker.sharedTracker.ForceRebuild();
            EditorApplication.delayCall += Apply;
        }

        public static void Clear()
        {
            if (Isolated.Count == 0) return;

            Isolated.Clear();
            _lastClickedIndex = -1;
            Apply();
        }

        /// <summary>
        /// Applies the isolated view to every open Inspector. Safe to call at any time — it only sets
        /// styles, and an empty isolated set shows everything again.
        /// </summary>
        public static void Apply()
        {
            Prune();

            foreach (EditorWindow window in HelpfulEditorWindows.AllInspectors())
            {
                if (!window) continue;

                VisualElement list = window.rootVisualElement?.Q(null, EditorListClass);
                if (list == null) continue;

                Elements.Clear();
                InspectorElementLookup.CollectComponentEditors(list, Elements);

                ApplyToWindow(Elements);
            }

            RepaintInspectors();
        }

        private static void ApplyToWindow(List<VisualElement> elements)
        {
            // Isolation only counts when at least one isolated component belongs to what this window
            // is inspecting. Without the check, isolating on one object and then selecting another
            // would hide every component of the new object, since none of them are in the set.
            bool isolating = false;

            foreach (VisualElement element in elements)
            {
                if (InspectorElementLookup.GetEditor(element)?.target is not Component component) continue;
                if (!Isolated.Contains(component)) continue;

                isolating = true;
                break;
            }

            foreach (VisualElement element in elements)
            {
                bool show = true;

                if (isolating)
                {
                    show = InspectorElementLookup.GetEditor(element)?.target is Component component
                           && Isolated.Contains(component);
                }

                element.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        /// <summary>
        /// Styling repaints itself, but the header bar's eye button is IMGUI and reads the isolated
        /// state, so it has to be told.
        /// </summary>
        private static void RepaintInspectors()
        {
            foreach (EditorWindow window in HelpfulEditorWindows.AllInspectors())
            {
                if (window) window.Repaint();
            }
        }

        // Deferred: the editor list for the new selection does not exist yet, so both the pruning and
        // the styling would otherwise land on the outgoing one.
        private static void OnSelectionChanged() => EditorApplication.delayCall += ApplyAfterSelectionChange;

        private static void ApplyAfterSelectionChange()
        {
            if (!HelpfulEditorSettings.Inspector.isolationPersistsAcrossSelection) DropUnshownComponents();

            Apply();
        }

        /// <summary>
        /// Isolation ends when the selection moves on — but only for the windows that moved with it.
        /// An Inspector locked to an object still shows it, and clearing on its behalf would undo an
        /// isolation that is still on screen. Asking which components are currently displayed answers
        /// that without having to know which windows are locked.
        /// </summary>
        private static void DropUnshownComponents()
        {
            Shown.Clear();

            foreach (EditorWindow window in HelpfulEditorWindows.AllInspectors())
            {
                if (!window) continue;

                VisualElement list = window.rootVisualElement?.Q(null, EditorListClass);
                if (list == null) continue;

                Elements.Clear();
                InspectorElementLookup.CollectComponentEditors(list, Elements);

                foreach (VisualElement element in Elements)
                {
                    if (InspectorElementLookup.GetEditor(element)?.target is Component component) Shown.Add(component);
                }
            }

            Isolated.RemoveAll(component => !Shown.Contains(component));

            if (Isolated.Count == 0) _lastClickedIndex = -1;
        }
    }
}
