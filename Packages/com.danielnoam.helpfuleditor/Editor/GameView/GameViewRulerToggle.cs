using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DNExtensions.HelpfulEditor.GameView
{
    /// <summary>
    /// The Rulers toggle in the Game View's toolbar. Drawn in Unity's own toolbar button style so it
    /// reads as one of the row rather than as something sat on top of it; where it goes is not its
    /// business, the strip reserves the space and places it.
    /// </summary>
    internal sealed class GameViewRulerToggle : GameViewToolbarItem
    {
        public const string ElementName = "helpfuleditor-gameview-ruler-toggle";

        private const float FallbackWidthGuess = 30f;

        /// <summary>
        /// Two icons, lit and unlit, the way the mute and shortcuts buttons carry one each. They differ
        /// in shape rather than tint — the off state is the struck-through grid — because Unity lights
        /// an icon itself when a toolbar toggle is on, which leaves two tints of one glyph identical.
        /// Built on the first draw: the strip is put together from an editor update, where the skin is
        /// not a given.
        /// </summary>
        private GUIContent _onContent;
        private GUIContent _offContent;

        private readonly IMGUIContainer _drawer;

        public GameViewRulerToggle()
        {
            name = ElementName;
            style.height = new StyleLength(StyleKeyword.Auto);

            _drawer = new IMGUIContainer(OnDrawGUI) { pickingMode = PickingMode.Position };

            _drawer.style.flexGrow = 1f;
            Add(_drawer);

            // IMGUI only knows the pointer is over the button on a repaint carrying its position, so
            // the hover highlight needs a repaint asking for one.
            RegisterCallback<PointerEnterEvent>(_ => _drawer.MarkDirtyRepaint());
            RegisterCallback<PointerLeaveEvent>(_ => _drawer.MarkDirtyRepaint());

            // Taken here rather than as an IMGUI ContextClick: the container never sees one, because
            // UI Toolkit keeps the right button for itself and only forwards the left to IMGUI. It has
            // to be the trickling phase — the press lands on the container inside this element, which
            // swallows it, so a callback waiting for the way back up never runs.
            RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
        }

        /// <summary>Nothing to show while the feature is off, and nothing to reserve for it either.</summary>
        public override float MeasureWidth()
        {
            GameViewSettings settings = HelpfulEditorSettings.GameView;
            if (!settings.moduleEnabled || !settings.guidesEnabled) return 0f;

            EnsureIcons();

            GUIStyle button = EditorStyles.toolbarButton;

            // Widest of the two, so the button does not change size as it is toggled.
            return Mathf.Ceil(Mathf.Max(button.CalcSize(_onContent).x, button.CalcSize(_offContent).x));
        }

        public override float FallbackWidth => FallbackWidthGuess;

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 1) return;

            GameViewModule.ShowGuideMenu();
            evt.StopPropagation();
        }

        private void OnDrawGUI()
        {
            GameViewSettings settings = HelpfulEditorSettings.GameView;
            if (!settings.moduleEnabled || !settings.guidesEnabled) return;

            ApplyMeasuredWidth(MeasureWidth());

            Rect rect = new Rect(0f, 0f, _drawer.contentRect.width, _drawer.contentRect.height);
            if (rect.width < 1f || rect.height < 1f) return;

            GUIContent content = settings.showRulers ? _onContent : _offContent;

            bool shown = GUI.Toggle(rect, settings.showRulers, content, EditorStyles.toolbarButton);
            if (shown == settings.showRulers) return;

            settings.showRulers = shown;
            HelpfulEditorSettings.SaveGameView();
            GameViewModule.Sync();
        }

        private void EnsureIcons()
        {
            if (_onContent != null && _offContent != null) return;

            const string tooltip = "Show the Game View rulers. Right-click for the guide menu.";

            _onContent = Icon("GridVisible", tooltip);
            _offContent = Icon("GridHidden", tooltip);
        }

        /// <summary>Falls back to the word, so a missing icon leaves a button that still reads.</summary>
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
    }
}
