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
#if UNITY_6000_4_OR_NEWER
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
#if UNITY_6000_4_OR_NEWER
        private static DragAndDropVisualMode OnProjectDrop(EntityId dragInstanceId, string dropUponPath, bool perform)
#else
        private static DragAndDropVisualMode OnProjectDrop(int dragInstanceId, string dropUponPath, bool perform)
#endif
        {
            ProjectModuleSettings settings = HelpfulEditorSettings.Project;
            if (!settings.moduleEnabled || !settings.dragConflictResolutionEnabled) return DragAndDropVisualMode.None;

            string folder = ResolveFolder(dropUponPath);
            if (folder == null) return DragAndDropVisualMode.None;

            List<string> sources = CollectDraggedAssetPaths(folder);
            if (sources.Count == 0 || !AnyConflicts(sources, folder)) return DragAndDropVisualMode.None;

            if (perform)
            {
                DragAndDrop.AcceptDrag();
                ResolveAll(sources, folder);
            }

            return DragAndDropVisualMode.Move;
        }

        /// <summary>
        /// The drop target as a folder. Dropping onto an asset row targets the folder holding it,
        /// which is what the Project window itself does.
        /// </summary>
        private static string ResolveFolder(string dropUponPath)
        {
            if (string.IsNullOrEmpty(dropUponPath)) return null;
            if (AssetDatabase.IsValidFolder(dropUponPath)) return dropUponPath;

            string directory = Path.GetDirectoryName(dropUponPath)?.Replace('\\', '/');
            return AssetDatabase.IsValidFolder(directory) ? directory : null;
        }

        /// <summary>
        /// In-project asset drags only. OS-level drags carry no objectReferences and go through
        /// Unity's import pipeline instead, which is out of scope for this feature.
        /// </summary>
        private static List<string> CollectDraggedAssetPaths(string destinationFolder)
        {
            List<string> result = new List<string>();

            if (DragAndDrop.objectReferences == null || DragAndDrop.objectReferences.Length == 0) return result;

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

        private static bool AnyConflicts(List<string> sources, string destinationFolder)
        {
            foreach (string source in sources)
            {
                if (File.Exists(DestinationFor(source, destinationFolder))) return true;
            }

            return false;
        }

        private enum ConflictAction
        {
            Move,
            Replace,
            KeepBoth
        }

        /// <summary>
        /// Every decision is collected before a single file is touched. Prompting inside
        /// StartAssetEditing would run a modal dialog while the asset database is held in a batched
        /// state, and it also meant cancelling half way left the earlier moves already applied —
        /// now Cancel aborts the whole drag with nothing moved.
        /// </summary>
        private static void ResolveAll(List<string> sources, string destinationFolder)
        {
            ProjectModuleSettings settings = HelpfulEditorSettings.Project;
            List<(string source, string destination, ConflictAction action)> plan =
                new List<(string, string, ConflictAction)>();

            foreach (string source in sources)
            {
                string destination = DestinationFor(source, destinationFolder);

                if (!File.Exists(destination))
                {
                    plan.Add((source, destination, ConflictAction.Move));
                    continue;
                }

                int choice = settings.conflictDefaultChoice switch
                {
                    ConflictDefaultChoice.Replace => 0,
                    ConflictDefaultChoice.KeepBoth => 2,
                    _ => AskUser(source, destination, settings.cancelIsDefaultOnEscape)
                };

                switch (choice)
                {
                    case 0:
                        plan.Add((source, destination, ConflictAction.Replace));
                        break;

                    case 2:
                        plan.Add((source, destination, ConflictAction.KeepBoth));
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
                    switch (action)
                    {
                        case ConflictAction.Replace:
                            Replace(source, destination);
                            break;

                        case ConflictAction.KeepBoth:
                            AssetDatabase.MoveAsset(source, AssetDatabase.GenerateUniqueAssetPath(destination));
                            break;

                        default:
                            AssetDatabase.MoveAsset(source, destination);
                            break;
                    }
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
        private static int AskUser(string source, string destination, bool cancelIsDefaultOnEscape)
        {
            string message =
                $"'{Path.GetFileName(source)}' already exists at:\n\n{destination}\n\n" +
                "Replace overwrites that file's contents and keeps its GUID, so existing references " +
                "point at the new content. This cannot be undone.";

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
        /// Overwrites the destination's bytes but leaves its .meta — and therefore its GUID —
        /// untouched, so every existing reference resolves to the new content.
        /// </summary>
        private static void Replace(string source, string destination)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot)) return;

            string sourceFull = Path.Combine(projectRoot, source);
            string destinationFull = Path.Combine(projectRoot, destination);

            File.Copy(sourceFull, destinationFull, true);

            AssetDatabase.DeleteAsset(source);
            AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceUpdate);
        }

        private static string DestinationFor(string sourcePath, string destinationFolder)
        {
            return $"{destinationFolder}/{Path.GetFileName(sourcePath)}";
        }
    }
}
