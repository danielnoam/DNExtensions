using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNExtensions.HelpfulEditor.Project
{
    /// <summary>
    /// Redraws Project window asset names: wrapped to two lines instead of ellipsised, and with the
    /// file extension shown.
    ///
    /// Wrapping is Icon view only. List rows are one line tall and the only internal that changes
    /// that — TreeViewGUI's k_LineHeight — belongs to the folder tree, so setting it stretches the
    /// left pane while leaving the asset list, the pane that actually ellipsises names, untouched.
    /// Extensions still work in both views, since those are drawn rather than re-laid-out.
    /// </summary>
    internal static class ProjectNameOverlay
    {
        private const float LabelInset = 12f;
        private const float ListLabelGap = 2f;

        private static readonly char[] Separators = { ' ', '_', '-', '.' };

        // Path splitting allocates, and this runs for every visible row on every repaint.
        private static readonly Dictionary<string, (string name, string extension)> NameCache =
            new Dictionary<string, (string, string)>();

        // Measuring text is the expensive half of laying out two lines, and a name's break point
        // only changes when the cell width does.
        private static readonly Dictionary<string, (string first, string second)> LineCache =
            new Dictionary<string, (string, string)>();

        private static readonly List<int> BoundaryBuffer = new List<int>();

        private static readonly GUIContent MeasureContent = new GUIContent();
        private static readonly GUIContent DrawContent = new GUIContent();

        private static GUIStyle _labelStyle;
        private static Color _backgroundColor;
        private static Color _selectedColor;
        private static float _lineCacheWidth = -1f;

        private static GUIStyle LabelStyle
        {
            get
            {
                if (_labelStyle == null) BuildStyle();
                return _labelStyle;
            }
        }

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            // A cached GUIStyle does not survive a play mode transition intact, so it is dropped and
            // rebuilt rather than left to render with a broken font.
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            EditorApplication.projectChanged -= OnProjectChanged;
            EditorApplication.projectChanged += OnProjectChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state is not (PlayModeStateChange.ExitingEditMode or PlayModeStateChange.EnteredEditMode)) return;

            _labelStyle = null;

            // Break points were measured with the old style's font, so they mean nothing once it
            // has been rebuilt.
            LineCache.Clear();
        }

        private static void OnProjectChanged()
        {
            NameCache.Clear();
            LineCache.Clear();
        }

        private static void BuildStyle()
        {
            _labelStyle = new GUIStyle
            {
                fontSize = 10,
                alignment = TextAnchor.UpperCenter,

                // Lines are worked out here rather than left to IMGUI, which breaks a long name at
                // whatever character it runs out of room on.
                wordWrap = false,
                clipping = TextClipping.Overflow,
                margin = new RectOffset(0, 0, 0, 0)
            };

            if (EditorGUIUtility.isProSkin)
            {
                _backgroundColor = new Color32(51, 51, 51, 255);
                _selectedColor = new Color32(44, 93, 135, 255);
            }
            else
            {
                _backgroundColor = new Color32(190, 190, 190, 255);
                _selectedColor = new Color32(58, 114, 176, 255);
            }
        }

        public static void Draw(Rect rowRect, string assetPath, bool isListView, bool isFolder, ProjectModuleSettings settings)
        {
            if (string.IsNullOrEmpty(assetPath)) return;

            // Folders have no extension, and Path.GetExtension would happily invent one from a dot
            // in the name — every package folder is reverse-DNS, so they would all sprout a fake
            // ".textmeshpro" style suffix.
            bool showExtension = settings.showFileExtensions && !isFolder;

            if (isListView)
            {
                // List rows keep Unity's own label and get the extension appended after it, which
                // avoids covering and redrawing a row that is only one line tall anyway.
                if (showExtension) DrawListExtension(rowRect, assetPath);
                return;
            }

            if (!settings.twoLineNamesEnabled && !showExtension) return;

            DrawGridLabel(rowRect, assetPath, settings.twoLineNamesEnabled, showExtension);
        }

        private static (string name, string extension) Split(string assetPath)
        {
            if (NameCache.TryGetValue(assetPath, out (string name, string extension) cached)) return cached;

            (string name, string extension) parts =
                (Path.GetFileNameWithoutExtension(assetPath), Path.GetExtension(assetPath));

            NameCache[assetPath] = parts;
            return parts;
        }

        private static void DrawListExtension(Rect rowRect, string assetPath)
        {
            (string name, string extension) = Split(assetPath);
            if (string.IsNullOrEmpty(extension)) return;

            Object asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (!asset || IsRenaming(asset)) return;

            MeasureContent.text = name;
            float nameWidth = EditorStyles.label.CalcSize(MeasureContent).x;
            float x = rowRect.x + rowRect.height + ListLabelGap + nameWidth;

            MeasureContent.text = extension;
            float extensionWidth = EditorStyles.label.CalcSize(MeasureContent).x;

            // No room means Unity already ellipsised the name, and an extension tacked onto "Foo…"
            // would read as part of the name.
            if (x + extensionWidth > rowRect.xMax) return;

            DrawContent.text = extension;
            DrawContent.tooltip = string.Empty;

            Color previous = GUI.color;
            GUI.color = new Color(previous.r, previous.g, previous.b, 0.5f);
            GUI.Label(new Rect(x, rowRect.y, rowRect.xMax - x, rowRect.height), DrawContent, EditorStyles.label);
            GUI.color = previous;
        }

        private static void DrawGridLabel(Rect rowRect, string assetPath, bool wrap, bool showExtension)
        {
            Object asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (!asset) return;

            // While a rename is in progress the row hosts a live text field — drawing over it would
            // cover what is being typed.
            if (IsRenaming(asset)) return;

            (string name, string extension) = Split(assetPath);
            string text = showExtension ? name + extension : name;

            int lines = 1;

            if (wrap)
            {
                (string first, string second) = BuildLines(text, rowRect.width);

                if (second != null)
                {
                    text = first + "\n" + second;
                    lines = 2;
                }
                else
                {
                    text = first;
                }
            }

            DrawContent.text = text;
            DrawContent.tooltip = string.Empty;

            bool selected = Selection.Contains(asset);

            // Measured from the style rather than asked of CalcHeight: with wrapping off, the
            // reported height of a string containing a break is not reliably two lines' worth.
            float textHeight = LabelStyle.lineHeight * lines;
            Rect nameRect = new Rect(rowRect.x, rowRect.yMax - LabelInset, rowRect.width, textHeight + 4f);

            // The backing is deliberately wider than the text rect: Unity's own label bleeds a few
            // pixels past its bounds, and anything left showing reads as a double-drawn name.
            Rect backgroundRect = new Rect(nameRect.x - 6f, nameRect.y - 1f, nameRect.width + 12f, nameRect.height + 3f);
            EditorGUI.DrawRect(backgroundRect, selected ? _selectedColor : _backgroundColor);

            LabelStyle.normal.textColor = EditorGUIUtility.isProSkin || selected ? Color.white : Color.black;

            // Intentionally does not consume the click: the row still has to handle selection,
            // double-click-to-open and click-to-rename.
            GUI.Label(nameRect, DrawContent, LabelStyle);
        }

        private static bool IsRenaming(Object asset)
        {
            return Selection.activeObject == asset && EditorGUIUtility.editingTextField;
        }

        private static (string first, string second) BuildLines(string text, float maxWidth)
        {
            if (!Mathf.Approximately(_lineCacheWidth, maxWidth))
            {
                _lineCacheWidth = maxWidth;
                LineCache.Clear();
            }

            if (LineCache.TryGetValue(text, out (string first, string second) cached)) return cached;

            (string first, string second) lines = SplitLines(text, maxWidth);
            LineCache[text] = lines;
            return lines;
        }

        /// <summary>
        /// Breaks a name at a boundary that means something — after a separator, or where a new
        /// capitalised word begins — rather than wherever it happens to run out of room. Asset names
        /// are mostly single compound words, so a naive wrap breaks them mid-word and the result
        /// reads worse than the ellipsis it replaced.
        /// </summary>
        private static (string first, string second) SplitLines(string text, float maxWidth)
        {
            if (Width(text) <= maxWidth) return (text, null);

            CollectBoundaries(text);

            // The last boundary that still fits, so the first line is filled as far as it can be.
            int best = -1;
            foreach (int boundary in BoundaryBuffer)
            {
                if (Width(text.Substring(0, boundary)) > maxWidth) break;

                best = boundary;
            }

            if (best > 0)
            {
                string head = text.Substring(0, best).TrimEnd();
                string tail = text.Substring(best).TrimStart();

                return (head, Width(tail) <= maxWidth ? tail : Truncate(tail, maxWidth));
            }

            // Nothing to break on that fits. Splitting mid-word still beats one ellipsised line,
            // because the end of a name is usually what tells two assets apart.
            int cut = LongestFitting(text, maxWidth);
            if (cut <= 0) return (Truncate(text, maxWidth), null);

            return (text.Substring(0, cut), Truncate(text.Substring(cut), maxWidth));
        }

        private static void CollectBoundaries(string text)
        {
            BoundaryBuffer.Clear();

            for (int i = 1; i < text.Length; i++)
            {
                char current = text[i];
                char previous = text[i - 1];

                // After a run of separators, so they stay on the first line where they read as the
                // end of a word rather than the start of one.
                if (IsSeparator(previous))
                {
                    if (!IsSeparator(current)) BoundaryBuffer.Add(i);
                    continue;
                }

                if (char.IsUpper(current) && char.IsLower(previous)) BoundaryBuffer.Add(i);
            }
        }

        private static bool IsSeparator(char character)
        {
            foreach (char separator in Separators)
            {
                if (character == separator) return true;
            }

            return false;
        }

        /// <summary>Length of the longest prefix that fits, or 0 if not even one character does.</summary>
        private static int LongestFitting(string text, float maxWidth)
        {
            int fitting = 0;

            for (int i = 1; i <= text.Length; i++)
            {
                if (Width(text.Substring(0, i)) > maxWidth) break;

                fitting = i;
            }

            return fitting;
        }

        private static string Truncate(string text, float maxWidth)
        {
            for (int i = text.Length - 1; i > 0; i--)
            {
                string candidate = text.Substring(0, i) + "…";
                if (Width(candidate) <= maxWidth) return candidate;
            }

            return "…";
        }

        private static float Width(string text)
        {
            MeasureContent.text = text;
            return LabelStyle.CalcSize(MeasureContent).x;
        }
    }
}
