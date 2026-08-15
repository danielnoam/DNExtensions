using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.HelpfulEditor
{
    /// <summary>
    /// Project Settings → DNExtensions → Helpful Editor, with one sub-section per module. Every toggleable feature in the
    /// suite reads its state from here, so nothing is gated by a hardcoded flag in code.
    /// </summary>
    internal static class HelpfulEditorSettingsProvider
    {
        private const string RootPath = "Project/DNExtensions/Helpful Editor";
        private const string FoldoutPrefix = "DNExtensions.HelpfulEditor.Foldout.";

        /// <summary>What one step of EditorGUI.indentLevel is worth in pixels.</summary>
        private const float IndentStep = 15f;

        private const string Hierarchy = "Hierarchy";
        private const string Inspector = "Inspector";
        private const string ProjectModule = "Project";
        private const string GameViewModule = "GameView";
        private const string SceneViewModule = "SceneView";

        private static int _sectionIndent;

        [MenuItem("Tools/DNExtensions/Helpful Editor Settings", false, 1000)]
        public static void OpenSettings()
        {
            SettingsService.OpenProjectSettings(RootPath);
        }

        [SettingsProvider]
        public static SettingsProvider CreateRootProvider()
        {
            return new SettingsProvider(RootPath, SettingsScope.Project)
            {
                label = "Helpful Editor",
                guiHandler = _ => DrawRoot(),
                keywords = new[] { "helpful editor", "hierarchy", "inspector", "project", "scene view", "folder", "tabs" }
            };
        }

        [SettingsProvider]
        public static SettingsProvider CreateHierarchyProvider()
        {
            return new SettingsProvider($"{RootPath}/Hierarchy", SettingsScope.Project)
            {
                label = "Hierarchy",
                guiHandler = _ => DrawHierarchy(),
                keywords = new[] { "zebra", "stripes", "component", "icons", "child count", "isolate" }
            };
        }

        [SettingsProvider]
        public static SettingsProvider CreateInspectorProvider()
        {
            return new SettingsProvider($"{RootPath}/Inspector", SettingsScope.Project)
            {
                label = "Inspector",
                guiHandler = _ => DrawInspector(),
                keywords = new[] { "header", "transform", "rect transform", "component dragger", "isolate", "search" }
            };
        }

        [SettingsProvider]
        public static SettingsProvider CreateProjectProvider()
        {
            return new SettingsProvider($"{RootPath}/Project", SettingsScope.Project)
            {
                label = "Project",
                guiHandler = _ => DrawProject(),
                keywords = new[] { "folder", "icon", "colour", "color", "tabs", "conflict", "replace" }
            };
        }

        [SettingsProvider]
        public static SettingsProvider CreateGameViewProvider()
        {
            return new SettingsProvider($"{RootPath}/Game View", SettingsScope.Project)
            {
                label = "Game View",
                guiHandler = _ => DrawGameView(),
                keywords = new[] { "game view", "guides", "guidelines", "rulers", "safe area" }
            };
        }

        [SettingsProvider]
        public static SettingsProvider CreateSceneViewProvider()
        {
            return new SettingsProvider($"{RootPath}/Scene View", SettingsScope.Project)
            {
                label = "Scene View",
                guiHandler = _ => DrawSceneView(),
                keywords = new[] { "scene view", "viewport", "overlay", "picker", "select", "overlapping", "guides", "rulers", "snap", "canvas" }
            };
        }

        /// <summary>Opened from the Scene View ruler's own context menu.</summary>
        public static void OpenSceneViewSettings()
        {
            SettingsService.OpenProjectSettings($"{RootPath}/Scene View");
        }

        private static void DrawSceneView()
        {
            SceneViewSettings settings = HelpfulEditorSettings.SceneView;

            EditorGUI.BeginChangeCheck();

            settings.moduleEnabled = EditorGUILayout.ToggleLeft(
                new GUIContent("Module Enabled", "Adds the suite's overlays to every Scene View."),
                settings.moduleEnabled, EditorStyles.boldLabel);

            DrawKeyBindNote();

            using (new EditorGUI.DisabledScope(!settings.moduleEnabled))
            {
                BeginSections();

                if (Section(SceneViewModule, "Selection Picker"))
                {
                    settings.pickerEnabled = EditorGUILayout.Toggle(
                        new GUIContent("Enabled", "Lists every object under the cursor in a window to choose from."),
                        settings.pickerEnabled);

                    using (new EditorGUI.DisabledScope(!settings.pickerEnabled))
                    {
                        settings.pickerKey = DrawKeyBind("Open Picker", settings.pickerKey);

                        settings.pickerMaxResults = EditorGUILayout.IntSlider("Max Results", settings.pickerMaxResults, 1, 100);

                        settings.pickerHighlightEnabled = EditorGUILayout.Toggle(
                            new GUIContent("Highlight On Hover", "Outline the hovered row's object in the Scene View, drawn through anything in front of it."),
                            settings.pickerHighlightEnabled);

                        using (new EditorGUI.DisabledScope(!settings.pickerHighlightEnabled))
                        {
                            settings.pickerHighlightColor = EditorGUILayout.ColorField("Highlight Colour", settings.pickerHighlightColor);
                        }

                        settings.pickerMaxIcons = EditorGUILayout.IntSlider("Max Icons", settings.pickerMaxIcons, 1, 20);
                        DrawStringList("Excluded Types", settings.pickerExcludedComponentTypes);
                    }

                    EditorGUILayout.Space(4);
                    IndentedHelpBox(
                        "Click a row to select it, Shift+Click to add it to the selection without closing.\n" +
                        "Up and Down move through the list, Return selects, Escape closes.\n" +
                        "While enabled, the bound click is taken outright — the editor's own menu for that gesture does not open.\n" +
                        "Unity 6000.3 and newer pick through the editor's own PickAllObjects; older versions rebuild the same list by picking repeatedly.\n" +
                        "If a built-in menu still appears alongside this one, rebind or remove its entry in Edit → Shortcuts.",
                        MessageType.Info);
                }

                if (Section(SceneViewModule, "Snap Menu"))
                {
                    settings.snapMenuEnabled = EditorGUILayout.Toggle(
                        new GUIContent("Enabled", "Adds a Snap submenu to the Scene View's right-click menu."),
                        settings.snapMenuEnabled);

                    using (new EditorGUI.DisabledScope(!settings.snapMenuEnabled))
                    {
                        settings.snapMaxDistance = Mathf.Max(0.01f, EditorGUILayout.FloatField(
                            new GUIContent("Max Distance", "How far a snap looks for a surface before giving up."),
                            settings.snapMaxDistance));
                    }

                    EditorGUILayout.Space(4);
                    IndentedHelpBox(
                        "Right-click a selected object in the Scene View and choose Snap → Floor, Ceiling or Nearest Wall.\n" +
                        "Nearest Wall casts the four world horizontals and takes whichever needs the least movement.\n" +
                        "The object lands with the leading face of its bounds on the surface, so an off-centre pivot still sits correctly.\n" +
                        "The (Pivot) variants land the object's origin on the surface instead, for pivots deliberately placed at a contact point.\n" +
                        "Snapping needs a collider to land on — triggers and the object's own colliders are ignored, and renderer bounds are what get measured.\n" +
                        "Each selected object snaps separately, and every snap is undoable.\n" +
                        "Snap sits at the top of the menu alongside Grid and Isolate, and shows under Unity's default GameObject tool context.\n" +
                        "Unity 6000.2 and older have no Scene View context menu, so there the same entries appear in the Transform's gear menu instead.",
                        MessageType.Info);
                }

                if (Section(SceneViewModule, "Canvas Guides"))
                {
                    settings.guidesEnabled = EditorGUILayout.Toggle(
                        new GUIContent("Enabled", "Draws guides and rulers on the canvas being edited, for placing UI against."),
                        settings.guidesEnabled);

                    using (new EditorGUI.DisabledScope(!settings.guidesEnabled))
                    {
                        settings.guideColor = EditorGUILayout.ColorField("Colour", settings.guideColor);
                        settings.guideWidth = EditorGUILayout.Slider("Width", settings.guideWidth, 0.5f, 8f);

                        settings.guideOutlineColor = EditorGUILayout.ColorField(
                            new GUIContent("Outline Colour", "Bordered rather than doubled — the outline is a wider line of the same shape drawn underneath."),
                            settings.guideOutlineColor);

                        settings.guideOutlineWidth = EditorGUILayout.Slider("Outline Width", settings.guideOutlineWidth, 0f, 4f);

                        settings.guideSnapColor = EditorGUILayout.ColorField(
                            new GUIContent("Snapped Colour", "What a guide turns while it is holding a rect."),
                            settings.guideSnapColor);

                        settings.guideSnapEnabled = EditorGUILayout.Toggle(
                            new GUIContent("Snap Rects", "Pulls a rect being dragged onto a guide once its edge or centre comes close enough."),
                            settings.guideSnapEnabled);

                        using (new EditorGUI.DisabledScope(!settings.guideSnapEnabled))
                        {
                            settings.guideSnapDistance = EditorGUILayout.Slider("Snap Distance", settings.guideSnapDistance, 1f, 32f);
                        }
                    }

                    EditorGUILayout.Space(4);
                    IndentedHelpBox(
                        "Guides belong to the root canvas of whatever is selected, and stay put when the selection moves off UI.\n" +
                        "The Canvas Guides overlay in the Scene View toolbar shows and hides the rulers and the guides together; right-click it, or a ruler, for the guide menu.\n" +
                        "Drag off the top ruler for a vertical guide, off the left ruler for a horizontal one.\n" +
                        "Drag a guide back onto a ruler, or off the canvas, to delete it.\n" +
                        "Hold Alt while dragging to snap to the centre, Shift to move in 10px steps.\n" +
                        "Positions are held against the canvas rect, so they survive a reference resolution change.\n" +
                        "The rulers are pinned to the window and appear only while the view is square to the canvas — orbit away and the guides stay, dimmed, but nothing can be placed against them.\n" +
                        "Like the Game View's, the rulers take the top-left 18 pixels of the view, and whatever Scene View overlay is sitting there with them.\n" +
                        "Snapping corrects the rect after Unity's own tools have moved it, so the Move and Rect tools keep working as they do; a rect being resized is left alone.\n" +
                        "A snap holds until it is pulled well clear of where it was made, so a rect can be slid along a guide without it letting go.\n" +
                        "Hold Ctrl (Cmd on a Mac) to stand this down mid-drag and let Unity's own grid snapping have the rect instead.\n" +
                        "Only a rect being moved is corrected — resizing, rotating and scaling are all left alone.\n" +
                        "Right-click a ruler and use From Selection to drop guides on the edges and centre of whatever is selected.",
                        MessageType.Info);
                }

                if (Section(SceneViewModule, "Canvas Grid"))
                {
                    settings.gridEnabled = EditorGUILayout.Toggle(
                        new GUIContent("Enabled", "Draws a grid in the canvas' own units on the canvas plane."),
                        settings.gridEnabled);

                    using (new EditorGUI.DisabledScope(!settings.gridEnabled))
                    {
                        settings.gridCellSize = Mathf.Max(1f, EditorGUILayout.FloatField(
                            new GUIContent("Cell Size", "In the canvas' own units, which for a scaled canvas is reference pixels."),
                            settings.gridCellSize));

                        settings.gridSubdivisions = EditorGUILayout.IntSlider("Subdivisions", settings.gridSubdivisions, 0, 16);

                        settings.gridColor = EditorGUILayout.ColorField("Colour", settings.gridColor);
                        settings.gridSubdivisionColor = EditorGUILayout.ColorField("Subdivision Colour", settings.gridSubdivisionColor);

                        settings.gridSnapEnabled = EditorGUILayout.Toggle(
                            new GUIContent("Snap Rects", "Snaps to the finest division on show. Guides always win over the grid."),
                            settings.gridSnapEnabled);
                    }

                    EditorGUILayout.Space(4);
                    EditorGUILayout.HelpBox(
                        "The Grid button in the Canvas Guides overlay shows and hides it; the guide menu carries the same toggle.\n" +
                        "Cells are counted from the canvas' left and top edges, so a line at 100 is where the ruler reads 100.\n" +
                        "Lines are dropped once they would fall closer together than five screen points, so zooming out thins the grid rather than filling it in.\n" +
                        "Snapping prefers a guide whenever one is in range, and only falls back to the grid when none is.",
                        MessageType.Info);
                }

                EndSections();
            }

            if (EditorGUI.EndChangeCheck())
            {
                HelpfulEditorSettings.SaveSceneView();
                Viewport.SceneViewGuides.Refresh();
            }

            DrawResetButton("Scene View", HelpfulEditorSettings.ResetSceneView);
        }

        /// <summary>Opened from the Game View ruler's own context menu.</summary>
        public static void OpenGameViewSettings()
        {
            SettingsService.OpenProjectSettings($"{RootPath}/Game View");
        }

        private static void DrawGameView()
        {
            GameViewSettings settings = HelpfulEditorSettings.GameView;

            EditorGUI.BeginChangeCheck();

            settings.moduleEnabled = EditorGUILayout.ToggleLeft(
                new GUIContent("Module Enabled", "Adds the guide rulers and the toolbar buttons to every Game View."),
                settings.moduleEnabled, EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(!settings.moduleEnabled))
            {
                BeginSections();

                if (Section(GameViewModule, "Guides & Rulers"))
                {
                    settings.guidesEnabled = EditorGUILayout.Toggle(
                        new GUIContent("Enabled", "The rulers, the toolbar's Rulers button and the guides are one feature — this takes the overlay off the Game View entirely."),
                        settings.guidesEnabled);

                    using (new EditorGUI.DisabledScope(!settings.guidesEnabled))
                    {
                        settings.guideColor = EditorGUILayout.ColorField("Colour", settings.guideColor);
                        settings.guideWidth = EditorGUILayout.Slider("Width", settings.guideWidth, 0.5f, 8f);
                    }

                    EditorGUILayout.Space(4);
                    IndentedHelpBox(
                        "Drag off the top ruler for a vertical guide, off the left ruler for a horizontal one.\n" +
                        "Drag a guide back onto a ruler to delete it.\n" +
                        "Hold Alt while dragging to snap to the centre, Shift to move in 10px steps.\n" +
                        "The toolbar's Rulers button shows and hides the rulers and the guides together; right-click it, or a ruler, for the guide menu.\n" +
                        "Positions are held against the render target, so they survive resizing and zooming.",
                        MessageType.Info);
                }

                if (Section(GameViewModule, "Screenshot"))
                {
                    settings.screenshotEnabled = EditorGUILayout.Toggle(
                        new GUIContent("Enabled", "Adds a camera button to the Game View toolbar that saves a PNG on the spot."),
                        settings.screenshotEnabled);

                    using (new EditorGUI.DisabledScope(!settings.screenshotEnabled))
                    {
                        DrawScreenshotFolder(settings);

                        settings.screenshotFormat = (ScreenshotFormat)EditorGUILayout.EnumPopup(
                            new GUIContent("Format", "PNG keeps alpha and is lossless. JPG is smaller but cannot store transparency."),
                            settings.screenshotFormat);

                        using (new EditorGUI.DisabledScope(settings.screenshotFormat != ScreenshotFormat.Jpg))
                        {
                            settings.screenshotJpgQuality = EditorGUILayout.IntSlider("JPG Quality", settings.screenshotJpgQuality, 1, 100);
                        }

                        settings.screenshotExcludeUi = EditorGUILayout.Toggle(
                            new GUIContent("Exclude UI", "Leave the UI out of the capture: overlay canvases are skipped entirely and the UI layer is culled."),
                            settings.screenshotExcludeUi);

                        settings.screenshotForceResolution = EditorGUILayout.Toggle(
                            new GUIContent("Force Resolution", "Sizes the Game View to a set resolution for the capture and puts it back afterwards."),
                            settings.screenshotForceResolution);

                        using (new EditorGUI.DisabledScope(!settings.screenshotForceResolution))
                        {
                            Vector2Int resolution = EditorGUILayout.Vector2IntField("Resolution", settings.screenshotResolution);

                            settings.screenshotResolution = new Vector2Int(
                                Mathf.Clamp(resolution.x, 1, 8192),
                                Mathf.Clamp(resolution.y, 1, 8192));
                        }
                    }

                    EditorGUILayout.Space(4);
                    IndentedHelpBox(
                        "Files are named GameView <date> <time> <width>x<height>, at the game's own resolution rather than the window's.\n" +
                        "A relative folder is taken from the project root, so it travels with the project.\n" +
                        "Forcing a resolution resizes the Game View for a moment — it will visibly flick to that size and back.\n" +
                        "Excluding the UI switches to rendering the cameras directly instead of saving the Game View's own image: no resize flicker, but what lands on disk is no longer guaranteed to match the window pixel for pixel.\n" +
                        "Right-click the button to open the folder.",
                        MessageType.Info);
                }

                if (Section(GameViewModule, "Recording"))
                {
                    settings.recordingEnabled = EditorGUILayout.Toggle(
                        new GUIContent("Enabled", "Adds a record button to the Game View toolbar that saves an MP4."),
                        settings.recordingEnabled);

                    using (new EditorGUI.DisabledScope(!settings.recordingEnabled))
                    {
                        DrawRecordingFolder(settings);

                        // Both are read when a recording starts and baked into the encoder, so the
                        // toolbar's own menu greys them out mid-take rather than letting them lie.
                        using (new EditorGUI.DisabledScope(GameView.GameViewRecording.IsActive))
                        {
                            settings.recordingMode = (RecordingMode)EditorGUILayout.EnumPopup(
                                new GUIContent("Mode", "Real Time is the suite's own recorder: it follows the clock and works outside play mode, without audio. Unity Recorder hands the take to that package, which captures every frame in step with the game and records its audio."),
                                settings.recordingMode);

                            if (settings.recordingMode == RecordingMode.Recorder && !GameView.GameViewRecorderBridge.Available)
                            {
                                IndentedHelpBox("The Unity Recorder package is not installed, so this mode cannot record. Install com.unity.recorder, or switch back to Real Time.",
                                    MessageType.Warning);
                            }

                            settings.recordingFps = EditorGUILayout.IntPopup("Frame Rate",
                                settings.recordingFps, new[] { "24", "30", "60" }, new[] { 24, 30, 60 });

                            settings.recordingQuality = (RecordingQuality)EditorGUILayout.EnumPopup(
                                new GUIContent("Quality", "The bit rate the video is written at."),
                                settings.recordingQuality);

                            settings.recordingForceResolution = EditorGUILayout.Toggle(
                                new GUIContent("Force Resolution", "Hold the Game View at a set resolution for the whole recording, and put it back afterwards."),
                                settings.recordingForceResolution);

                            using (new EditorGUI.DisabledScope(!settings.recordingForceResolution))
                            {
                                Vector2Int resolution = EditorGUILayout.Vector2IntField("Resolution", settings.recordingResolution);

                                settings.recordingResolution = new Vector2Int(
                                    Mathf.Clamp(resolution.x, 1, 8192),
                                    Mathf.Clamp(resolution.y, 1, 8192));
                            }
                        }
                    }

                    EditorGUILayout.Space(4);
                    IndentedHelpBox(
                        "Files are named the same way captures are, at the game's own resolution rather than the window's.\n" +
                        "Recording writes to a temporary file and only moves it into the folder when you stop, so a discarded take leaves nothing behind.\n" +
                        "While recording the button becomes pause and stop. Right-click it for the rate, the quality, the folder, and to discard the take instead of saving it.\n" +
                        "Real Time follows the clock and records whatever the editor draws, in play mode or out of it. A slow frame is held rather than dropped, so the video stays the length the take took. No audio — capturing it needs a frame driver inside the running game, which this deliberately does not have.\n" +
                        "Unity Recorder hands the take to that package, which does have one: every frame captured in step with the game, with audio. Pressing record out of play mode starts the game and picks the take up once it is running. It needs the package installed, the game runs slower than real time while it records, and there is no pause — only stop.\n" +
                        "Forcing a resolution holds the Game View at that size for the whole take, not just a moment, so the window visibly changes size until you stop. Real Time opens its encoder only once the view has reached it; Recorder is told the size directly.\n" +
                        "Quality is a bit rate in Real Time and Recorder's own quality steps otherwise, so Ultra records as High there.\n" +
                        "One recording at a time, and it follows the Game View it was started from. Reloading scripts ends it.",
                        MessageType.Info);
                }

                EndSections();
            }

            if (EditorGUI.EndChangeCheck())
            {
                HelpfulEditorSettings.SaveGameView();
                GameView.GameViewModule.Sync();
            }

            DrawResetButton("Game View", HelpfulEditorSettings.ResetGameView);
        }

        /// <summary>
        /// Typed or browsed to. What comes back from the panel is absolute, and is written back
        /// project-relative when it lands inside the project so the setting is worth committing.
        /// </summary>
        private static void DrawScreenshotFolder(GameViewSettings settings)
        {
            EditorGUILayout.BeginHorizontal();

            settings.screenshotFolder = EditorGUILayout.TextField(
                new GUIContent("Folder", "Where screenshots are saved. Relative paths are taken from the project root."),
                settings.screenshotFolder);

            if (GUILayout.Button("Browse…", GUILayout.Width(70f)))
            {
                string picked = EditorUtility.OpenFolderPanel("Screenshot Folder", GameView.GameViewScreenshot.ResolveFolder(), string.Empty);

                if (!string.IsNullOrEmpty(picked))
                {
                    settings.screenshotFolder = GameView.GameViewScreenshot.ToSettingPath(picked);
                    GUI.changed = true;
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>The recordings folder, browsed and stored the same way the screenshot one is.</summary>
        private static void DrawRecordingFolder(GameViewSettings settings)
        {
            EditorGUILayout.BeginHorizontal();

            settings.recordingFolder = EditorGUILayout.TextField(
                new GUIContent("Folder", "Where recordings are saved. Relative paths are taken from the project root."),
                settings.recordingFolder);

            if (GUILayout.Button("Browse…", GUILayout.Width(70f)))
            {
                string picked = EditorUtility.OpenFolderPanel("Recording Folder", GameView.GameViewRecording.ResolveFolder(), string.Empty);

                if (!string.IsNullOrEmpty(picked))
                {
                    settings.recordingFolder = GameView.GameViewScreenshot.ToSettingPath(picked);
                    GUI.changed = true;
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private static void DrawRoot()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Helpful Editor", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Editor toolkit for the Hierarchy, Inspector, Project, Game View and Scene View windows.", EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Settings are stored as JSON under ProjectSettings/HelpfulEditor/ and are tracked by source control.",
                EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);

            IndentedHelpBox(GlobalKeyCapture.Available
                    ? "Hover keybinds are active and fire regardless of which window has focus."
                    : "Hover keybinds could not hook Unity's global event handler — they will only fire while the target window has focus.",
                GlobalKeyCapture.Available ? MessageType.Info : MessageType.Warning);
        }

        private static void DrawHierarchy()
        {
            HierarchySettings settings = HelpfulEditorSettings.Hierarchy;

            EditorGUI.BeginChangeCheck();

            settings.moduleEnabled = EditorGUILayout.ToggleLeft("Module Enabled", settings.moduleEnabled, EditorStyles.boldLabel);
            DrawKeyBindNote();

            using (new EditorGUI.DisabledScope(!settings.moduleEnabled))
            {
                BeginSections();

                if (Section(Hierarchy, "Zebra Stripes"))
                {
                    settings.zebraStripesEnabled = EditorGUILayout.Toggle("Enabled", settings.zebraStripesEnabled);

                    using (new EditorGUI.DisabledScope(!settings.zebraStripesEnabled))
                    {
                        settings.zebraColorEven = EditorGUILayout.ColorField("Even Rows", settings.zebraColorEven);
                        settings.zebraColorOdd = EditorGUILayout.ColorField("Odd Rows", settings.zebraColorOdd);
                        settings.zebraOpacity = EditorGUILayout.Slider("Opacity", settings.zebraOpacity, 0f, 1f);
                    }
                }

                if (Section(Hierarchy, "Tree Lines"))
                {
                    settings.treeLinesEnabled = EditorGUILayout.Toggle("Enabled", settings.treeLinesEnabled);

                    using (new EditorGUI.DisabledScope(!settings.treeLinesEnabled))
                    {
                        settings.treeLineColor = EditorGUILayout.ColorField("Colour", settings.treeLineColor);
                        settings.treeLineStyle = (LineStyle)EditorGUILayout.EnumPopup("Style", settings.treeLineStyle);
                    }
                }

                if (Section(Hierarchy, "Component Strip"))
                {
                    settings.componentStripEnabled = EditorGUILayout.Toggle("Enabled", settings.componentStripEnabled);

                    using (new EditorGUI.DisabledScope(!settings.componentStripEnabled))
                    {
                        settings.componentStripMaxIcons = EditorGUILayout.IntSlider("Max Icons", settings.componentStripMaxIcons, 1, 20);
                        settings.componentStripIconSize = EditorGUILayout.Slider("Icon Size", settings.componentStripIconSize, 8f, 24f);
                        settings.componentQuickEditEnabled = EditorGUILayout.Toggle(
                            new GUIContent("Alt+Click Quick Edit", "Alt+Click a component icon to open it in a floating mini inspector."),
                            settings.componentQuickEditEnabled);
                        DrawStringList("Excluded Types", settings.excludedComponentTypes);
                    }
                }

                if (Section(Hierarchy, "Child Count"))
                {
                    settings.childCountEnabled = EditorGUILayout.Toggle("Enabled", settings.childCountEnabled);

                    using (new EditorGUI.DisabledScope(!settings.childCountEnabled))
                    {
                        settings.childCountPosition = (BadgePosition)EditorGUILayout.EnumPopup("Position", settings.childCountPosition);
                        settings.childCountHideWhenOneOrZero = EditorGUILayout.Toggle("Hide When ≤ 1", settings.childCountHideWhenOneOrZero);
                    }
                }

                if (Section(Hierarchy, "Folding"))
                {
                    settings.animatedFoldsEnabled = EditorGUILayout.Toggle(
                        new GUIContent("Animate", "Play Collapse Everything and Isolate one fold at a time instead of applying them instantly."),
                        settings.animatedFoldsEnabled);

                    settings.expandCollapseKey = DrawKeyBind("Expand / Collapse", settings.expandCollapseKey);
                    settings.expandCollapseRecursiveKey = DrawKeyBind("Expand / Collapse All", settings.expandCollapseRecursiveKey);
                    settings.collapseAllKey = DrawKeyBind("Collapse Everything", settings.collapseAllKey);

                    // Isolation is a fold: it collapses everything that is not on the path to the
                    // target, and it is played by the same queue Animate governs.
                    settings.isolateKey = DrawKeyBind("Isolate", settings.isolateKey);
                }

                if (Section(Hierarchy, "Row Actions"))
                {
                    settings.toggleActiveKey = DrawKeyBind("Toggle Active", settings.toggleActiveKey);
                    settings.focusKey = DrawKeyBind("Focus In Scene View", settings.focusKey);

                    EditorGUILayout.Space(4);
                    IndentedHelpBox(
                        "Both act on the row under the cursor rather than on the selection, so the Hierarchy does not have to be focused first.",
                        MessageType.Info);
                }

                if (Section(Hierarchy, "Scene Menu"))
                {
                    settings.sceneMenuEnabled = EditorGUILayout.Toggle(
                        new GUIContent("Enabled", "Clicking a scene header's name in the Hierarchy drops down every scene in the project."),
                        settings.sceneMenuEnabled);

                    EditorGUILayout.Space(4);
                    IndentedHelpBox(
                        "Every scene in the project is listed flat by name, with the open ones ticked — deliberately not grouped by folder, which would turn picking a scene into navigating the project.\n" +
                        "Star a scene to lift it to the top of the list; stars are per user and per project.\n" +
                        "Choosing one opens it, asking to save first if anything is unsaved; Shift+Click, or Shift+Return, loads it additively instead.\n" +
                        "Up and Down move through the list, Return opens, Escape closes. Scenes cannot be opened during play mode.\n" +
                        "Only the name is clickable — an arrow appears at the end of it under the cursor, and the rest of the header still selects the scene while right-click still opens Unity's own menu.",
                        MessageType.Info);
                }

                EndSections();
            }

            if (EditorGUI.EndChangeCheck()) HelpfulEditorSettings.SaveHierarchy();

            DrawResetButton("Hierarchy", HelpfulEditorSettings.ResetHierarchy);
        }

        private static void DrawInspector()
        {
            InspectorSettings settings = HelpfulEditorSettings.Inspector;

            EditorGUI.BeginChangeCheck();

            settings.moduleEnabled = EditorGUILayout.ToggleLeft("Module Enabled", settings.moduleEnabled, EditorStyles.boldLabel);
            DrawKeyBindNote();

            using (new EditorGUI.DisabledScope(!settings.moduleEnabled))
            {
                BeginSections();

                if (Section(Inspector, "Object Header Bar"))
                {
                    settings.headerBarEnabled = EditorGUILayout.Toggle("Enabled", settings.headerBarEnabled);

                    // The search bar and the isolation control are both drawn by the header bar, so
                    // with it off there is nothing for any of these to govern.
                    using (new EditorGUI.DisabledScope(!settings.headerBarEnabled))
                    {
                        settings.headerBarButtonHeight = EditorGUILayout.Slider("Button Height", settings.headerBarButtonHeight, 16f, 32f);
                        settings.fieldSearchEnabled = EditorGUILayout.Toggle(
                            new GUIContent("Field Search Bar", "Search field that filters properties across every component on the object."),
                            settings.fieldSearchEnabled);

                        using (new EditorGUI.DisabledScope(!settings.fieldSearchEnabled))
                        {
                            settings.focusSearchKey = DrawKeyBind("Focus Search", settings.focusSearchKey);
                        }

                        settings.isolationPersistsAcrossSelection = EditorGUILayout.Toggle("Isolation Persists", settings.isolationPersistsAcrossSelection);
                        DrawStringList("Excluded Types", settings.excludedComponentTypes);
                    }
                }

                if (Section(Inspector, "Component Actions"))
                {
                    settings.expandCollapseKey = DrawKeyBind("Expand / Collapse", settings.expandCollapseKey);
                    settings.toggleEnabledKey = DrawKeyBind("Toggle Enabled", settings.toggleEnabledKey);
                    settings.collapseAllKey = DrawKeyBind("Collapse Everything", settings.collapseAllKey);

                    EditorGUILayout.Space(4);
                    IndentedHelpBox(
                        "Expand / Collapse and Toggle Enabled act on the component under the cursor in the Inspector body, falling back to the header bar button when the cursor is up in the bar.\n" +
                        "Collapse Everything acts on the whole object, so it needs nothing under the cursor.\n" +
                        "None of them need the header bar, and they keep working with it switched off.",
                        MessageType.Info);
                }

                if (Section(Inspector, "Transform Inspector"))
                {
                    settings.betterTransformEnabled = EditorGUILayout.Toggle(
                        new GUIContent("Better Transform", "Replaces Unity's Transform inspector with one whose rows carry copy, paste and reset buttons."),
                        settings.betterTransformEnabled);

                    settings.betterRectTransformEnabled = EditorGUILayout.Toggle(
                        new GUIContent("Better Rect Transform", "The same for RectTransform. Separate because that inspector is the larger rebuild of the two — turning it off falls back to Unity's own while leaving the Transform one in place."),
                        settings.betterRectTransformEnabled);

                    // The rest describe rows that only exist inside those two inspectors, so with
                    // both off there is nothing for them to govern.
                    using (new EditorGUI.DisabledScope(!settings.betterTransformEnabled && !settings.betterRectTransformEnabled))
                    {
                        settings.scaleLockDefaultOn = EditorGUILayout.Toggle(
                            new GUIContent("Scale Lock Default On", "Start the proportional lock on the scale rows switched on. Applies to the local and world rows alike."),
                            settings.scaleLockDefaultOn);
                        settings.resetMenuItemsEnabled = EditorGUILayout.Toggle("Show Reset Menu Items", settings.resetMenuItemsEnabled);
                        settings.worldFieldsEnabled = EditorGUILayout.Toggle(
                            new GUIContent("World Fields", "Adds world position, rotation and scale below the local rows. Only shown on objects with a parent, since a root object's local values already are its world values."),
                            settings.worldFieldsEnabled);
                    }
                }

                if (Section(Inspector, "Save In Play Mode"))
                {
                    settings.saveInPlayModeEnabled = EditorGUILayout.Toggle(
                        new GUIContent("Enabled", "Adds a save button to component headers during play mode. Marked components are restored after returning to edit mode."),
                        settings.saveInPlayModeEnabled);

                    using (new EditorGUI.DisabledScope(!settings.saveInPlayModeEnabled))
                    {
                        DrawStringList("Blacklisted Types", settings.saveInPlayModeBlacklist);
                    }
                }

                if (Section(Inspector, "Component Header Buttons"))
                {
                    settings.cameraAlignButtonsEnabled = EditorGUILayout.Toggle(
                        new GUIContent("Camera Align", "Adds buttons to Camera headers that align the camera to the Scene View, or the Scene View to the camera."),
                        settings.cameraAlignButtonsEnabled);
                    settings.graphicResetColorEnabled = EditorGUILayout.Toggle(
                        new GUIContent("Reset Color", "Adds a button to UI Graphic and TextMeshPro headers that resets the color to white."),
                        settings.graphicResetColorEnabled);
                    settings.textMeshProDuplicateMaterialEnabled = EditorGUILayout.Toggle(
                        new GUIContent("TMP Duplicate Material", "Adds a button to TextMeshPro headers that copies the font material into a new asset and assigns it."),
                        settings.textMeshProDuplicateMaterialEnabled);
                }

                if (Section(Inspector, "Component Dragger"))
                {
                    settings.componentDraggerEnabled = EditorGUILayout.Toggle("Enabled", settings.componentDraggerEnabled);

                    using (new EditorGUI.DisabledScope(!settings.componentDraggerEnabled))
                    {
                        settings.altInvertsMoveCopyDefault = EditorGUILayout.Toggle(
                            new GUIContent("Alt Inverts Move/Copy", "Off: drag moves, Alt copies. On: drag copies, Alt moves."),
                            settings.altInvertsMoveCopyDefault);
                        settings.transferDependencies = EditorGUILayout.Toggle("Transfer Dependencies", settings.transferDependencies);

                        // The whitelist is nothing but extra pairs for the transfer to follow.
                        using (new EditorGUI.DisabledScope(!settings.transferDependencies))
                        {
                            DrawDependencyWhitelist(settings.dependencyWhitelist);
                        }
                    }
                }

                EndSections();
            }

            if (EditorGUI.EndChangeCheck()) HelpfulEditorSettings.SaveInspector();

            DrawResetButton("Inspector", HelpfulEditorSettings.ResetInspector);
        }

        private static void DrawProject()
        {
            ProjectSettings settings = HelpfulEditorSettings.Project;

            EditorGUI.BeginChangeCheck();

            settings.moduleEnabled = EditorGUILayout.ToggleLeft("Module Enabled", settings.moduleEnabled, EditorStyles.boldLabel);
            DrawKeyBindNote();

            using (new EditorGUI.DisabledScope(!settings.moduleEnabled))
            {
                BeginSections();

                if (Section(ProjectModule, "Hover Highlight"))
                {
                    settings.hoverHighlightEnabled = EditorGUILayout.Toggle(
                        new GUIContent("Enabled", "The Project window has no hover tint of its own, unlike the Hierarchy."),
                        settings.hoverHighlightEnabled);

                    using (new EditorGUI.DisabledScope(!settings.hoverHighlightEnabled))
                    {
                        settings.hoverColor = EditorGUILayout.ColorField("Colour", settings.hoverColor);
                        settings.hoverOpacity = EditorGUILayout.Slider("Opacity", settings.hoverOpacity, 0f, 1f);
                    }
                }

                if (Section(ProjectModule, "Zebra Stripes"))
                {
                    settings.zebraStripesEnabled = EditorGUILayout.Toggle("Enabled", settings.zebraStripesEnabled);

                    using (new EditorGUI.DisabledScope(!settings.zebraStripesEnabled))
                    {
                        settings.zebraColorEven = EditorGUILayout.ColorField("Even Rows", settings.zebraColorEven);
                        settings.zebraColorOdd = EditorGUILayout.ColorField("Odd Rows", settings.zebraColorOdd);
                        settings.zebraOpacity = EditorGUILayout.Slider("Opacity", settings.zebraOpacity, 0f, 1f);
                    }
                }

                if (Section(ProjectModule, "Tree Lines"))
                {
                    settings.treeLinesEnabled = EditorGUILayout.Toggle("Enabled", settings.treeLinesEnabled);

                    using (new EditorGUI.DisabledScope(!settings.treeLinesEnabled))
                    {
                        settings.treeLineColor = EditorGUILayout.ColorField("Colour", settings.treeLineColor);
                        settings.treeLineStyle = (LineStyle)EditorGUILayout.EnumPopup("Style", settings.treeLineStyle);
                    }
                }

                if (Section(ProjectModule, "Folding"))
                {
                    settings.animatedFoldsEnabled = EditorGUILayout.Toggle(
                        new GUIContent("Animate", "Play multi-row folds one at a time, and glide to where back/forward lands instead of jumping there."),
                        settings.animatedFoldsEnabled);

                    settings.expandCollapseKey = DrawKeyBind("Expand / Collapse", settings.expandCollapseKey);
                    settings.expandCollapseRecursiveKey = DrawKeyBind("Expand / Collapse All", settings.expandCollapseRecursiveKey);
                    settings.collapseAllKey = DrawKeyBind("Collapse Everything", settings.collapseAllKey);

                    // Isolation is a fold, played by the same queue Animate governs.
                    settings.isolateKey = DrawKeyBind("Isolate", settings.isolateKey);
                }

                if (Section(ProjectModule, "Row Actions"))
                {
                    settings.revealInFinderKey = DrawKeyBind($"Reveal In {HelpfulEditorPlatform.FileManagerName}", settings.revealInFinderKey);
                    settings.quickObjectWindowKey = DrawKeyBind("Quick Object Window", settings.quickObjectWindowKey);

                    EditorGUILayout.Space(4);
                    IndentedHelpBox(
                        "Both act on the row under the cursor rather than on the selection, so the Project window does not have to be focused first.\n" +
                        "Quick Object Window opens whatever is under the cursor in a floating window — a Properties window, or a folder tab when it is a folder. Open In New Tab, under Tabs, is the docking counterpart.",
                        MessageType.Info);
                }

                if (Section(ProjectModule, "Navigation"))
                {
                    settings.navigateBackKey = DrawKeyBind("Navigate Back", settings.navigateBackKey);
                    settings.navigateForwardKey = DrawKeyBind("Navigate Forward", settings.navigateForwardKey);

                    EditorGUILayout.Space(4);
                    IndentedHelpBox(
                        "Moves through the folders the Project window has browsed, the way back and forward work in a browser.\n" +
                        "Whether the jump is animated or instant is Animate, under Folding.",
                        MessageType.Info);
                }

                if (Section(ProjectModule, "Window History"))
                {
                    settings.closeWindowKey = DrawKeyBind("Close Focused Window", settings.closeWindowKey);
                    settings.reopenWindowKey = DrawKeyBind("Reopen Closed Window", settings.reopenWindowKey);

                    DrawStringList("Never Close These Windows", settings.closeWindowExcludedTypes);
                    EditorGUILayout.LabelField("Closing is undoable, so this list is usually best left empty.", EditorStyles.miniLabel);

                    if (HelpfulEditorPlugins.VTabsActive)
                    {
                        IndentedHelpBox("vTabs binds the same defaults for close and reopen, acting on the tab rather than the window. Both will fire — rebind one side.",
                            MessageType.Warning);
                    }

                    EditorGUILayout.Space(4);
                    IndentedHelpBox(
                        "A closed window is remembered with where it was docked, so reopening puts it back in its tab strip rather than floating.\n" +
                        "Folder tabs come back on their folder and Properties windows on their object.",
                        MessageType.Info);
                }

                if (Section(ProjectModule, "Asset Names"))
                {
                    settings.twoLineNamesEnabled = EditorGUILayout.Toggle(
                        new GUIContent("Two-Line Names", "Wrap names to two lines instead of ellipsising them. Icon view only — List rows are one line tall and cannot be made taller without stretching the folder tree."),
                        settings.twoLineNamesEnabled);
                    settings.showFileExtensionsEnabled = EditorGUILayout.Toggle(
                        new GUIContent("Show File Extensions", "Append the file extension to asset names. Applies to both views."),
                        settings.showFileExtensionsEnabled);
                }

                if (Section(ProjectModule, "Folder Contents"))
                {
                    settings.folderContentIconsEnabled = EditorGUILayout.Toggle(
                        new GUIContent("Type Icons", "Show which asset types a folder holds, most common first."),
                        settings.folderContentIconsEnabled);

                    using (new EditorGUI.DisabledScope(!settings.folderContentIconsEnabled))
                    {
                        settings.folderContentMaxIcons = EditorGUILayout.IntSlider("Max Icons", settings.folderContentMaxIcons, 1, 10);
                        settings.folderContentIconSize = EditorGUILayout.Slider("Icon Size", settings.folderContentIconSize, 8f, 20f);
                        settings.folderContentRecursive = EditorGUILayout.Toggle(
                            new GUIContent("Include Subfolders", "Off: only assets directly in the folder. On: everything beneath it."),
                            settings.folderContentRecursive);
                        settings.folderContentIconsInObjectView = EditorGUILayout.Toggle(
                            new GUIContent("Show In Object View", "Also draw the strip on folders in the right-hand pane, not just the folder tree."),
                            settings.folderContentIconsInObjectView);
                    }
                }

                if (Section(ProjectModule, "New Folder Button"))
                {
                    settings.createFolderButtonEnabled = EditorGUILayout.Toggle(
                        new GUIContent("Enabled", "A + button on the right of the Project window's breadcrumb header, creating a folder in the folder being browsed. Right-click it for other asset types."),
                        settings.createFolderButtonEnabled);
                }

                if (Section(ProjectModule, "Linked Assets"))
                {
                    settings.linkedAssetsEnabled = EditorGUILayout.Toggle(
                        new GUIContent("Enabled", "Badge folders that are meant to be symlinks with whether they still are."),
                        settings.linkedAssetsEnabled);

                    // The window itself stays reachable from Tools/DNExtensions while the feature is
                    // off — this is a shortcut to it, not the only way in.
                    using (new EditorGUI.DisabledScope(!settings.linkedAssetsEnabled))
                    {
                        EditorGUILayout.LabelField($"{settings.linkedAssetFolders.Count} folders tracked. Manage them in the window.",
                            EditorStyles.miniLabel);

                        if (IndentedButton("Open Linked Assets", 140f))
                        {
                            Project.LinkedAssetsWindow.ShowWindow();
                        }
                    }
                }

                if (Section(ProjectModule, "Drag Conflicts"))
                {
                    settings.dragConflictResolutionEnabled = EditorGUILayout.Toggle("Enabled", settings.dragConflictResolutionEnabled);

                    using (new EditorGUI.DisabledScope(!settings.dragConflictResolutionEnabled))
                    {
                        settings.conflictDefaultChoice = (ConflictDefaultChoice)EditorGUILayout.EnumPopup("Default Choice", settings.conflictDefaultChoice);

                        // Escape only has a slot to land in while the dialog is the thing deciding.
                        using (new EditorGUI.DisabledScope(settings.conflictDefaultChoice != ConflictDefaultChoice.AlwaysAsk))
                        {
                            settings.cancelIsDefaultOnEscape = EditorGUILayout.Toggle("Escape Cancels", settings.cancelIsDefaultOnEscape);
                        }
                    }
                    IndentedHelpBox("Replacing an asset overwrites the file in place and cannot be undone. Version control is the only recovery path.",
                        MessageType.Warning);
                }

                if (Section(ProjectModule, "Tabs"))
                {
                    settings.tabsEnabled = EditorGUILayout.Toggle(
                        new GUIContent("Enabled", "Opening what is under the cursor as its own tab: a folder as a folder tab, anything else as its Properties window."),
                        settings.tabsEnabled);

                    using (new EditorGUI.DisabledScope(!settings.tabsEnabled))
                    {
                        settings.dropOnTabsEnabled = EditorGUILayout.Toggle(
                            new GUIContent("Drop On Tabs", "Drag onto any window's tab strip to open it there, as well as using the keybind. Works for folders, assets and scene objects alike."),
                            settings.dropOnTabsEnabled);

                        settings.openInNewTabKey = DrawKeyBind("Open In New Tab", settings.openInNewTabKey);
                    }

                    EditorGUILayout.Space(4);
                    IndentedHelpBox(
                        "Open In New Tab acts on whatever is under the cursor: a folder opens as a folder tab, anything else opens its Properties window.\n" +
                        "A folder tab is the object view on its own — the same grid as the Project window's right-hand pane, with a breadcrumb instead of a folder tree. Double-click a subfolder to go into it, click a breadcrumb segment to come back out.\n" +
                        "The new tab docks beside the window it was opened from, and only floats when that window is floating itself. Dropping onto a tab strip docks into the strip it was dropped on.\n" +
                        "To open something floating instead, use Quick Object Window under Row Actions.\n" +
                        "Scene objects work as well as assets — they have no folder to show, so they always take the Properties route.",
                        MessageType.Info);
                }

                EndSections();
            }

            if (EditorGUI.EndChangeCheck())
            {
                HelpfulEditorSettings.SaveProject();

                // Toggling this should show in the editor immediately, not on the next poll.
                Project.ProjectCreateFolderButton.RequestRefresh();
            }

            DrawResetButton("Project", HelpfulEditorSettings.ResetProject);
        }

        /// <summary>
        /// A collapsible feature section. Open state lives in EditorPrefs rather than a static, so it
        /// survives domain reloads — a section that reopened itself on every recompile would be worse
        /// than the plain headers this replaced. Keyed by module because several titles repeat across
        /// the three pages.
        /// </summary>
        private static bool Section(string module, string title)
        {
            // Back to the header's own level first: sections are drawn one after another, so this
            // call is also what closes the indent the previous one opened.
            EditorGUI.indentLevel = _sectionIndent;
            EditorGUILayout.Space(4);

            string key = $"{FoldoutPrefix}{module}.{title}";
            bool expanded = EditorPrefs.GetBool(key, false);

            bool current = EditorGUILayout.Foldout(expanded, title, true, EditorStyles.foldoutHeader);
            if (current != expanded) EditorPrefs.SetBool(key, current);

            if (current) EditorGUI.indentLevel = _sectionIndent + 1;

            return current;
        }

        /// <summary>
        /// Remembers the level sections sit at, so their contents can be indented one step without
        /// every call site having to balance an increment against an early return.
        /// </summary>
        private static void BeginSections()
        {
            _sectionIndent = EditorGUI.indentLevel;
        }

        private static void EndSections()
        {
            EditorGUI.indentLevel = _sectionIndent;
        }

        /// <summary>
        /// GUILayout buttons take no notice of EditorGUI.indentLevel, so a button inside a section
        /// would sit flush against the window edge while every field above it is indented. The indent
        /// is applied by hand here so the two line up.
        /// </summary>
        private static bool IndentedButton(string label, float width)
        {
            EditorGUILayout.BeginHorizontal();

            GUILayout.Space(EditorGUI.indentLevel * IndentStep);
            bool pressed = GUILayout.Button(label, EditorStyles.miniButton, GUILayout.Width(width));
            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();

            return pressed;
        }

        /// <summary>
        /// Help boxes are laid out by GUILayout too, so they share the button's blind spot to
        /// EditorGUI.indentLevel and need the same treatment to line up with the section they belong
        /// to. Deliberately outside whatever DisabledScope the section's options sit in — the text
        /// explaining a feature is worth reading while deciding whether to switch it on.
        /// </summary>
        private static void IndentedHelpBox(string message, MessageType type)
        {
            EditorGUILayout.BeginHorizontal();

            GUILayout.Space(EditorGUI.indentLevel * IndentStep);
            EditorGUILayout.HelpBox(message, type);

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// The two things that are true of every keybind on the page. Drawn once under the module
        /// toggle rather than inside each section — the keybinds now live beside the features they
        /// belong to, and repeating this in every one of them would be most of what the page says.
        /// </summary>
        private static void DrawKeyBindNote()
        {
            EditorGUILayout.LabelField("Set a key to None to disable that action.", EditorStyles.miniLabel);

            EditorGUILayout.LabelField(
                $"Mouse0 / Mouse1 / Mouse2 bind to left, right and middle click. Ctrl means {HelpfulEditorPlatform.CommandModifierName} on this platform.",
                EditorStyles.miniLabel);
        }

        private static KeyBind DrawKeyBind(string label, KeyBind value)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.PrefixLabel(label);

            KeyCode key = (KeyCode)EditorGUILayout.EnumPopup(value.key);
            bool ctrl = GUILayout.Toggle(value.ctrl, "Ctrl", EditorStyles.miniButtonLeft, GUILayout.Width(40f));
            bool alt = GUILayout.Toggle(value.alt, "Alt", EditorStyles.miniButtonMid, GUILayout.Width(38f));
            bool shift = GUILayout.Toggle(value.shift, "Shift", EditorStyles.miniButtonRight, GUILayout.Width(44f));

            EditorGUILayout.EndHorizontal();

            return new KeyBind { key = key, ctrl = ctrl, alt = alt, shift = shift };
        }

        private static void DrawStringList(string label, List<string> values, string addLabel = "Add Type")
        {
            if (values == null) return;

            EditorGUILayout.LabelField(label);
            EditorGUI.indentLevel++;

            int removeAt = -1;
            for (int i = 0; i < values.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                values[i] = EditorGUILayout.TextField(values[i]);
                if (GUILayout.Button("−", EditorStyles.miniButton, GUILayout.Width(22f))) removeAt = i;
                EditorGUILayout.EndHorizontal();
            }

            if (removeAt >= 0) values.RemoveAt(removeAt);

            if (IndentedButton(addLabel, 80f)) values.Add(string.Empty);

            EditorGUI.indentLevel--;
        }

        private static void DrawDependencyWhitelist(List<ComponentDependencyPair> pairs)
        {
            if (pairs == null) return;

            EditorGUILayout.LabelField(new GUIContent("Dependency Whitelist",
                "Extra 'dependent follows dependency' pairs on top of RequireComponent and the built-in Unity pairs."));
            EditorGUI.indentLevel++;

            int removeAt = -1;
            for (int i = 0; i < pairs.Count; i++)
            {
                ComponentDependencyPair pair = pairs[i];
                if (pair == null) continue;

                EditorGUILayout.BeginHorizontal();
                pair.dependencyType = EditorGUILayout.TextField(pair.dependencyType);
                EditorGUILayout.LabelField("←", GUILayout.Width(16f));
                pair.dependentType = EditorGUILayout.TextField(pair.dependentType);
                if (GUILayout.Button("−", EditorStyles.miniButton, GUILayout.Width(22f))) removeAt = i;
                EditorGUILayout.EndHorizontal();
            }

            if (removeAt >= 0) pairs.RemoveAt(removeAt);

            if (IndentedButton("Add Pair", 80f)) pairs.Add(new ComponentDependencyPair());

            EditorGUI.indentLevel--;
        }

        private static void DrawResetButton(string moduleName, System.Action reset)
        {
            EditorGUILayout.Space(12);

            if (!GUILayout.Button($"Reset {moduleName} Settings", GUILayout.Height(24f))) return;

            if (EditorUtility.DisplayDialog("Reset Settings", $"Reset all {moduleName} settings to their defaults?", "Reset", "Cancel"))
            {
                reset();
            }
        }
    }
}
