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

        private const string Hierarchy = "Hierarchy";
        private const string Inspector = "Inspector";
        private const string ProjectModule = "Project";
        private const string GameViewModule = "GameView";

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
                keywords = new[] { "helpful editor", "hierarchy", "inspector", "project", "folder", "tabs" }
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
                new GUIContent("Module Enabled", "Adds rulers to every Game View. Drag off a ruler to place a guide."),
                settings.moduleEnabled, EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(!settings.moduleEnabled))
            {
                BeginSections();

                if (Section(GameViewModule, "Guides"))
                {
                    settings.showGuides = EditorGUILayout.Toggle("Show Guides", settings.showGuides);
                    settings.guideColor = EditorGUILayout.ColorField("Colour", settings.guideColor);
                    settings.guideWidth = EditorGUILayout.Slider("Width", settings.guideWidth, 0.5f, 8f);

                    EditorGUILayout.LabelField($"{settings.guides.Count} placed", EditorStyles.miniLabel);

                    using (new EditorGUI.DisabledScope(settings.guides.Count == 0))
                    {
                        if (GUILayout.Button("Clear All Guides", GUILayout.Width(140f))) GameView.GameViewModule.ClearGuides();
                    }

                    EditorGUILayout.Space(4);
                    EditorGUILayout.HelpBox(
                        "Drag off the top ruler for a vertical guide, off the left ruler for a horizontal one.\n" +
                        "Drag a guide back onto a ruler to delete it.\n" +
                        "Hold Alt while dragging to snap to the centre, Shift to move in 10px steps.\n" +
                        "Right-click a ruler for the guide menu.\n" +
                        "Positions are held against the render target, so they survive resizing and zooming.",
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

        private static void DrawRoot()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Helpful Editor", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Editor toolkit for the Hierarchy, Inspector and Project windows.", EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Settings are stored as JSON under ProjectSettings/HelpfulEditor/ and are tracked by source control.",
                EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(GlobalKeyCapture.Available
                    ? "Hover keybinds are active and fire regardless of which window has focus."
                    : "Hover keybinds could not hook Unity's global event handler — they will only fire while the target window has focus.",
                GlobalKeyCapture.Available ? MessageType.Info : MessageType.Warning);
        }

        private static void DrawHierarchy()
        {
            HierarchySettings settings = HelpfulEditorSettings.Hierarchy;

            EditorGUI.BeginChangeCheck();

            settings.moduleEnabled = EditorGUILayout.ToggleLeft("Module Enabled", settings.moduleEnabled, EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(!settings.moduleEnabled))
            {
                BeginSections();

                if (Section(Hierarchy, "Zebra Stripes"))
                {
                    settings.zebraStripesEnabled = EditorGUILayout.Toggle("Enabled", settings.zebraStripesEnabled);
                    settings.zebraColorEven = EditorGUILayout.ColorField("Even Rows", settings.zebraColorEven);
                    settings.zebraColorOdd = EditorGUILayout.ColorField("Odd Rows", settings.zebraColorOdd);
                    settings.zebraOpacity = EditorGUILayout.Slider("Opacity", settings.zebraOpacity, 0f, 1f);
                }

                if (Section(Hierarchy, "Tree Depth Lines"))
                {
                    settings.treeDepthLinesEnabled = EditorGUILayout.Toggle("Enabled", settings.treeDepthLinesEnabled);
                    settings.treeDepthLineColor = EditorGUILayout.ColorField("Colour", settings.treeDepthLineColor);
                    settings.treeDepthLineStyle = (LineStyle)EditorGUILayout.EnumPopup("Style", settings.treeDepthLineStyle);
                }

                if (Section(Hierarchy, "Component Strip"))
                {
                    settings.componentStripEnabled = EditorGUILayout.Toggle("Enabled", settings.componentStripEnabled);
                    settings.componentStripMaxIcons = EditorGUILayout.IntSlider("Max Icons", settings.componentStripMaxIcons, 1, 20);
                    settings.componentIconSize = EditorGUILayout.Slider("Icon Size", settings.componentIconSize, 8f, 24f);
                    settings.componentQuickEditEnabled = EditorGUILayout.Toggle(
                        new GUIContent("Alt+Click Quick Edit", "Alt+Click a component icon to open it in a floating mini inspector."),
                        settings.componentQuickEditEnabled);
                    DrawStringList("Excluded Types", settings.excludedComponentTypes);
                }

                if (Section(Hierarchy, "Child Count"))
                {
                    settings.childCountEnabled = EditorGUILayout.Toggle("Enabled", settings.childCountEnabled);
                    settings.childCountPosition = (BadgePosition)EditorGUILayout.EnumPopup("Position", settings.childCountPosition);
                    settings.childCountHideWhenOneOrZero = EditorGUILayout.Toggle("Hide When ≤ 1", settings.childCountHideWhenOneOrZero);
                }

                if (Section(Hierarchy, "Keybinds"))
                {
                    EditorGUILayout.LabelField("Set a key to None to disable that action.", EditorStyles.miniLabel);
                    settings.toggleActiveKey = DrawKeyBind("Toggle Active", settings.toggleActiveKey);
                    settings.expandCollapseKey = DrawKeyBind("Expand / Collapse", settings.expandCollapseKey);
                    settings.expandCollapseRecursiveKey = DrawKeyBind("Expand / Collapse All", settings.expandCollapseRecursiveKey);
                    settings.collapseAllKey = DrawKeyBind("Collapse Everything", settings.collapseAllKey);
                    settings.isolateKey = DrawKeyBind("Isolate", settings.isolateKey);
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

            using (new EditorGUI.DisabledScope(!settings.moduleEnabled))
            {
                BeginSections();

                if (Section(Inspector, "Object Header Bar"))
                {
                    settings.headerBarEnabled = EditorGUILayout.Toggle("Enabled", settings.headerBarEnabled);
                    settings.headerBarButtonHeight = EditorGUILayout.Slider("Button Height", settings.headerBarButtonHeight, 16f, 32f);
                    settings.fieldSearchEnabled = EditorGUILayout.Toggle(
                        new GUIContent("Field Search Bar", "Search field that filters properties across every component on the object."),
                        settings.fieldSearchEnabled);
                    settings.isolationPersistsAcrossSelection = EditorGUILayout.Toggle("Isolation Persists", settings.isolationPersistsAcrossSelection);
                    DrawStringList("Excluded Types", settings.excludedComponentTypes);
                }

                if (Section(Inspector, "Transform Inspector"))
                {
                    settings.betterTransformEnabled = EditorGUILayout.Toggle("Better Transform", settings.betterTransformEnabled);
                    settings.scaleLockDefaultOn = EditorGUILayout.Toggle("Scale Lock Default On", settings.scaleLockDefaultOn);
                    settings.resetMenuItemsEnabled = EditorGUILayout.Toggle("Show Reset Menu Items", settings.resetMenuItemsEnabled);
                }

                if (Section(Inspector, "Save In Play Mode"))
                {
                    settings.saveInPlayModeEnabled = EditorGUILayout.Toggle(
                        new GUIContent("Enabled", "Adds a save button to component headers during play mode. Marked components are restored after returning to edit mode."),
                        settings.saveInPlayModeEnabled);
                    DrawStringList("Blacklisted Types", settings.saveInPlayModeBlacklist);
                }

                if (Section(Inspector, "Component Header Buttons"))
                {
                    settings.cameraAlignButtonsEnabled = EditorGUILayout.Toggle(
                        new GUIContent("Camera Align", "Adds buttons to Camera headers that align the camera to the Scene View, or the Scene View to the camera."),
                        settings.cameraAlignButtonsEnabled);
                    settings.rectTransformResetButtonsEnabled = EditorGUILayout.Toggle(
                        new GUIContent("RectTransform Rows", "Adds buttons to RectTransform headers for position, size, rotation and scale. Left-click to copy or paste that value, right-click to reset it."),
                        settings.rectTransformResetButtonsEnabled);
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
                    settings.altInvertsMoveCopyDefault = EditorGUILayout.Toggle(
                        new GUIContent("Alt Inverts Move/Copy", "Off: drag moves, Alt copies. On: drag copies, Alt moves."),
                        settings.altInvertsMoveCopyDefault);
                    settings.transferDependencies = EditorGUILayout.Toggle("Transfer Dependencies", settings.transferDependencies);
                    DrawDependencyWhitelist(settings.dependencyWhitelist);
                }

                if (Section(Inspector, "Keybinds"))
                {
                    EditorGUILayout.LabelField("Component actions apply to the header bar button under the cursor.", EditorStyles.miniLabel);
                    settings.isolateKey = DrawKeyBind("Isolate", settings.isolateKey);
                    settings.expandCollapseKey = DrawKeyBind("Expand / Collapse", settings.expandCollapseKey);
                    settings.collapseAllKey = DrawKeyBind("Collapse Everything", settings.collapseAllKey);
                    settings.toggleEnabledKey = DrawKeyBind("Toggle Enabled", settings.toggleEnabledKey);
                    settings.focusSearchKey = DrawKeyBind("Focus Search", settings.focusSearchKey);
                }

                EndSections();
            }

            if (EditorGUI.EndChangeCheck()) HelpfulEditorSettings.SaveInspector();

            DrawResetButton("Inspector", HelpfulEditorSettings.ResetInspector);
        }

        private static void DrawProject()
        {
            ProjectModuleSettings settings = HelpfulEditorSettings.Project;

            EditorGUI.BeginChangeCheck();

            settings.moduleEnabled = EditorGUILayout.ToggleLeft("Module Enabled", settings.moduleEnabled, EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(!settings.moduleEnabled))
            {
                BeginSections();

                if (Section(ProjectModule, "Hover Highlight"))
                {
                    settings.hoverHighlightEnabled = EditorGUILayout.Toggle(
                        new GUIContent("Enabled", "The Project window has no hover tint of its own, unlike the Hierarchy."),
                        settings.hoverHighlightEnabled);
                    settings.hoverColor = EditorGUILayout.ColorField("Colour", settings.hoverColor);
                    settings.hoverOpacity = EditorGUILayout.Slider("Opacity", settings.hoverOpacity, 0f, 1f);
                }

                if (Section(ProjectModule, "Zebra Stripes"))
                {
                    settings.zebraStripesEnabled = EditorGUILayout.Toggle("Enabled", settings.zebraStripesEnabled);
                    settings.zebraColorEven = EditorGUILayout.ColorField("Even Rows", settings.zebraColorEven);
                    settings.zebraColorOdd = EditorGUILayout.ColorField("Odd Rows", settings.zebraColorOdd);
                    settings.zebraOpacity = EditorGUILayout.Slider("Opacity", settings.zebraOpacity, 0f, 1f);
                }

                if (Section(ProjectModule, "Tree Lines"))
                {
                    settings.treeLinesEnabled = EditorGUILayout.Toggle("Enabled", settings.treeLinesEnabled);
                    settings.treeLineColor = EditorGUILayout.ColorField("Colour", settings.treeLineColor);
                    settings.treeLineStyle = (LineStyle)EditorGUILayout.EnumPopup("Style", settings.treeLineStyle);
                }

                if (Section(ProjectModule, "Folding"))
                {
                    settings.animatedFoldsEnabled = EditorGUILayout.Toggle(
                        new GUIContent("Animate", "Play multi-row folds one at a time, and glide to where back/forward lands instead of jumping there."),
                        settings.animatedFoldsEnabled);
                }

                if (Section(ProjectModule, "Asset Names"))
                {
                    settings.twoLineNamesEnabled = EditorGUILayout.Toggle(
                        new GUIContent("Two-Line Names", "Wrap names to two lines instead of ellipsising them. Icon view only — List rows are one line tall and cannot be made taller without stretching the folder tree."),
                        settings.twoLineNamesEnabled);
                    settings.showFileExtensions = EditorGUILayout.Toggle(
                        new GUIContent("Show File Extensions", "Append the file extension to asset names. Applies to both views."),
                        settings.showFileExtensions);
                }

                if (Section(ProjectModule, "Folder Contents"))
                {
                    settings.folderContentIconsEnabled = EditorGUILayout.Toggle(
                        new GUIContent("Type Icons", "Show which asset types a folder holds, most common first."),
                        settings.folderContentIconsEnabled);
                    settings.folderContentMaxIcons = EditorGUILayout.IntSlider("Max Icons", settings.folderContentMaxIcons, 1, 10);
                    settings.folderContentIconSize = EditorGUILayout.Slider("Icon Size", settings.folderContentIconSize, 8f, 20f);
                    settings.folderContentRecursive = EditorGUILayout.Toggle(
                        new GUIContent("Include Subfolders", "Off: only assets directly in the folder. On: everything beneath it."),
                        settings.folderContentRecursive);
                    settings.folderContentIconsInObjectView = EditorGUILayout.Toggle(
                        new GUIContent("Show In Object View", "Also draw the strip on folders in the right-hand pane, not just the folder tree."),
                        settings.folderContentIconsInObjectView);
                }

                if (Section(ProjectModule, "Window Titles"))
                {
                    settings.windowTitlesEnabled = EditorGUILayout.Toggle(
                        new GUIContent("Enabled", "Name windows pinned to something after what they show: a locked Project window takes its folder's name, a floating Properties window takes its object's."),
                        settings.windowTitlesEnabled);

                    if (HelpfulEditorPlugins.VTabsActive)
                    {
                        EditorGUILayout.HelpBox("vTabs is installed and already renames these windows, so this is standing down. Disable vTabs to use it.",
                            MessageType.Info);
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

                    EditorGUILayout.LabelField($"{settings.linkedAssetFolders.Count} folders tracked. Manage them in the window.",
                        EditorStyles.miniLabel);

                    if (GUILayout.Button("Open Linked Assets", EditorStyles.miniButton, GUILayout.Width(140f)))
                    {
                        Project.LinkedAssetsWindow.ShowWindow();
                    }
                }

                if (Section(ProjectModule, "Drag Conflicts"))
                {
                    settings.dragConflictResolutionEnabled = EditorGUILayout.Toggle("Enabled", settings.dragConflictResolutionEnabled);
                    settings.conflictDefaultChoice = (ConflictDefaultChoice)EditorGUILayout.EnumPopup("Default Choice", settings.conflictDefaultChoice);
                    settings.cancelIsDefaultOnEscape = EditorGUILayout.Toggle("Escape Cancels", settings.cancelIsDefaultOnEscape);
                    EditorGUILayout.HelpBox("Replacing an asset overwrites the file in place and cannot be undone. Version control is the only recovery path.",
                        MessageType.Warning);
                }

                if (Section(ProjectModule, "Keybinds"))
                {
                    EditorGUILayout.LabelField("Set a key to None to disable that action.", EditorStyles.miniLabel);
                    settings.expandCollapseKey = DrawKeyBind("Expand / Collapse", settings.expandCollapseKey);
                    settings.expandCollapseRecursiveKey = DrawKeyBind("Expand / Collapse All", settings.expandCollapseRecursiveKey);
                    settings.collapseAllKey = DrawKeyBind("Collapse Everything", settings.collapseAllKey);
                    settings.isolateKey = DrawKeyBind("Isolate", settings.isolateKey);
                    settings.revealInFinderKey = DrawKeyBind($"Reveal In {HelpfulEditorPlatform.FileManagerName}", settings.revealInFinderKey);
                    settings.quickObjectWindowKey = DrawKeyBind("Quick Object Window", settings.quickObjectWindowKey);
                    settings.openFolderInNewTabKey = DrawKeyBind("Open Folder In New Window", settings.openFolderInNewTabKey);
                    settings.autoDock = EditorGUILayout.Toggle(
                        new GUIContent("Auto Dock", "On: the new window docks beside the one clicked. Off: it opens floating."),
                        settings.autoDock);
                    settings.lockFolderWindows = EditorGUILayout.Toggle(
                        new GUIContent("Lock To Folder", "Keep the new window on its folder instead of letting it follow the selection. Also what lets it be named after the folder."),
                        settings.lockFolderWindows);
                    settings.folderDropCreatesTabEnabled = EditorGUILayout.Toggle(
                        new GUIContent("Drop Folder On Tabs", "Drag a folder onto any window's tab strip to open it there as its own Project tab."),
                        settings.folderDropCreatesTabEnabled);
                    settings.navigateBackKey = DrawKeyBind("Navigate Back", settings.navigateBackKey);
                    settings.navigateForwardKey = DrawKeyBind("Navigate Forward", settings.navigateForwardKey);
                    settings.closeWindowKey = DrawKeyBind("Close Focused Window", settings.closeWindowKey);
                    settings.reopenWindowKey = DrawKeyBind("Reopen Closed Window", settings.reopenWindowKey);
                    DrawStringList("Never Close These Windows", settings.closeWindowExcludedTypes);
                    EditorGUILayout.LabelField("Closing is undoable, so this list is usually best left empty.", EditorStyles.miniLabel);

                    if (HelpfulEditorPlugins.VTabsActive)
                    {
                        EditorGUILayout.HelpBox("vTabs binds the same defaults for close and reopen, acting on the tab rather than the window. Both will fire — rebind one side.",
                            MessageType.Warning);
                    }
                    EditorGUILayout.LabelField(
                        $"Mouse0 / Mouse1 / Mouse2 bind to left, right and middle click. Ctrl means {HelpfulEditorPlatform.CommandModifierName} on this platform.",
                        EditorStyles.miniLabel);
                }

                EndSections();
            }

            if (EditorGUI.EndChangeCheck())
            {
                HelpfulEditorSettings.SaveProject();

                // Toggling either of these should show in the editor immediately, not on the next poll.
                HelpfulEditorWindowTitles.RequestRefresh();
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

            if (GUILayout.Button(addLabel, EditorStyles.miniButton, GUILayout.Width(80f))) values.Add(string.Empty);

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

            if (GUILayout.Button("Add Pair", EditorStyles.miniButton, GUILayout.Width(80f))) pairs.Add(new ComponentDependencyPair());

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
