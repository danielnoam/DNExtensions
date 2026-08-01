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
                Section("Zebra Stripes");
                settings.zebraStripesEnabled = EditorGUILayout.Toggle("Enabled", settings.zebraStripesEnabled);
                settings.zebraColorEven = EditorGUILayout.ColorField("Even Rows", settings.zebraColorEven);
                settings.zebraColorOdd = EditorGUILayout.ColorField("Odd Rows", settings.zebraColorOdd);
                settings.zebraOpacity = EditorGUILayout.Slider("Opacity", settings.zebraOpacity, 0f, 1f);

                Section("Tree Depth Lines");
                settings.treeDepthLinesEnabled = EditorGUILayout.Toggle("Enabled", settings.treeDepthLinesEnabled);
                settings.treeDepthLineColor = EditorGUILayout.ColorField("Colour", settings.treeDepthLineColor);
                settings.treeDepthLineStyle = (LineStyle)EditorGUILayout.EnumPopup("Style", settings.treeDepthLineStyle);
                settings.treeDepthLineThickness = EditorGUILayout.IntSlider("Thickness", Mathf.RoundToInt(settings.treeDepthLineThickness), 1, 3);

                Section("Component Strip");
                settings.componentStripEnabled = EditorGUILayout.Toggle("Enabled", settings.componentStripEnabled);
                settings.componentStripMaxIcons = EditorGUILayout.IntSlider("Max Icons", settings.componentStripMaxIcons, 1, 20);
                settings.componentIconSize = EditorGUILayout.Slider("Icon Size", settings.componentIconSize, 8f, 24f);
                settings.componentQuickEditEnabled = EditorGUILayout.Toggle(
                    new GUIContent("Alt+Click Quick Edit", "Alt+Click a component icon to open it in a floating mini inspector."),
                    settings.componentQuickEditEnabled);
                DrawStringList("Excluded Types", settings.excludedComponentTypes);

                Section("Child Count");
                settings.childCountEnabled = EditorGUILayout.Toggle("Enabled", settings.childCountEnabled);
                settings.childCountPosition = (BadgePosition)EditorGUILayout.EnumPopup("Position", settings.childCountPosition);
                settings.childCountHideWhenOneOrZero = EditorGUILayout.Toggle("Hide When ≤ 1", settings.childCountHideWhenOneOrZero);

                Section("Keybinds");
                EditorGUILayout.LabelField("Set a key to None to disable that action.", EditorStyles.miniLabel);
                settings.toggleActiveKey = DrawKeyBind("Toggle Active", settings.toggleActiveKey);
                settings.expandCollapseKey = DrawKeyBind("Expand / Collapse", settings.expandCollapseKey);
                settings.expandCollapseRecursiveKey = DrawKeyBind("Expand / Collapse All", settings.expandCollapseRecursiveKey);
                settings.isolateKey = DrawKeyBind("Isolate", settings.isolateKey);
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
                Section("Object Header Bar");
                settings.headerBarEnabled = EditorGUILayout.Toggle("Enabled", settings.headerBarEnabled);
                settings.headerBarButtonHeight = EditorGUILayout.Slider("Button Height", settings.headerBarButtonHeight, 16f, 32f);
                settings.fieldSearchEnabled = EditorGUILayout.Toggle(
                    new GUIContent("Field Search Bar", "Search field that filters properties across every component on the object."),
                    settings.fieldSearchEnabled);
                settings.isolationPersistsAcrossSelection = EditorGUILayout.Toggle("Isolation Persists", settings.isolationPersistsAcrossSelection);
                DrawStringList("Excluded Types", settings.excludedComponentTypes);

                Section("Transform Inspectors");
                settings.betterTransformEnabled = EditorGUILayout.Toggle("Better Transform", settings.betterTransformEnabled);
                settings.betterRectTransformEnabled = EditorGUILayout.Toggle("Better RectTransform", settings.betterRectTransformEnabled);
                settings.scaleLockDefaultOn = EditorGUILayout.Toggle("Scale Lock Default On", settings.scaleLockDefaultOn);
                settings.resetMenuItemsEnabled = EditorGUILayout.Toggle("Show Reset Menu Items", settings.resetMenuItemsEnabled);

                Section("Component Dragger");
                settings.componentDraggerEnabled = EditorGUILayout.Toggle("Enabled", settings.componentDraggerEnabled);
                settings.altInvertsMoveCopyDefault = EditorGUILayout.Toggle(
                    new GUIContent("Alt Inverts Move/Copy", "Off: drag moves, Alt copies. On: drag copies, Alt moves."),
                    settings.altInvertsMoveCopyDefault);
                settings.transferDependencies = EditorGUILayout.Toggle("Transfer Dependencies", settings.transferDependencies);
                DrawDependencyWhitelist(settings.dependencyWhitelist);

                Section("Keybinds");
                EditorGUILayout.LabelField("Component actions apply to the header bar button under the cursor.", EditorStyles.miniLabel);
                settings.isolateKey = DrawKeyBind("Isolate", settings.isolateKey);
                settings.expandCollapseKey = DrawKeyBind("Expand / Collapse", settings.expandCollapseKey);
                settings.toggleEnabledKey = DrawKeyBind("Toggle Enabled", settings.toggleEnabledKey);
                settings.focusSearchKey = DrawKeyBind("Focus Search", settings.focusSearchKey);
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
                Section("Hover Highlight");
                settings.hoverHighlightEnabled = EditorGUILayout.Toggle(
                    new GUIContent("Enabled", "The Project window has no hover tint of its own, unlike the Hierarchy."),
                    settings.hoverHighlightEnabled);
                settings.hoverColor = EditorGUILayout.ColorField("Colour", settings.hoverColor);
                settings.hoverOpacity = EditorGUILayout.Slider("Opacity", settings.hoverOpacity, 0f, 1f);

                Section("Zebra Stripes");
                settings.zebraStripesEnabled = EditorGUILayout.Toggle("Enabled", settings.zebraStripesEnabled);
                settings.zebraColorEven = EditorGUILayout.ColorField("Even Rows", settings.zebraColorEven);
                settings.zebraColorOdd = EditorGUILayout.ColorField("Odd Rows", settings.zebraColorOdd);
                settings.zebraOpacity = EditorGUILayout.Slider("Opacity", settings.zebraOpacity, 0f, 1f);

                Section("Tree Lines");
                settings.treeLinesEnabled = EditorGUILayout.Toggle("Enabled", settings.treeLinesEnabled);
                settings.treeLineColor = EditorGUILayout.ColorField("Colour", settings.treeLineColor);
                settings.treeLineStyle = (LineStyle)EditorGUILayout.EnumPopup("Style", settings.treeLineStyle);
                settings.treeLineThickness = EditorGUILayout.IntSlider("Thickness", Mathf.RoundToInt(settings.treeLineThickness), 1, 3);

                Section("Asset Names");
                settings.twoLineNamesEnabled = EditorGUILayout.Toggle(
                    new GUIContent("Two-Line Names", "Wrap names to two lines instead of ellipsising them. Icon view only — List rows are one line tall and cannot be made taller without stretching the folder tree."),
                    settings.twoLineNamesEnabled);
                settings.showFileExtensions = EditorGUILayout.Toggle(
                    new GUIContent("Show File Extensions", "Append the file extension to asset names. Applies to both views."),
                    settings.showFileExtensions);

                Section("New Folder Button");
                settings.createFolderButtonEnabled = EditorGUILayout.Toggle(
                    new GUIContent("Enabled", "A + button on the right of the Project window's breadcrumb header, creating a folder in the folder being browsed. Two-column layout only."),
                    settings.createFolderButtonEnabled);

                Section("Drag Conflicts");
                settings.dragConflictResolutionEnabled = EditorGUILayout.Toggle("Enabled", settings.dragConflictResolutionEnabled);
                settings.conflictDefaultChoice = (ConflictDefaultChoice)EditorGUILayout.EnumPopup("Default Choice", settings.conflictDefaultChoice);
                settings.cancelIsDefaultOnEscape = EditorGUILayout.Toggle("Escape Cancels", settings.cancelIsDefaultOnEscape);
                EditorGUILayout.HelpBox("Replacing an asset overwrites the file in place and cannot be undone. Version control is the only recovery path.",
                    MessageType.Warning);

                Section("Keybinds");
                EditorGUILayout.LabelField("Set a key to None to disable that action.", EditorStyles.miniLabel);
                settings.expandCollapseKey = DrawKeyBind("Expand / Collapse", settings.expandCollapseKey);
                settings.expandCollapseRecursiveKey = DrawKeyBind("Expand / Collapse All", settings.expandCollapseRecursiveKey);
                settings.collapseAllKey = DrawKeyBind("Collapse Everything", settings.collapseAllKey);
                settings.revealInFinderKey = DrawKeyBind($"Reveal In {HelpfulEditorPlatform.FileManagerName}", settings.revealInFinderKey);
                settings.quickObjectWindowKey = DrawKeyBind("Quick Object Window", settings.quickObjectWindowKey);
                settings.navigateBackKey = DrawKeyBind("Navigate Back", settings.navigateBackKey);
                settings.navigateForwardKey = DrawKeyBind("Navigate Forward", settings.navigateForwardKey);
                settings.closeWindowKey = DrawKeyBind("Close Focused Window", settings.closeWindowKey);
                DrawStringList("Never Close These Windows", settings.closeWindowExcludedTypes);
                EditorGUILayout.LabelField(
                    $"Mouse0 / Mouse1 / Mouse2 bind to left, right and middle click. Ctrl means {HelpfulEditorPlatform.CommandModifierName} on this platform.",
                    EditorStyles.miniLabel);
            }

            if (EditorGUI.EndChangeCheck()) HelpfulEditorSettings.SaveProject();

            DrawResetButton("Project", HelpfulEditorSettings.ResetProject);
        }

        private static void Section(string title)
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
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

        private static void DrawStringList(string label, List<string> values)
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

            if (GUILayout.Button("Add Type", EditorStyles.miniButton, GUILayout.Width(80f))) values.Add(string.Empty);

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
