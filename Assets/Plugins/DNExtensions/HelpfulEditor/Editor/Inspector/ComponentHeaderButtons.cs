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
        /// Roots of every open Inspector. Deliberately re-resolved rather than cached:
        /// FindObjectsOfTypeAll turns up inspector windows that are not showing anything, and
        /// latching onto one of those leaves the buttons permanently missing.
        /// </summary>
        private static IEnumerable<VisualElement> EnumerateEditorLists()
        {
            foreach (EditorWindow window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (!HelpfulEditorWindows.IsInspector(window)) continue;

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

            GameObject selected = Selection.activeGameObject;
            if (!selected) return;

            foreach (VisualElement list in EnumerateEditorLists())
            {
                Inject(list, selected);
            }
        }

        private static void Inject(VisualElement editorList, GameObject selected)
        {
            Elements.Clear();
            CollectEditorElements(editorList, Elements);

            foreach (VisualElement matched in Elements)
            {
                Editor editor = InspectorElementLookup.GetEditor(matched);
                if (!editor || !(editor.target is Component component)) continue;
                if (component.gameObject != selected) continue;

                CollectButtons(component);
                if (Buffer.Count == 0) continue;

                if (!ResolveInsertion(matched, out VisualElement container, out VisualElement anchor)) continue;

                VisualElement existing = FindExistingBar(container, anchor);
                int hash = GetButtonHash(Buffer);

                // The bar is only rebuilt when its contents would actually differ, otherwise every
                // editor tick would tear down and recreate a bar per component.
                if (existing != null && existing.panel != null && existing.userData is int previous && previous == hash) continue;

                existing?.RemoveFromHierarchy();

                VisualElement bar = CreateButtonBar(component, Buffer);
                bar.userData = hash;

                // Index is read after the old bar is gone. Reading it beforehand counts the bar
                // itself, so the replacement lands one slot late — below the component body.
                int index = container.IndexOf(anchor);
                container.Insert(Mathf.Clamp(index, 0, container.childCount), bar);
            }
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

        /// <summary>
        /// Only the bar immediately in front of this component's body counts. A plain query would
        /// also find other components' bars whenever the container is a shared parent.
        /// </summary>
        private static VisualElement FindExistingBar(VisualElement container, VisualElement anchor)
        {
            int anchorIndex = container.IndexOf(anchor);
            if (anchorIndex <= 0) return null;

            VisualElement candidate = container.ElementAt(anchorIndex - 1);
            return candidate != null && candidate.name == ButtonBarName ? candidate : null;
        }

        /// <summary>
        /// Elements that own an Editor, found by walking the tree. The Inspector's editor list used
        /// to be reachable by its USS class, but that class no longer exists in Unity 6 — and the
        /// element type names are internal — so the editor itself is the only dependable landmark.
        /// Recursion stops at each match, so component bodies are never walked into.
        /// </summary>
        private static void CollectEditorElements(VisualElement element, List<VisualElement> results)
        {
            if (element.name == ButtonBarName) return;

            Editor editor = InspectorElementLookup.GetEditor(element);

            // Only a component editor ends the walk. The GameObject's own editor sits above the
            // component ones, so stopping there found exactly one element — which is why a single
            // bar appeared under the object header instead of one per component.
            if (editor && editor.target is Component)
            {
                results.Add(element);
                return;
            }

            foreach (VisualElement child in element.Children())
            {
                CollectEditorElements(child, results);
            }
        }

        private static void CollectButtons(Component component)
        {
            Buffer.Clear();

            foreach (Func<Component, ButtonData> provider in Providers)
            {
                try
                {
                    ButtonData data = provider(component);
                    if (data != null) Buffer.Add(data);
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

            /// <summary>Runs on right-click. Providers that want a menu open one from here.</summary>
            public Action<Component> ContextCallback;

            public Action<Button> StyleCallback;

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
