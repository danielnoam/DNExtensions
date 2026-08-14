using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.HelpfulEditor.Project
{
    /// <summary>
    /// Replaces Unity's silent auto-rename on a same-name drop with an explicit Replace / Cancel /
    /// Keep Both choice. The conflict is detected here, on DragPerform over the folder row, because
    /// AssetModificationProcessor.OnWillMoveAsset only ever sees the already-deduplicated path.
    ///
    /// Both kinds of drop are covered. An in-project drag carries objectReferences and project
    /// relative paths and moves its sources; a drag in from Explorer or Finder carries neither —
    /// nothing in it is an asset yet — only absolute file paths, and copies them in, leaving the
    /// originals where they are. Everything before the file operation itself is shared.
    ///
    /// None of this is undoable: asset file operations are outside Unity's undo stack, so the
    /// confirmation dialog — which shows the full destination path — is the only safety net.
    /// </summary>
    [InitializeOnLoad]
    internal static class DragConflictResolver
    {
        /// <summary>
        /// The drop handler API gained a V2 form when object ids became EntityId in 6.4, and the
        /// original became a compile error in 6.5. Only the id type differs, and this handler has no
        /// use for it either way.
        /// </summary>
        static DragConflictResolver()
        {
#if UNITY_6000_3_OR_NEWER
            DragAndDrop.RemoveDropHandlerV2((DragAndDrop.ProjectBrowserDropHandlerV2)OnProjectDrop);
            DragAndDrop.AddDropHandlerV2((DragAndDrop.ProjectBrowserDropHandlerV2)OnProjectDrop);
#else
            DragAndDrop.RemoveDropHandler(OnProjectDrop);
            DragAndDrop.AddDropHandler(OnProjectDrop);
#endif
        }

        /// <summary>
        /// Unity's own drop handler hook, rather than reading drag events out of the row callback.
        /// The ProjectBrowser resolves drags in its tree view's drag controller, which claims the
        /// event before any per-row GUI runs — so the row callback only ever saw drags that Unity had
        /// already decided about. Returning anything but None here takes the drop.
        /// </summary>
#if UNITY_6000_3_OR_NEWER
        private static DragAndDropVisualMode OnProjectDrop(EntityId dragInstanceId, string dropUponPath, bool perform)
#else
        private static DragAndDropVisualMode OnProjectDrop(int dragInstanceId, string dropUponPath, bool perform)
#endif
        {
            ProjectSettings settings = HelpfulEditorSettings.Project;
            if (!settings.moduleEnabled || !settings.dragConflictResolutionEnabled) return DragAndDropVisualMode.None;

            string folder = ResolveFolder(dropUponPath);
            if (folder == null) return DragAndDropVisualMode.None;

            bool external = IsExternalDrag();
            List<string> sources = external ? CollectExternalFiles() : CollectDraggedAssetPaths(folder);
            if (sources.Count == 0 || !AnyConflicts(sources, folder)) return DragAndDropVisualMode.None;

            if (perform)
            {
                DragAndDrop.AcceptDrag();
                ResolveAll(sources, folder, external);
            }

            // Files dragged in from outside are copied, not moved, and the cursor should say so.
            return external ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Move;
        }

        /// <summary>
        /// The drop target as a folder. Dropping onto an asset row targets the folder holding it,
        /// which is what the Project window itself does. Dropping into the empty space below the
        /// icons arrives here as the browsed folder, so that gesture needs no special case.
        /// </summary>
        private static string ResolveFolder(string dropUponPath)
        {
            if (string.IsNullOrEmpty(dropUponPath)) return null;
            if (AssetDatabase.IsValidFolder(dropUponPath)) return dropUponPath;

            string directory = Path.GetDirectoryName(dropUponPath)?.Replace('\\', '/');
            return AssetDatabase.IsValidFolder(directory) ? directory : null;
        }

        /// <summary>
        /// A drag from outside the editor carries file paths but no objectReferences, because none of
        /// what it holds is an asset yet. A drag of scene objects has the opposite shape and falls
        /// through to the in-project path, which finds no eligible paths in it and declines the drop.
        /// </summary>
        private static bool IsExternalDrag()
        {
            if (DragAndDrop.objectReferences is { Length: > 0 }) return false;

            return DragAndDrop.paths is { Length: > 0 };
        }

        /// <summary>
        /// In-project asset drags. Anything already sitting in the destination folder is left alone,
        /// since dropping a file onto its own folder is not a conflict.
        /// </summary>
        private static List<string> CollectDraggedAssetPaths(string destinationFolder)
        {
            List<string> result = new List<string>();

            string[] paths = DragAndDrop.paths;
            if (paths == null) return result;

            foreach (string path in paths)
            {
                if (string.IsNullOrEmpty(path)) continue;
                if (AssetDatabase.IsValidFolder(path)) continue;
                if (Path.GetDirectoryName(path)?.Replace('\\', '/') == destinationFolder) continue;

                result.Add(path);
            }

            return result;
        }

        /// <summary>
        /// Files dragged in from the OS, as absolute paths. A dragged directory would have to be
        /// merged entry by entry with a decision per file inside it, so a drag holding one is handed
        /// back to Unity whole — taking only the loose files out of it would import half the drag and
        /// silently drop the rest.
        /// </summary>
        private static List<string> CollectExternalFiles()
        {
            List<string> result = new List<string>();

            foreach (string path in DragAndDrop.paths)
            {
                if (string.IsNullOrEmpty(path)) continue;
                if (Directory.Exists(path)) return new List<string>();
                if (!File.Exists(path)) continue;

                result.Add(path);
            }

            return result;
        }

        private static bool AnyConflicts(List<string> sources, string destinationFolder)
        {
            foreach (string source in sources)
            {
                if (Exists(DestinationFor(source, destinationFolder))) return true;
            }

            return false;
        }

        private enum ConflictAction
        {
            Take,
            Replace
        }

        /// <summary>
        /// Every decision is collected before a single file is touched. Prompting inside
        /// StartAssetEditing would run a modal dialog while the asset database is held in a batched
        /// state, and it also meant cancelling half way left the earlier moves already applied —
        /// now Cancel aborts the whole drag with nothing moved.
        ///
        /// Keep Both is planned as a Take onto a free name rather than as an action of its own, which
        /// also keeps GenerateUniqueAssetPath outside the batch, where the database can still answer
        /// what exists.
        /// </summary>
        private static void ResolveAll(List<string> sources, string destinationFolder, bool external)
        {
            ProjectSettings settings = HelpfulEditorSettings.Project;
            List<(string source, string destination, ConflictAction action)> plan =
                new List<(string, string, ConflictAction)>();

            foreach (string source in sources)
            {
                string destination = DestinationFor(source, destinationFolder);

                if (!Exists(destination))
                {
                    plan.Add((source, destination, ConflictAction.Take));
                    continue;
                }

                int choice = settings.conflictDefaultChoice switch
                {
                    ConflictDefaultChoice.Replace => 0,
                    ConflictDefaultChoice.KeepBoth => 2,
                    _ => AskUser(source, destination, external, settings.cancelIsDefaultOnEscape)
                };

                switch (choice)
                {
                    case 0:
                        plan.Add((source, destination, ConflictAction.Replace));
                        break;

                    case 2:
                        plan.Add((source, AssetDatabase.GenerateUniqueAssetPath(destination), ConflictAction.Take));
                        break;

                    default:
                        return;
                }
            }

            try
            {
                AssetDatabase.StartAssetEditing();

                foreach ((string source, string destination, ConflictAction action) in plan)
                {
                    // Copying in from outside is the same operation either way: Take lands on a name
                    // nothing holds, Replace lands on one something does.
                    if (external) CopyIn(source, destination);
                    else if (action == ConflictAction.Replace) Replace(source, destination);
                    else AssetDatabase.MoveAsset(source, destination);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }
        }

        /// <summary>
        /// Returns 0 for Replace, 1 for Cancel, 2 for Keep Both. Escape maps to the dialog's cancel
        /// slot, so which action sits there is driven by the setting.
        /// </summary>
        private static int AskUser(string source, string destination, bool external, bool cancelIsDefaultOnEscape)
        {
            string message =
                $"'{Path.GetFileName(source)}' already exists at:\n\n{destination}\n\n" +
                "Replace overwrites that file's contents and keeps its GUID, so existing references " +
                "point at the new content. This cannot be undone." +
                (external ? "\n\nThe file you dragged in stays where it is." : string.Empty);

            int result = cancelIsDefaultOnEscape
                ? EditorUtility.DisplayDialogComplex("Asset Already Exists", message, "Replace", "Cancel", "Keep Both")
                : EditorUtility.DisplayDialogComplex("Asset Already Exists", message, "Replace", "Keep Both", "Cancel");

            if (cancelIsDefaultOnEscape) return result;

            return result switch
            {
                1 => 2,
                2 => 1,
                _ => 0
            };
        }

        /// <summary>
        /// Overwrites the destination's bytes but leaves its .meta — and therefore its GUID and its
        /// import settings — untouched, so every existing reference resolves to the new content.
        /// </summary>
        private static void Replace(string source, string destination)
        {
            string sourceFull = FullPath(source);
            string destinationFull = FullPath(destination);
            if (sourceFull == null || destinationFull == null) return;

            try
            {
                File.Copy(sourceFull, destinationFull, true);
            }
            catch (Exception e)
            {
                Debug.LogError($"[HelpfulEditor] Could not replace '{destination}'. ({e.Message})");
                return;
            }

            AssetDatabase.DeleteAsset(source);
            AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceUpdate);
        }

        /// <summary>
        /// Brings a file in from outside the project. The source is left alone — it is not the
        /// editor's to move — and an existing destination keeps its .meta for the same reason
        /// Replace does.
        /// </summary>
        private static void CopyIn(string sourceFullPath, string destination)
        {
            string destinationFull = FullPath(destination);
            if (destinationFull == null) return;

            try
            {
                File.Copy(sourceFullPath, destinationFull, true);
            }
            catch (Exception e)
            {
                Debug.LogError($"[HelpfulEditor] Could not copy '{sourceFullPath}' into '{destination}'. ({e.Message})");
                return;
            }

            AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceUpdate);
        }

        private static string DestinationFor(string sourcePath, string destinationFolder)
        {
            return $"{destinationFolder}/{Path.GetFileName(sourcePath)}";
        }

        /// <summary>
        /// Asset paths are relative to the project root, which is the editor's working directory
        /// almost always but not by contract — a native file dialog can leave it somewhere else.
        /// Every file test and file operation here goes through an absolute path for that reason.
        /// </summary>
        private static bool Exists(string projectRelativePath)
        {
            string full = FullPath(projectRelativePath);

            return full != null && File.Exists(full);
        }

        private static string FullPath(string projectRelativePath)
        {
            string root = Directory.GetParent(Application.dataPath)?.FullName;

            return string.IsNullOrEmpty(root) ? null : Path.Combine(root, projectRelativePath);
        }
    }
}
