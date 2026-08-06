using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace DNExtensions.HelpfulEditor.Inspector
{
    /// <summary>
    /// Injects a small button bar under each component's header in the Inspector, built from
    /// registered providers. Uses UI Toolkit to insert elements alongside Unity's own header rather
    /// than trying to draw into it — Unity exposes no hook for the inline component titlebars.
    /// </summary>
    [InitializeOnLoad]
    internal static class ComponentHeaderButtons
    {
        private const string ButtonBarName = "helpfuleditor-component-header-buttons";

        private static readonly List<Func<Component, ButtonData>> Providers = new List<Func<Component, ButtonData>>();
        private static readonly List<ButtonData> Buffer = new List<ButtonData>();
        private static readonly List<VisualElement> Elements = new List<VisualElement>();
        private static readonly List<VisualElement> Bars = new List<VisualElement>();

        private const double ScanInterval = 0.1;

        private static double _lastScan;

        static ComponentHeaderButtons()
        {
            EditorApplication.update -= Update;
            EditorApplication.update += Update;

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        /// <summary>
        /// Registers a source of header buttons. The provider is called per component and returns a
        /// button to add, or null for components it does not apply to.
        /// </summary>
        public static void RegisterProvider(Func<Component, ButtonData> provider)
        {
            if (provider != null && !Providers.Contains(provider)) Providers.Add(provider);
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            foreach (VisualElement root in EnumerateEditorLists())
            {
                root.Query(ButtonBarName).ForEach(bar => bar.RemoveFromHierarchy());
            }
        }

        /// <summary>
        /// Roots of every open Inspector. Deliberately re-resolved rather than cached: the lookup
        /// turns up inspector windows that are not showing anything, and latching onto one of those
        /// leaves the buttons permanently missing.
        ///
        /// Asked for by type rather than fetching every EditorWindow in the editor and filtering
        /// afterwards, which is what this used to do ten times a second.
        /// </summary>
        private static IEnumerable<VisualElement> EnumerateEditorLists()
        {
            foreach (EditorWindow window in HelpfulEditorWindows.AllInspectors())
            {
                if (!window) continue;

                VisualElement root = window.rootVisualElement;
                if (root != null) yield return root;
            }
        }

        private static void Update()
        {
            if (Providers.Count == 0) return;
            if (!HelpfulEditorSettings.Inspector.moduleEnabled) return;

            if (EditorApplication.timeSinceStartup - _lastScan < ScanInterval) return;
            _lastScan = EditorApplication.timeSinceStartup;

            foreach (VisualElement list in EnumerateEditorLists())
            {
                Inject(list);
            }
        }

        /// <summary>
        /// The objects a button should act on: the whole selection when the component belongs to it,
        /// otherwise just its own object.
        /// </summary>
        public static GameObject[] TargetObjects(Component component)
        {
            if (!component) return Array.Empty<GameObject>();

            GameObject[] selection = Selection.gameObjects;
            return Array.IndexOf(selection, component.gameObject) >= 0 ? selection : new[] { component.gameObject };
        }

        /// <summary>
        /// Driven entirely by the editors this window is showing, never by the selection. The two
        /// agree for an ordinary Inspector and do not for a locked one, which goes on showing an
        /// object after the selection has moved elsewhere — gating on the selection meant a locked
        /// Inspector got no buttons at all.
        /// </summary>
        private static void Inject(VisualElement editorList)
        {
            Elements.Clear();
            InspectorElementLookup.CollectComponentEditors(editorList, Elements);

            Bars.Clear();
            CollectBars(Elements, Bars);

            foreach (VisualElement matched in Elements)
            {
                Editor editor = InspectorElementLookup.GetEditor(matched);
                if (!editor || !(editor.target is Component component)) continue;

                // The editor knows how many objects it is editing, which is the question actually
                // being asked. Selection.gameObjects is only the same answer by coincidence.
                CollectButtons(component, editor.targets != null && editor.targets.Length > 1);
                if (Buffer.Count == 0) continue;

                if (!ResolveInsertion(matched, out VisualElement container, out VisualElement anchor)) continue;

                VisualElement existing = TakeBarFor(component);
                int hash = GetButtonHash(Buffer);

                int anchorIndex = container.IndexOf(anchor);
                bool inPlace = existing != null && anchorIndex > 0 && container.ElementAt(anchorIndex - 1) == existing;

                // Rebuilt only when the contents would differ or it has drifted from its component,
                // otherwise every editor tick would tear down and recreate a bar per component.
                if (inPlace && existing.panel != null && existing.userData is BarState state && state.Hash == hash) continue;

                existing?.RemoveFromHierarchy();

                VisualElement bar = CreateButtonBar(component, Buffer);
                bar.userData = new BarState { Owner = component, Hash = hash };

                // Index is read after the old bar is gone. Reading it beforehand counts the bar
                // itself, so the replacement lands one slot late — below the component body.
                int index = container.IndexOf(anchor);
                container.Insert(Mathf.Clamp(index, 0, container.childCount), bar);
            }

            // Whatever is left belongs to no component drawn this pass — a bar the Inspector moved
            // away from its component, or one whose component is gone. Either way it is a duplicate
            // waiting to happen, so it goes.
            foreach (VisualElement stale in Bars)
            {
                stale.RemoveFromHierarchy();
            }
        }

        /// <summary>
        /// The bar belonging to this component, removed from the pending list so that anything still
        /// in that list at the end of the pass is known to be orphaned.
        /// </summary>
        private static VisualElement TakeBarFor(Component component)
        {
            for (int i = 0; i < Bars.Count; i++)
            {
                if (!(Bars[i].userData is BarState state) || state.Owner != component) continue;

                VisualElement bar = Bars[i];
                Bars.RemoveAt(i);
                return bar;
            }

            return null;
        }

        /// <summary>
        /// Every bar currently in this Inspector. A bar is always a direct child of a component
        /// editor's element or of that element's parent — those are the only two containers
        /// ResolveInsertion hands back — so nothing deeper is worth looking at. Walking the whole
        /// tree instead meant descending through every component body on every tick, which was the
        /// bulk of what this module cost while sitting idle.
        ///
        /// A bar left behind by a component that has since gone is still found, because it stays a
        /// child of the container its surviving siblings share.
        /// </summary>
        private static void CollectBars(List<VisualElement> editors, List<VisualElement> results)
        {
            foreach (VisualElement matched in editors)
            {
                AddBars(matched, results);
                AddBars(matched.parent, results);
            }
        }

        /// <summary>Direct children only, and de-duplicated: sibling editors share a parent.</summary>
        private static void AddBars(VisualElement container, List<VisualElement> results)
        {
            if (container == null) return;

            foreach (VisualElement child in container.Children())
            {
                if (child.name == ButtonBarName && !results.Contains(child)) results.Add(child);
            }
        }

        /// <summary>What a bar is for, so it can be found by owner rather than by where it sits.</summary>
        private class BarState
        {
            public Component Owner;
            public int Hash;
        }

        /// <summary>
        /// The element the bar must sit directly in front of, so it lands between a component's
        /// header and its body. Both the wrapper element and the body element expose the same
        /// Editor, so which one the walk matched decides whether the bar goes inside it or beside it.
        /// </summary>
        private static bool ResolveInsertion(VisualElement matched, out VisualElement container, out VisualElement anchor)
        {
            foreach (VisualElement child in matched.Children())
            {
                if (!InspectorElementLookup.GetEditor(child)) continue;

                // Matched the wrapper: the body is a child, and the header precedes it.
                container = matched;
                anchor = child;
                return true;
            }

            // Matched the body itself: sit just before it among its siblings.
            container = matched.parent;
            anchor = matched;
            return container != null;
        }

        private static void CollectButtons(Component component, bool multiSelection)
        {
            Buffer.Clear();

            foreach (Func<Component, ButtonData> provider in Providers)
            {
                try
                {
                    ButtonData data = provider(component);
                    if (data == null) continue;

                    // Hidden rather than shown-and-partial: a button that cannot act on the whole
                    // selection would otherwise change one object and look like it changed all.
                    if (multiSelection && !data.SupportsMultiSelect) continue;

                    Buffer.Add(data);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[HelpfulEditor] Component header button provider failed: {e.Message}");
                }
            }

            Buffer.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        }

        private static VisualElement CreateButtonBar(Component component, List<ButtonData> buttons)
        {
            bool proSkin = EditorGUIUtility.isProSkin;

            VisualElement wrapper = new VisualElement { name = ButtonBarName };

            VisualElement row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    backgroundColor = proSkin ? new Color(0.18f, 0.18f, 0.18f) : new Color(0.78f, 0.78f, 0.78f),
                    paddingLeft = 15f,
                    paddingRight = 5f,
                    paddingTop = 2f,
                    paddingBottom = 2f
                }
            };

            foreach (ButtonData data in buttons)
            {
                ButtonData captured = data;

                Button button = new Button(() =>
                {
                    // Captured by reference: a destroyed component reads as null and is skipped.
                    if (component) captured.Invoke(component);
                })
                {
                    text = captured.Icon,
                    tooltip = captured.Tooltip
                };

                // Right-click is wired separately because Clickable only claims button 0, so the
                // event reaches this handler untouched.
                if (captured.ContextCallback != null)
                {
                    button.RegisterCallback<MouseDownEvent>(evt =>
                    {
                        if (evt.button != 1) return;

                        if (component) captured.InvokeContext(component);
                        evt.StopPropagation();
                    });
                }

                StyleButton(button);
                captured.StyleCallback?.Invoke(button);
                row.Add(button);
            }

            wrapper.Add(row);
            wrapper.Add(new VisualElement
            {
                style =
                {
                    height = 1f,
                    backgroundColor = proSkin ? new Color(0.13f, 0.13f, 0.13f) : new Color(0.58f, 0.58f, 0.58f)
                }
            });

            return wrapper;
        }

        private static void StyleButton(Button button)
        {
            button.style.minWidth = 24f;
            button.style.height = 18f;
            button.style.fontSize = 11f;
            button.style.paddingLeft = 2f;
            button.style.paddingRight = 2f;
            button.style.paddingTop = 2f;
            button.style.paddingBottom = 2f;
            button.style.marginLeft = 1f;
            button.style.marginRight = 1f;
            button.style.unityTextAlign = TextAnchor.MiddleCenter;
        }

        private static int GetButtonHash(List<ButtonData> buttons)
        {
            unchecked
            {
                int hash = 17;
                foreach (ButtonData button in buttons)
                {
                    hash = hash * 31 + (button.Icon?.GetHashCode() ?? 0);
                    hash = hash * 31 + (button.Tooltip?.GetHashCode() ?? 0);
                    hash = hash * 31 + button.Priority;
                }

                return hash;
            }
        }

        internal class ButtonData
        {
            public string Icon;
            public string Tooltip;
            public int Priority;
            public Action<Component> Callback;

            /// <summary>
            /// Runs on right-click. Providers that want a menu open one from here. Initialised rather
            /// than left to its default so it does not warn while no provider happens to use it — the
            /// right-click plumbing above stays available for the next one that does.
            /// </summary>
            public Action<Component> ContextCallback = null;

            public Action<Button> StyleCallback;

            /// <summary>
            /// Whether the action means something for several objects at once. Buttons that leave
            /// this off are hidden while more than one object is selected, rather than appearing and
            /// quietly acting on just one of them.
            /// </summary>
            public bool SupportsMultiSelect;

            public void Invoke(Component component)
            {
                Run(Callback, component);
            }

            public void InvokeContext(Component component)
            {
                Run(ContextCallback, component);
            }

            private static void Run(Action<Component> action, Component component)
            {
                if (action == null) return;

                try
                {
                    action(component);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[HelpfulEditor] Component header button failed: {(e.InnerException ?? e).Message}", component);
                }
            }
        }
    }
}
