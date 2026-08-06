using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DNExtensions.HelpfulEditor.GameView
{
    /// <summary>
    /// A Rulers toggle in the Game View's toolbar, sat immediately left of Unity's mute and shortcuts
    /// buttons. That toolbar is IMGUI with no extension point, so this is an overlay drawn in Unity's
    /// own toolbar style — but it is laid out the way GUILayout would have laid it out had it been one
    /// of them: right-aligned against the mute/shortcuts/stats/gizmos cluster while there is room, then
    /// pushed off the edge by the controls to its left once the window is too narrow to hold everyone.
    /// </summary>
    internal class GameViewRulerToggle : IMGUIContainer
    {
        public const string ElementName = "helpfuleditor-gameview-ruler-toggle";

        /// <summary>What the cluster comes to in the default skin, used until it can be measured.</summary>
        private const float FallbackClusterWidth = 190f;
        private const float FallbackButtonWidth = 30f;
        private const float FallbackToolbarHeight = 21f;

        // Widths GameView.DoToolbarGUI hands its own controls. Reproduced rather than reflected: they
        // are literals in that method, so there is nothing to read them off at runtime.
        private const float WindowTypePopupWidth = 90f;
        private const float DisplayPopupWidth = 80f;
        private const float AspectPopupWidth = 160f;
        private const float ZoomSliderMinWidth = 30f;
        private const float ZoomReadoutWidth = 40f;
        private const float PlayModeBehaviorWidth = 110f;
        private const float PopupSpacing = 6f;

        private static GUIContent _muteContent;
        private static GUIContent _shortcutsContent;
        private static GUIContent _statsContent;
        private static GUIContent _gizmosContent;
        private static GUIContent _zoomContent;
        private static GUIStyle _gizmosStyle;
        private static bool _windowTypePopupShown = true;
        private static bool _displayPopupShown = true;
        private static bool _clusterResolved;

        private readonly Action _showMenu;

        /// <summary>
        /// Two icons, lit and unlit, the way the mute and shortcuts buttons carry one each. Built on the
        /// first draw: the overlay is put together from an editor update, where the skin is not a given.
        /// </summary>
        private GUIContent _onContent;
        private GUIContent _offContent;

        private float _toolbarHeight = FallbackToolbarHeight;
        private float _windowWidth;
        private float _clusterWidth = FallbackClusterWidth;
        private float _buttonWidth = FallbackButtonWidth;
        private float _leftControlsWidth;
        private RectOffset _padding = new RectOffset();
        private Rect _placement = Rect.zero;

        public GameViewRulerToggle(Action showMenu)
        {
            _showMenu = showMenu;

            name = ElementName;
            pickingMode = PickingMode.Position;
            onGUIHandler = OnDrawGUI;

            style.position = Position.Absolute;
            style.top = 0f;
            style.left = 0f;
            style.width = FallbackButtonWidth;
            style.height = FallbackToolbarHeight;

            // IMGUI only knows the pointer is over the button on a repaint carrying its position, so
            // the hover highlight needs a repaint asking for one.
            RegisterCallback<PointerEnterEvent>(_ => MarkDirtyRepaint());
            RegisterCallback<PointerLeaveEvent>(_ => MarkDirtyRepaint());

            // Taken here rather than as an IMGUI ContextClick: the container never sees one, because
            // UI Toolkit keeps the right button for itself and only forwards the left to IMGUI.
            RegisterCallback<PointerDownEvent>(OnPointerDown);
        }

        /// <summary>
        /// Toolbar height comes from the window rather than the style, because a Game View can be told
        /// to hide its toolbar. Placement runs off the last measurements so the button keeps up with a
        /// live resize; the measuring itself needs the toolbar styles, so it waits for the next draw.
        /// </summary>
        public void Layout(float toolbarHeight, float windowWidth)
        {
            _toolbarHeight = toolbarHeight > 1f ? toolbarHeight : FallbackToolbarHeight;
            _windowWidth = windowWidth;

            Place(toolbarHeight > 10f);
            MarkDirtyRepaint();
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 1) return;

            _showMenu?.Invoke();
            evt.StopPropagation();
        }

        private void OnDrawGUI()
        {
            GameViewSettings settings = HelpfulEditorSettings.GameView;

            Measure();
            Place(true);

            Rect rect = new Rect(0f, 0f, contentRect.width, contentRect.height);
            if (rect.width < 1f || rect.height < 1f) return;

            GUIContent content = settings.showRulers ? _onContent : _offContent;

            bool shown = GUI.Toggle(rect, settings.showRulers, content, EditorStyles.toolbarButton);
            if (shown == settings.showRulers) return;

            settings.showRulers = shown;
            HelpfulEditorSettings.SaveGameView();
            GameViewModule.Sync();
        }

        /// <summary>
        /// Where GUILayout would have put the button. Above the pinch point that is hard against the
        /// cluster; below it the button rides the left-hand controls outwards and clips at the window
        /// edge, which is what Unity's own buttons do rather than rearranging themselves.
        /// </summary>
        private void Place(bool toolbarVisible)
        {
            float left = Mathf.Max(_leftControlsWidth, _windowWidth - _padding.right - _clusterWidth - _buttonWidth);
            bool visible = toolbarVisible && left < _windowWidth;

            style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!visible) return;

            Rect placement = new Rect(left, _padding.top, _buttonWidth, Mathf.Max(1f, _toolbarHeight - _padding.vertical));
            if (placement == _placement) return;

            _placement = placement;

            style.left = placement.x;
            style.top = placement.y;
            style.width = placement.width;
            style.height = placement.height;
        }

        /// <summary>
        /// Measured every draw rather than cached: these widths move with the editor skin and the font,
        /// and neither change announces itself to the overlay.
        /// </summary>
        private void Measure()
        {
            ResolveCluster();
            EnsureIcons();

            GUIStyle button = EditorStyles.toolbarButton;

            _padding = EditorStyles.toolbar.padding;

            // Widest of the two, so the button does not change size as it is toggled.
            _buttonWidth = Mathf.Ceil(Mathf.Max(button.CalcSize(_onContent).x, button.CalcSize(_offContent).x));
            _clusterWidth = MeasureCluster();
            _leftControlsWidth = MeasureLeftControls();
        }

        private static float MeasureCluster()
        {
            if (_muteContent == null || _shortcutsContent == null || _statsContent == null || _gizmosContent == null)
            {
                return FallbackClusterWidth;
            }

            GUIStyle button = EditorStyles.toolbarButton;

            float width = button.CalcSize(_muteContent).x
                          + button.CalcSize(_shortcutsContent).x
                          + button.CalcSize(_statsContent).x
                          + (_gizmosStyle ?? EditorStyles.toolbarDropDown).CalcSize(_gizmosContent).x;

            return Mathf.Ceil(width) + EditorStyles.toolbar.padding.right;
        }

        /// <summary>
        /// The earliest x the button could sit at: everything Unity draws to its left at that group's
        /// narrowest — the popups, the zoom slider wound in, and the play mode dropdown that sits
        /// between the two flexible spaces.
        /// </summary>
        private static float MeasureLeftControls()
        {
            float width = AspectPopupWidth + ZoomSliderMinWidth + ZoomReadoutWidth + PlayModeBehaviorWidth;

            width += _zoomContent != null ? GUI.skin.label.CalcSize(_zoomContent).x : ZoomReadoutWidth;
            if (_windowTypePopupShown) width += WindowTypePopupWidth + PopupSpacing;
            if (_displayPopupShown) width += DisplayPopupWidth;

            return Mathf.Ceil(width) + EditorStyles.toolbar.padding.left;
        }

        /// <summary>
        /// The contents Unity's own toolbar draws, so the measurement matches whatever it will actually
        /// lay out. Both mute states — and both shortcut states — are the same 16px icon, so only one of
        /// each is needed. Fetched, not guessed, because a language pack translates Stats and Gizmos.
        /// </summary>
        private static void ResolveCluster()
        {
            if (_clusterResolved) return;
            _clusterResolved = true;

            Type gameView = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
            Type styles = typeof(Editor).Assembly.GetType("UnityEditor.GameView+Styles");

            _muteContent = StyleContent(styles, "muteOffContent") ?? EditorGUIUtility.IconContent("GameViewAudio");
            _shortcutsContent = StyleContent(styles, "shortcutsOnContent") ?? EditorGUIUtility.IconContent("Keyboard");
            _statsContent = StyleContent(styles, "statsContent") ?? new GUIContent("Stats");
            _gizmosContent = StyleContent(styles, "gizmosContent") ?? new GUIContent("Gizmos");
            _zoomContent = StyleContent(styles, "zoomSliderContent");

            // The gizmos button is a dropdown toggle whose style is internal; without it the plain
            // dropdown style is close enough that the button lands a pixel or two out at worst.
            _gizmosStyle = SafeInvoke(typeof(EditorStyles), "get_toolbarDropDownToggleRight") as GUIStyle;

            // Two of the left-hand controls are conditional. When they cannot be asked about, they are
            // assumed present: reserving room that is not needed only pushes the button off a touch
            // early in a cramped window, where drawing over Unity's controls would eat their clicks.
            _windowTypePopupShown = SafeInvoke(gameView?.BaseType, "GetAvailableWindowTypes") is ICollection types && types.Count > 1;
            _displayPopupShown = SafeInvoke(typeof(Editor).Assembly.GetType("UnityEditor.Modules.ModuleManager"),
                "ShouldShowMultiDisplayOption") is not bool shown || shown;
        }

        /// <summary>
        /// Unity's grid visibility pair, which is the nearest thing it ships to a rulers icon. The two
        /// differ in shape rather than tint — the off state is the struck-through grid, the way the
        /// shortcuts button has a struck-through keyboard — so the state reads at a glance instead of
        /// resting on a highlight the toolbar background already competes with. Either falls back to
        /// the word, so a missing icon leaves a button that still reads rather than a blank square.
        /// </summary>
        private void EnsureIcons()
        {
            if (_onContent != null && _offContent != null) return;

            const string tooltip = "Show the Game View rulers. Right-click for the guide menu.";

            _onContent = Icon("GridVisible", tooltip);
            _offContent = Icon("GridHidden", tooltip);
        }

        private static GUIContent Icon(string iconName, string tooltip)
        {
            try
            {
                GUIContent icon = EditorGUIUtility.IconContent(iconName);
                if (icon?.image) return new GUIContent(icon.image, tooltip);
            }
            catch (Exception)
            {
                // Falls through to the text button below.
            }

            return new GUIContent("Rulers", tooltip);
        }

        private static GUIContent StyleContent(Type styles, string fieldName)
        {
            FieldInfo field = styles?.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

            try
            {
                return field?.GetValue(null) as GUIContent;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static object SafeInvoke(Type type, string methodName)
        {
            MethodInfo method = type?.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
                null, Type.EmptyTypes, null);

            try
            {
                return method?.Invoke(null, null);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
