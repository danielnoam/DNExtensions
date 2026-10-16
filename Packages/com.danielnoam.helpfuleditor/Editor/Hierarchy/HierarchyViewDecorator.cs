#if UNITY_6000_6_OR_NEWER
using System.Collections.Generic;
using Unity.Hierarchy;
using Unity.Hierarchy.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace DNExtensions.HelpfulEditor.Hierarchy
{
    /// <summary>
    /// The row overlays for the UI Toolkit Hierarchy that 6.6 made the default. That window never
    /// raises hierarchyWindowItemOnGUI, so instead of painting over rows this adds elements to the
    /// custom containers Unity gives every row, and re-fills them each time a row is bound.
    ///
    /// Rows are recycled as the view scrolls, so every bind resets what the row shows rather than
    /// trusting what a previous object left there. Elements are created once per row and found again
    /// by name.
    ///
    /// Zebra stripes are not ported: the new window draws alternating rows itself.
    /// </summary>
    [InitializeOnLoad]
    internal static class HierarchyViewDecorator
    {
        private const string StripName = "dn-component-strip";
        private const string OverflowName = "dn-component-overflow";
        private const string CountName = "dn-child-count";
        private const string SceneArrowName = "dn-scene-arrow";
        private const string HookedClass = "dn-hooked";

        private const string DropdownIcon = "icon dropdown";
        private const float CountBadgeSize = 11f;
        private const float CountOpacity = 0.6f;

        /// <summary>How long after the scene list closes a press on its arrow is swallowed, so a click meant to close it does not reopen it.</summary>
        private const double ReopenGuard = 0.25;

        private static readonly List<Component> Buffer = new List<Component>();

        private static VisualElement _pressed;
        private static VisualElement _openArrow;
        private static double _closedAt;

        static HierarchyViewDecorator()
        {
            HierarchyWindow.BindViewItem -= OnBindViewItem;
            HierarchyWindow.BindViewItem += OnBindViewItem;

            HierarchyWindow.PopulateContextMenu -= OnPopulateContextMenu;
            HierarchyWindow.PopulateContextMenu += OnPopulateContextMenu;
        }

        /// <summary>Re-binds every built row, so a settings change shows without waiting for a scroll.</summary>
        public static void Refresh()
        {
            foreach (EditorWindow candidate in HelpfulEditorWindows.AllOfType(typeof(HierarchyWindow)))
            {
                if (candidate is not HierarchyWindow window || window.View == null) continue;

                foreach (HierarchyViewItem item in window.View.Query<HierarchyViewItem>().ToList())
                {
                    OnBindViewItem(window, window.View, item);
                }
            }
        }

        private static void OnBindViewItem(HierarchyWindow window, HierarchyView view, HierarchyViewItem item)
        {
            HookRow(item);
            ResetRow(item);

            HierarchySettings settings = HelpfulEditorSettings.Hierarchy;
            if (!settings.moduleEnabled) return;

            if (item.Handler is HierarchyGameObjectHandler gameObjectHandler)
            {
                GameObject gameObject = gameObjectHandler.GetGameObject(item.Node);
                if (!gameObject) return;

                if (settings.componentStripEnabled) BindStrip(window, item, gameObject, settings);
                if (settings.childCountEnabled) BindChildCount(item, gameObject, settings);

                return;
            }

            if (item.Handler is HierarchySceneHandler sceneHandler && settings.sceneMenuEnabled)
            {
                BindSceneArrow(window, item, sceneHandler.GetScene(item.Node));
            }
        }

        private static void ResetRow(HierarchyViewItem item)
        {
            SetShown(item.Q(StripName), false);
            SetShown(item.Q(CountName), false);
            SetShown(item.Q(SceneArrowName), false);
        }

        /// <summary>
        /// Hover is tracked on the row element rather than read back per bind: the keybinds need to
        /// know what the cursor is over between binds, and a recycled row only says what it shows
        /// right now — which is why the callbacks read the node at the moment they fire.
        /// </summary>
        private static void HookRow(HierarchyViewItem item)
        {
            if (item.ClassListContains(HookedClass)) return;

            item.AddToClassList(HookedClass);
            item.RegisterCallback<PointerEnterEvent>(_ => OnRowEnter(item));
            item.RegisterCallback<PointerLeaveEvent>(_ => OnRowLeave(item));
        }

        private static void OnRowEnter(HierarchyViewItem item)
        {
            if (item.Handler is HierarchyGameObjectHandler gameObjectHandler)
            {
                GameObject gameObject = gameObjectHandler.GetGameObject(item.Node);
                HierarchyModule.SetHover(gameObject, HelpfulEditorObjectId.Raw(gameObject));
            }
            else if (item.Handler is HierarchySceneHandler sceneHandler)
            {
                // Boxed rather than reduced to an id: HierarchyTree takes the scene itself and asks
                // the view for its node, which is the one thing a scene header can be looked up by.
                HierarchyModule.SetHover(null, sceneHandler.GetScene(item.Node));
            }

            VisualElement arrow = item.Q(SceneArrowName);
            if (arrow != null && arrow != _openArrow) arrow.style.opacity = 0.6f;
        }

        private static void OnRowLeave(HierarchyViewItem item)
        {
            HierarchyModule.ClearHover();

            VisualElement arrow = item.Q(SceneArrowName);
            if (arrow != null && arrow != _openArrow) arrow.style.opacity = 0f;
        }

        private static void BindStrip(HierarchyWindow window, HierarchyViewItem item, GameObject gameObject, HierarchySettings settings)
        {
            HelpfulEditorGUI.GetDisplayComponents(gameObject, settings.excludedComponentTypes, Buffer);
            if (Buffer.Count == 0) return;

            VisualElement strip = item.Q(StripName) ?? CreateStrip(item);
            SetShown(strip, true);

            int shown = settings.componentStripMaxIcons > 0 ? Mathf.Min(Buffer.Count, settings.componentStripMaxIcons) : Buffer.Count;
            float size = settings.componentStripIconSize;

            // The overflow badge is the strip's last child and icons sit before it, so the icon slots
            // are everything else. Grown on demand and hidden when unused, never removed.
            Label overflow = strip.Q<Label>(OverflowName);
            int slots = strip.childCount - 1;

            for (int i = slots; i < shown; i++)
            {
                strip.Insert(strip.childCount - 1, CreateIcon(window));
            }

            for (int i = 0; i < strip.childCount - 1; i++)
            {
                Image icon = (Image)strip[i];
                bool used = i < shown;
                SetShown(icon, used);
                if (!used) continue;

                Component component = Buffer[i];
                icon.image = HelpfulEditorGUI.GetIcon(component);
                icon.userData = component;
                icon.tooltip = component.GetType().Name;
                icon.style.width = size;
                icon.style.height = size;
                icon.style.opacity = HelpfulEditorGUI.IconStripOpacity;
            }

            bool truncated = shown < Buffer.Count;
            SetShown(overflow, truncated);
            if (!truncated) return;

            overflow.text = $"+{Buffer.Count - shown}";
            overflow.userData = Buffer.GetRange(shown, Buffer.Count - shown);
            overflow.tooltip = BuildOverflowTooltip((List<Component>)overflow.userData);
        }

        private static VisualElement CreateStrip(HierarchyViewItem item)
        {
            VisualElement strip = new VisualElement { name = StripName };
            strip.style.flexDirection = FlexDirection.Row;
            strip.style.alignItems = Align.Center;
            strip.style.marginRight = 2f;

            Label overflow = new Label { name = OverflowName };
            overflow.style.fontSize = 9f;
            overflow.style.opacity = HelpfulEditorGUI.IconStripOpacity;
            overflow.style.paddingLeft = 2f;
            overflow.style.paddingRight = 0f;
            overflow.RegisterCallback<PointerDownEvent>(OnOverflowDown);
            strip.Add(overflow);

            item.RightCustomContainer.Add(strip);
            return strip;
        }

        /// <summary>
        /// Alt+Click opens the quick-edit popup for that component, and only the icon under the
        /// cursor brightens while Alt is held, so the gesture is discoverable without lifting the
        /// whole strip as if every icon were a button at once.
        /// </summary>
        private static Image CreateIcon(HierarchyWindow window)
        {
            Image icon = new Image { scaleMode = ScaleMode.ScaleToFit };
            icon.style.marginLeft = 1f;

            icon.RegisterCallback<PointerMoveEvent>(evt => LiftIcon(icon, evt.altKey));
            icon.RegisterCallback<PointerLeaveEvent>(_ => LiftIcon(icon, false));

            icon.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (!CanQuickEdit(evt.button, evt.altKey)) return;

                _pressed = icon;

                // Taken here so the press neither selects the row nor starts dragging the object,
                // which is what a click slightly off-target would otherwise turn into.
                evt.StopImmediatePropagation();
            });

            // Opened on release, not on press: the popup closes as soon as it loses focus, and the
            // release of the opening click landing back on the Hierarchy would dismiss it at once.
            icon.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (_pressed != icon) return;

                _pressed = null;
                evt.StopImmediatePropagation();

                if (icon.userData is not Component component || !component) return;

                Vector2 screenPosition = ToScreenRect(window, icon.worldBound).min;
                EditorApplication.delayCall += () => HelpfulEditorQuickEditWindow.Open(component, screenPosition);
            });

            return icon;
        }

        private static void LiftIcon(Image icon, bool lifted)
        {
            bool armed = lifted && HelpfulEditorSettings.Hierarchy.componentQuickEditEnabled;
            icon.style.opacity = armed ? 1f : HelpfulEditorGUI.IconStripOpacity;
        }

        private static bool CanQuickEdit(int button, bool alt)
        {
            return button == 0 && alt && HelpfulEditorSettings.Hierarchy.componentQuickEditEnabled;
        }

        private static void OnOverflowDown(PointerDownEvent evt)
        {
            if (!CanQuickEdit(evt.button, evt.altKey)) return;
            if (evt.currentTarget is not Label overflow || overflow.userData is not List<Component> hidden) return;

            evt.StopImmediatePropagation();

            GenericDropdownMenu menu = new GenericDropdownMenu();
            foreach (Component component in hidden)
            {
                if (!component) continue;

                menu.AddItem(component.GetType().Name, false, () => OpenQuickEditAt(overflow, component));
            }

            menu.DropDown(overflow.worldBound, overflow, true);
        }

        private static void OpenQuickEditAt(VisualElement anchor, Component component)
        {
            if (anchor.panel == null) return;

            EditorWindow window = HelpfulEditorWindows.First(typeof(HierarchyWindow));
            if (!window) return;

            Vector2 screenPosition = ToScreenRect(window, anchor.worldBound).min;
            EditorApplication.delayCall += () => HelpfulEditorQuickEditWindow.Open(component, screenPosition);
        }

        private static void BindChildCount(HierarchyViewItem item, GameObject gameObject, HierarchySettings settings)
        {
            int count = gameObject.transform.childCount;
            if (count == 0) return;
            if (settings.childCountHideWhenOneOrZero && count <= 1) return;

            Label badge = item.Q<Label>(CountName) ?? CreateChildCount();
            badge.text = count.ToString();
            badge.userData = gameObject.transform;

            PlaceChildCount(item, badge, settings.childCountPosition);
            SetShown(badge, true);
        }

        private static Label CreateChildCount()
        {
            Label badge = new Label { name = CountName };
            badge.style.unityTextAlign = TextAnchor.MiddleCenter;

            // Built on hover only: the recursive count is the expensive half, and only one row's
            // tooltip can ever be up at a time.
            badge.RegisterCallback<PointerEnterEvent>(_ =>
            {
                if (badge.userData is Transform transform && transform) badge.tooltip = BuildTooltip(transform);
            });

            return badge;
        }

        /// <summary>The badge moves between containers when the setting changes, so it is re-parented and re-styled every bind.</summary>
        private static void PlaceChildCount(HierarchyViewItem item, Label badge, BadgePosition position)
        {
            VisualElement parent = position switch
            {
                BadgePosition.OnIcon => item.Icon,
                BadgePosition.LeftOfName => item.LeftCustomContainer,
                _ => item.RightCustomContainer
            };

            if (badge.parent != parent)
            {
                badge.RemoveFromHierarchy();

                // Right-aligned sits left of the component strip, as it does in the legacy window.
                if (position == BadgePosition.RightAligned) parent.Insert(0, badge);
                else parent.Add(badge);
            }

            if (position == BadgePosition.OnIcon)
            {
                // Tucked into the icon's corner and backed, so it stays legible over any icon.
                badge.style.position = Position.Absolute;
                badge.style.right = -2f;
                badge.style.bottom = -2f;
                badge.style.width = CountBadgeSize;
                badge.style.height = CountBadgeSize;
                badge.style.fontSize = 7f;
                badge.style.paddingLeft = 0f;
                badge.style.paddingRight = 0f;
                badge.style.backgroundColor = HelpfulEditorGUI.WindowBackground;
                badge.style.opacity = 1f;
                return;
            }

            badge.style.position = Position.Relative;
            badge.style.right = StyleKeyword.Auto;
            badge.style.bottom = StyleKeyword.Auto;
            badge.style.width = StyleKeyword.Auto;
            badge.style.height = StyleKeyword.Auto;
            badge.style.fontSize = 9f;
            badge.style.paddingLeft = 2f;
            badge.style.paddingRight = 2f;
            badge.style.backgroundColor = StyleKeyword.Null;
            badge.style.opacity = CountOpacity;
        }

        /// <summary>
        /// A caret after the scene header's name that drops down every scene in the project. Only
        /// the caret is a button: the rest of the header keeps selecting the scene, and right-click
        /// keeps Unity's own scene menu.
        /// </summary>
        private static void BindSceneArrow(HierarchyWindow window, HierarchyViewItem item, Scene scene)
        {
            if (!scene.IsValid()) return;

            VisualElement arrow = item.Q(SceneArrowName) ?? CreateSceneArrow(window, item);
            SetShown(arrow, true);
            arrow.style.opacity = arrow == _openArrow ? 1f : 0f;
        }

        private static VisualElement CreateSceneArrow(HierarchyWindow window, HierarchyViewItem item)
        {
            Image arrow = new Image { name = SceneArrowName, scaleMode = ScaleMode.ScaleToFit };
            arrow.image = HelpfulEditorGUI.LoadIcon(DropdownIcon);
            arrow.style.width = 15f;
            arrow.style.height = 15f;
            arrow.style.marginLeft = 3f;

            // Registered on the row in the trickle-down phase, so a press on the name is taken before
            // the view reads it as a selection or the start of a rename. The row is recycled, so what
            // it shows is checked when the event fires rather than when the callback is added.
            item.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0 || !IsOverSceneLabel(item, arrow, evt.position)) return;

                evt.StopImmediatePropagation();

                // A press that only just dismissed this list is the closing half of a toggle.
                if (EditorApplication.timeSinceStartup - _closedAt < ReopenGuard) return;

                _pressed = arrow;
                arrow.style.opacity = 1f;
            }, TrickleDown.TrickleDown);

            item.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (_pressed != arrow) return;

                _pressed = null;
                evt.StopImmediatePropagation();

                if (!IsOverSceneLabel(item, arrow, evt.position)) return;

                Rect activator = ToScreenRect(window, Rect.MinMaxRect(item.Name.worldBound.xMin, arrow.worldBound.yMin,
                    arrow.worldBound.xMax, arrow.worldBound.yMax));

                _openArrow = arrow;
                EditorApplication.delayCall += () => HierarchySceneMenuWindow.Open(activator, () => OnSceneMenuClosed(arrow));
            }, TrickleDown.TrickleDown);

            item.LeftCustomContainer.Add(arrow);
            return arrow;
        }

        /// <summary>
        /// The scene's name and the caret after it, matching the legacy window where the whole name
        /// opens the list. The rest of the header still selects the scene.
        /// </summary>
        private static bool IsOverSceneLabel(HierarchyViewItem item, VisualElement arrow, Vector2 position)
        {
            if (item.Handler is not HierarchySceneHandler || arrow.resolvedStyle.display == DisplayStyle.None) return false;

            Rect name = item.Name.worldBound;
            Rect hit = Rect.MinMaxRect(name.xMin, name.yMin, Mathf.Max(name.xMax, arrow.worldBound.xMax), name.yMax);

            return hit.Contains(position);
        }

        private static void OnSceneMenuClosed(VisualElement arrow)
        {
            if (_openArrow != arrow) return;

            _openArrow = null;
            _closedAt = EditorApplication.timeSinceStartup;
            arrow.style.opacity = 0f;
        }

        /// <summary>
        /// Isolate is a hover keybind elsewhere, which is invisible until you know it. Listing it on
        /// the row's own menu is the discoverable way in, and the only one that works without a
        /// keyboard.
        /// </summary>
        private static void OnPopulateContextMenu(HierarchyWindow window, HierarchyView view, HierarchyViewItem item, DropdownMenu menu)
        {
            if (!HelpfulEditorSettings.Hierarchy.moduleEnabled) return;

            menu.AppendSeparator();

            if (item?.Handler is HierarchyGameObjectHandler handler)
            {
                GameObject gameObject = handler.GetGameObject(item.Node);
                if (gameObject)
                {
                    string label = HierarchyIsolation.Target == gameObject ? "Exit Isolation" : "Isolate";
                    menu.AppendAction(label, _ => HierarchyIsolation.Toggle(gameObject));
                }
            }

            menu.AppendAction("Collapse Everything", _ => HierarchyExpandQueue.CollapseAll());
        }

        /// <summary>
        /// A panel-space rect in screen space. The panel's origin is the window's content area,
        /// which is what EditorWindow.position describes.
        /// </summary>
        private static Rect ToScreenRect(EditorWindow window, Rect panelRect)
        {
            Vector2 origin = window.rootVisualElement.worldBound.position;
            return new Rect(window.position.position + panelRect.position - origin, panelRect.size);
        }

        private static void SetShown(VisualElement element, bool shown)
        {
            if (element == null) return;

            element.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static string BuildTooltip(Transform transform)
        {
            int direct = transform.childCount;
            int total = CountDescendants(transform);
            if (total == direct) return $"{direct} direct child object(s)";

            return $"{direct} direct child object(s)\n{total} total in subtree";
        }

        private static int CountDescendants(Transform transform)
        {
            int total = transform.childCount;

            for (int i = 0; i < transform.childCount; i++)
            {
                total += CountDescendants(transform.GetChild(i));
            }

            return total;
        }

        private static string BuildOverflowTooltip(List<Component> components)
        {
            List<string> names = new List<string>();
            foreach (Component component in components)
            {
                if (component) names.Add(component.GetType().Name);
            }

            return string.Join("\n", names);
        }
    }
}
#endif
