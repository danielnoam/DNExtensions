using System;
using System.Collections.Generic;
using UnityEngine;

namespace DNExtensions.HelpfulEditor
{
    [Serializable]
    internal class HierarchySettings
    {
        public bool moduleEnabled = true;

        // Even rows are left untinted and odd rows only ever darken: Unity's hover highlight
        // lightens the row, so a light stripe is indistinguishable from the cursor being there.
        public bool zebraStripesEnabled = true;
        public Color zebraColorEven = new Color(0f, 0f, 0f, 0f);
        public Color zebraColorOdd = new Color(0f, 0f, 0f, 1f);
        public float zebraOpacity = 0.07f;

        public bool treeDepthLinesEnabled = true;
        public Color treeDepthLineColor = new Color(0.5f, 0.5f, 0.5f, 0.35f);
        public LineStyle treeDepthLineStyle = LineStyle.Solid;

        public bool componentStripEnabled = true;
        public int componentStripMaxIcons = 6;
        public float componentIconSize = 14f;
        public bool componentQuickEditEnabled = true;
        public List<string> excludedComponentTypes = new List<string> { "Transform", "RectTransform" };

        public bool childCountEnabled = true;
        public BadgePosition childCountPosition = BadgePosition.RightAligned;
        public bool childCountHideWhenOneOrZero;

        /// <summary>Plays multi-row folds as motion instead of applying them instantly.</summary>
        public bool animatedFoldsEnabled = true;

        /// <summary>Clicking a scene header's name drops down every scene in the project.</summary>
        public bool sceneMenuEnabled = true;

        public KeyBind toggleActiveKey = KeyBind.Of(KeyCode.A);

        /// <summary>Frames the hovered object in the Scene View, without needing it selected or that window focused.</summary>
        public KeyBind focusKey = KeyBind.Of(KeyCode.F);

        public KeyBind expandCollapseKey = KeyBind.Of(KeyCode.E);
        public KeyBind expandCollapseRecursiveKey = KeyBind.Of(KeyCode.E, shift: true);
        public KeyBind collapseAllKey = KeyBind.Of(KeyCode.E, ctrl: true, shift: true);
        public KeyBind isolateKey = KeyBind.Of(KeyCode.E, ctrl: true);
    }

    [Serializable]
    internal class InspectorSettings
    {
        public bool moduleEnabled = true;

        public bool headerBarEnabled = true;
        public float headerBarButtonHeight = 22f;
        public bool fieldSearchEnabled = true;

        // Empty by default: unlike the Hierarchy strip, the header bar is a per-object control panel
        // and hiding the Transform from it would leave a gap where every object has a component.
        public List<string> excludedComponentTypes = new List<string>();
        public bool isolationPersistsAcrossSelection;

        public bool betterTransformEnabled = true;
        public bool betterRectTransformEnabled = true;

        /// <summary>Seeds the proportional lock on both scale rows, local and world.</summary>
        public bool scaleLockDefaultOn;
        public bool resetMenuItemsEnabled = true;

        /// <summary>Adds world position, rotation and scale below the local rows on parented objects.</summary>
        public bool worldFieldsEnabled = true;

        public bool saveInPlayModeEnabled = true;

        /// <summary>Component types that get no save button — ones whose state does not survive a restore usefully.</summary>
        public List<string> saveInPlayModeBlacklist = new List<string>
        {
            "UnityEngine.Canvas",
            "UnityEngine.CanvasRenderer",
            "UnityEngine.Animator",
            "UnityEngine.UI.CanvasScaler",
            "UnityEngine.SignalReceiver"
        };

        public bool cameraAlignButtonsEnabled = true;
        public bool graphicResetColorEnabled = true;
        public bool textMeshProDuplicateMaterialEnabled = true;

        public bool componentDraggerEnabled = true;
        public bool altInvertsMoveCopyDefault;
        public bool transferDependencies = true;
        public List<ComponentDependencyPair> dependencyWhitelist = new List<ComponentDependencyPair>();

        public KeyBind expandCollapseKey = KeyBind.Of(KeyCode.E);

        /// <summary>Same chord the Hierarchy and Project use for the equivalent action.</summary>
        public KeyBind collapseAllKey = KeyBind.Of(KeyCode.E, ctrl: true, shift: true);

        public KeyBind toggleEnabledKey = KeyBind.Of(KeyCode.A);
        public KeyBind focusSearchKey = KeyBind.None;
    }

    [Serializable]
    internal class SceneViewSettings
    {
        public bool moduleEnabled = true;

        /// <summary>Lists every object under the cursor to choose from, in place of Unity 6's own menu.</summary>
        public bool pickerEnabled = true;

        public KeyBind pickerKey = KeyBind.Of(KeyCode.Mouse1, ctrl: true);

        public int pickerMaxResults = 30;
        public int pickerMaxIcons = 6;
        public List<string> pickerExcludedComponentTypes = new List<string> { "Transform", "RectTransform" };

        /// <summary>Outlines the row under the cursor in the Scene View, which is the point of a window over a menu.</summary>
        public bool pickerHighlightEnabled = true;

        public Color pickerHighlightColor = new Color(1f, 0.6f, 0.1f, 0.9f);

        /// <summary>Adds a Snap submenu to the Scene View's right-click menu, and to the Transform's gear menu.</summary>
        public bool snapMenuEnabled = true;

        /// <summary>How far a snap will look for a surface before giving up.</summary>
        public float snapMaxDistance = 1000f;
    }

    /// <summary>One Game View guide, stored as a fraction of the render target so it survives resizing and zoom.</summary>
    [Serializable]
    internal class GameViewGuide
    {
        public bool isHorizontal;
        public float normalizedPosition = 0.5f;
    }

    [Serializable]
    internal class GameViewSettings
    {
        // Off by default: the rulers take a bite out of the Game View, which is not something to
        // impose on someone who never asked for guides.
        public bool moduleEnabled;

        /// <summary>Owned by the Rulers button in the Game View toolbar, which is the only thing that sets it.</summary>
        public bool showRulers = true;

        public bool guidesEnabled = true;

        public bool screenshotEnabled = true;

        /// <summary>
        /// Sizes the Game View to this for the capture and puts it back afterwards, so the shot is the
        /// resolution asked for rather than whatever the window happens to be.
        /// </summary>
        public bool screenshotForceResolution;

        public Vector2Int screenshotResolution = new Vector2Int(1920, 1080);

        public ScreenshotFormat screenshotFormat = ScreenshotFormat.Png;

        /// <summary>JPG only. Unity's own default is 75, which is visibly soft on UI and text.</summary>
        public int screenshotJpgQuality = 90;

        /// <summary>
        /// Leaves the UI layer out and skips the overlay canvases entirely, which are not drawn by a
        /// camera at all. Takes the capture off the Game View's own image and onto a render of its
        /// cameras, so what is saved is no longer quite what the window is showing.
        /// </summary>
        public bool screenshotExcludeUi;

        /// <summary>Relative paths are taken from the project root, so the default travels with the project.</summary>
        public string screenshotFolder = "Screenshots";

        public Color guideColor = new Color(0f, 0.85f, 1f, 0.9f);
        public float guideWidth = 2f;

        public List<GameViewGuide> guides = new List<GameViewGuide>();
    }

    /// <summary>User-added "dependent follows dependency" pair, matched by component type name.</summary>
    [Serializable]
    internal class ComponentDependencyPair
    {
        public string dependencyType;
        public string dependentType;
    }

    [Serializable]
    internal class ProjectModuleSettings
    {
        public bool moduleEnabled = true;

        // The Project window has no hover tint of its own, unlike the Hierarchy, so the suite draws
        // one to keep the two windows feeling the same.
        public bool hoverHighlightEnabled = true;
        public Color hoverColor = new Color(1f, 1f, 1f, 1f);
        public float hoverOpacity = 0.06f;

        // Same reasoning as the Hierarchy's stripes: only ever darken, so a stripe can't be mistaken
        // for the hover highlight.
        public bool zebraStripesEnabled = true;
        public Color zebraColorEven = new Color(0f, 0f, 0f, 0f);
        public Color zebraColorOdd = new Color(0f, 0f, 0f, 1f);
        public float zebraOpacity = 0.07f;

        public bool treeLinesEnabled = true;
        public Color treeLineColor = new Color(0.5f, 0.5f, 0.5f, 0.35f);
        public LineStyle treeLineStyle = LineStyle.Solid;

        public bool twoLineNamesEnabled;
        public bool showFileExtensions;

        public bool folderContentIconsEnabled = true;
        public int folderContentMaxIcons = 4;
        public float folderContentIconSize = 12f;
        public bool folderContentRecursive;

        /// <summary>Off by default: in the right-hand pane the icons compete with the assets themselves.</summary>
        public bool folderContentIconsInObjectView;

        public bool createFolderButtonEnabled = true;

        public bool linkedAssetsEnabled = true;

        /// <summary>Folder names directly under Assets/ that are expected to be symlinks.</summary>
        public List<string> linkedAssetFolders = new List<string> { "LinkedAssets" };

        public bool dragConflictResolutionEnabled = true;
        public ConflictDefaultChoice conflictDefaultChoice = ConflictDefaultChoice.AlwaysAsk;
        public bool cancelIsDefaultOnEscape = true;

        /// <summary>Plays multi-row folds and navigation jumps as motion instead of applying them instantly.</summary>
        public bool animatedFoldsEnabled = true;

        // Mirrors the Hierarchy so the same keys mean the same thing in both windows.
        public KeyBind expandCollapseKey = KeyBind.Of(KeyCode.E);
        public KeyBind expandCollapseRecursiveKey = KeyBind.Of(KeyCode.E, shift: true);
        public KeyBind collapseAllKey = KeyBind.Of(KeyCode.E, ctrl: true, shift: true);
        public KeyBind isolateKey = KeyBind.Of(KeyCode.E, ctrl: true);
        public KeyBind revealInFinderKey = KeyBind.Of(KeyCode.R, ctrl: true);
        public KeyBind quickObjectWindowKey = KeyBind.Of(KeyCode.Mouse0, alt: true);
        /// <summary>
        /// Opens whatever is under the cursor in a new tab: a folder as a second Project window,
        /// anything else as its Properties window.
        /// </summary>
        public KeyBind openInNewTabKey = KeyBind.Of(KeyCode.Mouse2);

        /// <summary>On: the new window becomes a tab beside the one clicked. Off: it floats.</summary>
        public bool autoDock = true;

        /// <summary>Keeps the new window on its folder, which also lets it be named after it.</summary>
        public bool lockFolderWindows = true;

        /// <summary>Dropping a folder on a dock area's tab strip opens it there as its own tab.</summary>
        public bool folderDropCreatesTabEnabled = true;

        /// <summary>Dropping anything else on a tab strip opens its Properties window there instead.</summary>
        public bool objectDropOpensPropertiesEnabled = true;

        /// <summary>Names windows pinned to something after what they show, instead of their window type.</summary>
        public bool windowTitlesEnabled = true;

        public KeyBind closeWindowKey = KeyBind.Of(KeyCode.W, ctrl: true);
        public KeyBind reopenWindowKey = KeyBind.Of(KeyCode.T, ctrl: true, shift: true);

        /// <summary>
        /// Window types Close Focused Window refuses to act on, matched by type name. Empty by
        /// default: closing is undoable, so the core windows no longer need protecting from it.
        /// </summary>
        public List<string> closeWindowExcludedTypes = new List<string>();
        public KeyBind navigateBackKey = KeyBind.Of(KeyCode.Mouse3);
        public KeyBind navigateForwardKey = KeyBind.Of(KeyCode.Mouse4);
    }
}
