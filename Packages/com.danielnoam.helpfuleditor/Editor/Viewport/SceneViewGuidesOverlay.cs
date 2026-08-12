using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace DNExtensions.HelpfulEditor.Viewport
{
    /// <summary>
    /// The Guides toggle, registered as a Scene View toolbar element so it docks alongside Unity's own
    /// tools rather than being drawn over them. It owns <see cref="SceneViewSettings.showRulers"/>,
    /// which takes the rulers and the guides together — the same thing the Game View's Rulers button
    /// owns, and for the same reason: an unobstructed look at the scene is rather defeated by a set of
    /// guides left drawn across it.
    /// </summary>
    [EditorToolbarElement(Id, typeof(SceneView))]
    internal sealed class SceneViewGuidesToggle : EditorToolbarToggle
    {
        public const string Id = "HelpfulEditor/CanvasGuides";

        private const string Tooltip = "Show the canvas guides and rulers. Right-click for the guide menu.";

        public SceneViewGuidesToggle()
        {
            tooltip = Tooltip;

            ApplyIcons();

            value = HelpfulEditorSettings.SceneView.showRulers;

            this.RegisterValueChangedCallback(OnValueChanged);

            // The setting is shared by every open view and by the settings window, so the button reads
            // it again on the way in rather than trusting what it was left showing.
            RegisterCallback<AttachToPanelEvent>(_ => SetValueWithoutNotify(HelpfulEditorSettings.SceneView.showRulers));

            // Has to be the trickling phase: the press lands on the toggle's own input element, which
            // swallows it, so a callback waiting for the way back up never runs.
            RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
        }

        private static void OnValueChanged(ChangeEvent<bool> evt)
        {
            SceneViewSettings settings = HelpfulEditorSettings.SceneView;
            if (settings.showRulers == evt.newValue) return;

            settings.showRulers = evt.newValue;
            HelpfulEditorSettings.SaveSceneView();

            SceneViewGuides.Refresh();
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 1) return;

            SceneViewGuides.ShowGuideMenu();
            evt.StopPropagation();
        }

        /// <summary>Falls back to the word, so a missing icon leaves a button that still reads.</summary>
        private void ApplyIcons()
        {
            Texture2D on = LoadIcon("GridVisible");
            Texture2D off = LoadIcon("GridHidden");

            if (on && off)
            {
                onIcon = on;
                offIcon = off;
                return;
            }

            text = "Guides";
        }

        private static Texture2D LoadIcon(string iconName)
        {
            try
            {
                return EditorGUIUtility.IconContent(iconName)?.image as Texture2D;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Shown by default, because a feature reached only by first knowing to add its overlay is a
    /// feature nobody finds — and taken away entirely while the feature is switched off, rather than
    /// left in the toolbar as a button that does nothing.
    /// </summary>
    [Overlay(typeof(SceneView), OverlayId, "Canvas Guides", true)]
    internal sealed class SceneViewGuidesOverlay : ToolbarOverlay
    {
        private const string OverlayId = "helpfuleditor-canvas-guides";

        /// <summary>
        /// Overlays are made by the window rather than by us, and there is no public way to ask a
        /// Scene View for one, so each keeps its own note of itself for the settings to reach.
        /// </summary>
        private static readonly List<SceneViewGuidesOverlay> Instances = new List<SceneViewGuidesOverlay>();

        /// <summary>
        /// Whether it was this that hid the overlay. Hiding is the same flag the user's own "close
        /// overlay" sets, so without knowing which of the two put it away, switching the feature back
        /// on would reopen an overlay that had been deliberately closed.
        /// </summary>
        private bool _hiddenByFeature;

        private SceneViewGuidesOverlay() : base(SceneViewGuidesToggle.Id)
        {
        }

        private static bool FeatureEnabled
        {
            get
            {
                SceneViewSettings settings = HelpfulEditorSettings.SceneView;

                return settings.moduleEnabled && settings.guidesEnabled;
            }
        }

        public override void OnCreated()
        {
            Instances.Add(this);
            ApplyVisibility();
        }

        public override void OnWillBeDestroyed()
        {
            Instances.Remove(this);
        }

        public static void RefreshVisibility()
        {
            for (int i = Instances.Count - 1; i >= 0; i--)
            {
                if (Instances[i] == null) Instances.RemoveAt(i);
                else Instances[i].ApplyVisibility();
            }
        }

        private void ApplyVisibility()
        {
            if (!FeatureEnabled)
            {
                // Already away, so it was the user who put it there and there is nothing to take back.
                if (!displayed) return;

                _hiddenByFeature = true;
                displayed = false;
                return;
            }

            if (!_hiddenByFeature) return;

            _hiddenByFeature = false;
            displayed = true;
        }
    }
}
