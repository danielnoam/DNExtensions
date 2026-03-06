using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace DNExtensions.Utilities
{
    internal class AssetTreeView : TreeView<int>
    {
        internal class AssetItem : TreeViewItem<int>
        {
            internal string FullPath;
            internal bool IsFolder;
            internal bool Checked = true;
        }

        private readonly Dictionary<int, AssetItem> _items = new();
        private readonly string _rootPath;

        internal AssetTreeView(TreeViewState<int> state, string rootPath) : base(state)
        {
            _rootPath = rootPath;
        }

        protected override TreeViewItem<int> BuildRoot()
        {
            _items.Clear();
            var root = new AssetItem { id = 0, depth = -1, displayName = "root", IsFolder = true };
            int idCounter = 1;

            foreach (string dir in Directory.GetDirectories(_rootPath).OrderBy(d => d))
                root.AddChild(BuildTree(dir, ref idCounter, 0, Path.GetFileName(dir)));

            foreach (string file in Directory.GetFiles(_rootPath).Where(f => !f.EndsWith(".meta")).OrderBy(f => f))
            {
                var fileItem = new AssetItem
                {
                    id = idCounter++,
                    depth = 0,
                    displayName = Path.GetFileName(file),
                    FullPath = file,
                    IsFolder = false,
                    Checked = true
                };
                _items[fileItem.id] = fileItem;
                root.AddChild(fileItem);
            }

            SetupDepthsFromParentsAndChildren(root);
            return root;
        }

        private AssetItem BuildTree(string path, ref int idCounter, int depth, string displayName)
        {
            var item = new AssetItem
            {
                id = idCounter++,
                depth = depth,
                displayName = displayName,
                FullPath = path,
                IsFolder = true,
                Checked = true
            };
            _items[item.id] = item;

            foreach (string dir in Directory.GetDirectories(path).OrderBy(d => d))
                item.AddChild(BuildTree(dir, ref idCounter, depth + 1, Path.GetFileName(dir)));

            foreach (string file in Directory.GetFiles(path).Where(f => !f.EndsWith(".meta")).OrderBy(f => f))
            {
                var fileItem = new AssetItem
                {
                    id = idCounter++,
                    depth = depth + 1,
                    displayName = Path.GetFileName(file),
                    FullPath = file,
                    IsFolder = false,
                    Checked = true
                };
                _items[fileItem.id] = fileItem;
                item.AddChild(fileItem);
            }

            return item;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            if (!_items.TryGetValue(args.item.id, out var item)) return;

            float indent = GetContentIndent(args.item);
            var checkRect = new Rect(args.rowRect.x + indent, args.rowRect.y + 1, 16, args.rowRect.height - 2);
            var labelRect = new Rect(checkRect.xMax + 2, args.rowRect.y, args.rowRect.xMax - checkRect.xMax - 2, args.rowRect.height);

            EditorGUI.BeginChangeCheck();
            bool newVal = EditorGUI.Toggle(checkRect, item.Checked);
            if (EditorGUI.EndChangeCheck())
                SetCheckedRecursive(item, newVal);

            Texture icon;
            if (item.IsFolder)
            {
                icon = EditorGUIUtility.IconContent("Folder Icon").image;
            }
            else
            {
                string assetPath = ToAssetPath(item.FullPath);
                icon = string.IsNullOrEmpty(assetPath) ? null : AssetDatabase.GetCachedIcon(assetPath) as Texture;
            }

            if (icon != null)
            {
                GUI.DrawTexture(new Rect(labelRect.x, labelRect.y + 1, 16, 16), icon, ScaleMode.ScaleToFit);
                labelRect.x += 18;
                labelRect.width -= 18;
            }

            using (new EditorGUI.DisabledScope(!item.Checked))
                EditorGUI.LabelField(labelRect, item.displayName);
        }

        private void SetCheckedRecursive(AssetItem item, bool value)
        {
            item.Checked = value;
            if (item.hasChildren)
                foreach (var child in item.children.OfType<AssetItem>())
                    SetCheckedRecursive(child, value);
            Repaint();
        }

        internal void SetAllChecked(bool value)
        {
            foreach (var item in _items.Values)
                item.Checked = value;
            Repaint();
        }

        internal List<string> GetCheckedFilePaths() =>
            _items.Values.Where(i => !i.IsFolder && i.Checked).Select(i => i.FullPath).ToList();

        // Only works for files under the actual project Assets folder
        private string ToAssetPath(string fullPath)
        {
            string dataPath = Application.dataPath;
            return fullPath.StartsWith(dataPath)
                ? "Assets" + fullPath.Substring(dataPath.Length).Replace('\\', '/')
                : null;
        }

        protected override bool CanMultiSelect(TreeViewItem<int> item) => false;
    }
}