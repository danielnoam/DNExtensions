#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.Utilities
{
    public static class EditorExtensions
    {
        #region File Dialogs

        /// <summary>
        /// Checks if a file exists and prompts the user for confirmation to overwrite it
        /// </summary>
        /// <param name="path">The file path to check</param>
        /// <returns>True if the file does not exist or user confirms overwrite, false otherwise</returns>
        public static bool ConfirmOverwrite(this string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            
            if (File.Exists(path))
            {
                return EditorUtility.DisplayDialog(
                    "File Exists",
                    $"The file already exists:\n{path}\n\nDo you want to overwrite it?",
                    "Overwrite",
                    "Cancel"
                );
            }
            return true;
        }

        /// <summary>
        /// Opens a folder browser dialog and returns the selected folder path
        /// </summary>
        /// <param name="defaultPath">The default path to open the folder browser at</param>
        /// <param name="title">The title of the dialog</param>
        /// <returns>The selected folder path, or empty string if cancelled</returns>
        public static string BrowseForFolder(this string defaultPath, string title = "Choose Folder")
        {
            return EditorUtility.SaveFolderPanel(title, defaultPath ?? "", "");
        }

        /// <summary>
        /// Opens a file browser dialog for opening a file
        /// </summary>
        /// <param name="defaultPath">The default path to start at</param>
        /// <param name="extension">The file extension filter (e.g., "txt")</param>
        /// <param name="title">The title of the dialog</param>
        /// <returns>The selected file path, or empty string if cancelled</returns>
        public static string BrowseForFile(this string defaultPath, string extension = "", string title = "Choose File")
        {
            return EditorUtility.OpenFilePanel(title, defaultPath ?? "", extension);
        }

        /// <summary>
        /// Opens a file browser dialog for saving a file
        /// </summary>
        /// <param name="defaultPath">The default path to start at</param>
        /// <param name="defaultName">The default file name</param>
        /// <param name="extension">The file extension (e.g., "txt")</param>
        /// <param name="title">The title of the dialog</param>
        /// <returns>The selected file path, or empty string if cancelled</returns>
        public static string BrowseForSaveFile(this string defaultPath, string defaultName = "newFile", string extension = "", string title = "Save File")
        {
            return EditorUtility.SaveFilePanel(title, defaultPath ?? "", defaultName, extension);
        }

        #endregion

        #region Asset Selection and Ping

        /// <summary>
        /// Pings and selects the specified asset in the Unity Editor
        /// </summary>
        public static void PingAndSelect(this Object asset)
        {
            if (!asset) return;
            
            EditorGUIUtility.PingObject(asset);
            Selection.activeObject = asset;
        }

        /// <summary>
        /// Pings the asset without selecting it
        /// </summary>
        public static void Ping(this Object asset)
        {
            if (!asset) return;
            EditorGUIUtility.PingObject(asset);
        }

        /// <summary>
        /// Selects the asset in the Project window
        /// </summary>
        public static void Select(this Object asset)
        {
            if (!asset) return;
            Selection.activeObject = asset;
        }

        /// <summary>
        /// Focuses the asset in the Project window (reveals it in the folder structure)
        /// </summary>
        public static void FocusInProjectWindow(this Object asset)
        {
            if (!asset) return;
            
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        #endregion

        #region Asset Paths

        /// <summary>
        /// Gets the asset path for a Unity Object (e.g., "Assets/MyFolder/MyAsset.asset")
        /// </summary>
        public static string GetAssetPath(this Object asset)
        {
            if (!asset) return string.Empty;
            return AssetDatabase.GetAssetPath(asset);
        }

        /// <summary>
        /// Gets the directory path of an asset
        /// </summary>
        public static string GetAssetDirectory(this Object asset)
        {
            if (!asset) return string.Empty;
            
            string path = AssetDatabase.GetAssetPath(asset);
            return Path.GetDirectoryName(path);
        }

        /// <summary>
        /// Gets the full system path of an asset
        /// </summary>
        public static string GetFullPath(this Object asset)
        {
            if (!asset) return string.Empty;
            
            string assetPath = AssetDatabase.GetAssetPath(asset);
            return Path.GetFullPath(assetPath);
        }

        #endregion

        #region Asset Operations

        /// <summary>
        /// Marks the asset as dirty (needs to be saved)
        /// </summary>
        public static void MarkDirty(this Object asset)
        {
            if (!asset) return;
            EditorUtility.SetDirty(asset);
        }

        /// <summary>
        /// Saves the asset to disk
        /// </summary>
        public static void SaveAsset(this Object asset)
        {
            if (!asset) return;
            
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Renames an asset
        /// </summary>
        public static bool RenameAsset(this Object asset, string newName)
        {
            if (!asset) return false;
            
            string assetPath = AssetDatabase.GetAssetPath(asset);
            string error = AssetDatabase.RenameAsset(assetPath, newName);
            
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError($"Failed to rename asset: {error}");
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// Deletes an asset
        /// </summary>
        public static bool DeleteAsset(this Object asset, bool askConfirmation = true)
        {
            if (!asset) return false;

            if (askConfirmation)
            {
                if (!EditorUtility.DisplayDialog(
                    "Delete Asset",
                    $"Are you sure you want to delete '{asset.name}'?",
                    "Delete",
                    "Cancel"))
                {
                    return false;
                }
            }

            string assetPath = AssetDatabase.GetAssetPath(asset);
            return AssetDatabase.DeleteAsset(assetPath);
        }

        #endregion

        #region Progress Bar

        /// <summary>
        /// Displays a progress bar
        /// </summary>
        public static void DisplayProgressBar(string title, string info, float progress)
        {
            EditorUtility.DisplayProgressBar(title, info, progress);
        }

        /// <summary>
        /// Clears the progress bar
        /// </summary>
        public static void ClearProgressBar()
        {
            EditorUtility.ClearProgressBar();
        }

        #endregion

        #region Dialogs

        /// <summary>
        /// Shows a simple dialog with OK button
        /// </summary>
        public static void ShowDialog(string title, string message)
        {
            EditorUtility.DisplayDialog(title, message, "OK");
        }

        /// <summary>
        /// Shows a confirmation dialog
        /// </summary>
        public static bool ShowConfirmDialog(string title, string message, string ok = "OK", string cancel = "Cancel")
        {
            return EditorUtility.DisplayDialog(title, message, ok, cancel);
        }

        /// <summary>
        /// Shows a three-option dialog
        /// </summary>
        public static int ShowDialogComplex(string title, string message, string ok, string cancel, string alt)
        {
            return EditorUtility.DisplayDialogComplex(title, message, ok, cancel, alt);
        }

        #endregion

        #region Clipboard

        /// <summary>
        /// Copies text to the system clipboard
        /// </summary>
        public static void CopyToClipboard(this string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            EditorGUIUtility.systemCopyBuffer = text;
        }

        /// <summary>
        /// Gets text from the system clipboard
        /// </summary>
        public static string GetFromClipboard()
        {
            return EditorGUIUtility.systemCopyBuffer;
        }

        #endregion
    }
}
#endif