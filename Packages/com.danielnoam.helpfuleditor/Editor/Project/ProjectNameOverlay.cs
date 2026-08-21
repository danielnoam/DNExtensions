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

        /// <summary>The style the Project window labels its own grid rows with, selection highlight and all.</summary>
        private const string GridLabelStyleName = "ProjectBrowserGridLabel";

        /// <summary>Breathing room between the name and the edge of the highlight around it.</summary>
        private const float HighlightPadding = 10f;

        /// <summary>How far the highlight sits above the first line, so its slack is shared between the
        /// top and the bottom rather than all of it landing under the last line.</summary>
        private const float HighlightLift = 2f;

        private static readonly char[] Separators = { ' ', '_', '-', '.' };

        // Path splitting allocates, and this runs for every visible row on every repaint.
        private static readonly Dictionary<string, (string name, string extension)> NameCache =
            new Dictionary<string, (string, string)>();

        // Measuring text is the expensive half of laying out two lines, and a name's break point
        // only changes when the cell width does.
        private static readonly Dictionary<string, (string first, string second)> LineCache =
            new Dictionary<string, (string, string)>();

        // The same, for the single-line form: one cut point per name per width.
        private static readonly Dictionary<string, string> SingleLineCache = new Dictionary<string, string>();

        // Widths of the drawn text, which is what the highlight is sized from. Keyed by the final
        // string, so a name and the same name over two lines are separate entries.
        private static readonly Dictionary<string, float> WidthCache = new Dictionary<string, float>();

        private static readonly List<int> BoundaryBuffer = new List<int>();

        private static readonly GUIContent MeasureContent = new GUIContent();
        private static readonly GUIContent DrawContent = new GUIContent();

        private static GUIStyle _labelStyle;
        private static GUIStyle _highlightStyle;
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
            SingleLineCache.Clear();
            WidthCache.Clear();
        }

        private static void OnProjectChanged()
        {
            NameCache.Clear();
            LineCache.Clear();
            SingleLineCache.Clear();
            WidthCache.Clear();
        }

        /// <summary>
        /// Two styles, because they are two jobs. The highlight is Unity's own grid label, used for
        /// nothing but its background — its shape, its colours, and the grey it turns when the window
        /// loses focus, none of which is worth reproducing by hand. The text is ours, because the two
        /// lines are measured and broken here and a style that brings its own padding, content offset
        /// and alignment moves the name around inside a box sized by different arithmetic. Asking one
        /// style to do both is what left a second line hanging outside the highlight.
        /// </summary>
        private static void BuildStyle()
        {
            GUIStyle skinStyle = GUI.skin.FindStyle(GridLabelStyleName);

            // Copied, never used directly: the skin's styles are shared, and a field set on one of
            // them changes it everywhere Unity draws with it.
            if (skinStyle != null)
            {
                _highlightStyle = new GUIStyle(skinStyle)
                {
                    // The reason a two-line name kept getting a one-line highlight. Unity's grid label
                    // is a single row, so it pins its own height — and a style with fixedHeight set
                    // renders at that height whatever rect it is handed, so nothing about the rect
                    // this is drawn into could ever have made it taller.
                    fixedHeight = 0f,
                    fixedWidth = 0f,
                    stretchHeight = true,
                    stretchWidth = true
                };
            }
            else
            {
                _highlightStyle = null;
            }

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

            WidthCache.Clear();

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

        public static void Draw(Rect rowRect, string assetPath, bool isListView, bool isFolder, bool selected, ProjectSettings settings)
        {
            if (string.IsNullOrEmpty(assetPath)) return;

            // Folders have no extension, and Path.GetExtension would happily invent one from a dot
            // in the name — every package folder is reverse-DNS, so they would all sprout a fake
            // ".textmeshpro" style suffix.
            bool showExtension = settings.showFileExtensionsEnabled && !isFolder;

            if (isListView)
            {
                // List rows keep Unity's own label and get the extension appended after it, which
                // avoids covering and redrawing a row that is only one line tall anyway.
                if (showExtension) DrawListExtension(rowRect, assetPath);
                return;
            }

            if (!settings.twoLineNamesEnabled && !showExtension) return;

            DrawGridLabel(rowRect, assetPath, settings.twoLineNamesEnabled, showExtension, selected);
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

            if (IsRenaming(assetPath)) return;

            float nameWidth = HelpfulEditorGUI.LabelWidth(name);
            float x = rowRect.x + rowRect.height + ListLabelGap + nameWidth;

            float extensionWidth = HelpfulEditorGUI.LabelWidth(extension);

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

        /// <summary>The widest of the drawn lines, which is what the highlight is sized to.</summary>
        private static float LineWidth(string text)
        {
            if (WidthCache.TryGetValue(text, out float cached)) return cached;

            float widest = 0f;

            foreach (string line in text.Split('\n'))
            {
                widest = Mathf.Max(widest, Width(line));
            }

            WidthCache[text] = widest;
            return widest;
        }

        /// <param name="selected">
        /// Whether the row is in the selection. Handed down rather than asked of Selection here: the
        /// caller has already settled it for this row, and answering it again meant loading the asset
        /// purely to have something to pass to Selection.Contains.
        /// </param>
        private static void DrawGridLabel(Rect rowRect, string assetPath, bool wrap, bool showExtension, bool selected)
        {
            // GUIStyle.Draw is a repaint-only call, and nothing here is anything but drawing — no
            // control is claimed, so there is nothing for the other events to do either.
            if (Event.current.type != EventType.Repaint) return;

            // While a rename is in progress the row hosts a live text field — drawing over it would
            // cover what is being typed.
            if (IsRenaming(assetPath)) return;

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
            else
            {
                // The style overflows rather than clips — which is what lets the two-line form draw
                // past the cell it was measured in — so a single line has to be cut to fit or a long
                // name runs straight over the one beside it. Only reachable with wrapping off and
                // extensions on, since that is the one case that draws an unmeasured line.
                text = FitOneLine(text, rowRect.width);
            }

            DrawContent.text = text;
            DrawContent.tooltip = string.Empty;

            // Measured from the style rather than asked of CalcHeight: with wrapping off, the
            // reported height of a string containing a break is not reliably two lines' worth. The
            // style is the plain one, which carries no padding of its own, so this is the whole of it.
            float textHeight = LabelStyle.lineHeight * lines;
            Rect nameRect = new Rect(rowRect.x, rowRect.yMax - LabelInset, rowRect.width, textHeight + 4f);

            // Covered in the plain background whatever the state, including selected: Unity's own
            // highlight is hidden along with the label it sits behind, and a new one is drawn below
            // around the name this actually shows. The backing is deliberately wider than the text
            // rect — Unity's label bleeds a few pixels past its bounds, and anything left showing
            // reads as a double-drawn name.
            // Hugging the text rather than filling the cell, which is what Unity's own highlight does
            // and the largest part of why filling it looked wrong at bigger icon sizes.
            float width = Mathf.Min(rowRect.width, LineWidth(text) + HighlightPadding);
            float x = rowRect.x + (rowRect.width - width) * 0.5f;

            Rect textRect = new Rect(x, nameRect.y, width, nameRect.height);

            // Lifted off the text rather than sharing its rect. Sharing it put the top edge exactly on
            // the first line and left the whole of the slack under the last one, which reads as a box
            // sitting too low behind the name.
            Rect highlightRect = new Rect(x, nameRect.y - HighlightLift, width, nameRect.height);

            Rect backgroundRect = new Rect(nameRect.x - 6f, highlightRect.y - 1f, nameRect.width + 12f, nameRect.height + 5f);
            EditorGUI.DrawRect(backgroundRect, _backgroundColor);

            // Unity greys a selection while its window is not the focused one, and the style knows how
            // — it only has to be told which of the two this is.
            bool focused = HelpfulEditorWindows.IsProjectBrowser(EditorWindow.focusedWindow);

            if (selected) DrawHighlight(highlightRect, focused);

            LabelStyle.normal.textColor = TextColor(selected, focused);

            // Intentionally does not consume the click: the row still has to handle selection,
            // double-click-to-open and click-to-rename.
            GUI.Label(textRect, DrawContent, LabelStyle);
        }

        /// <summary>
        /// Drawn with empty content on purpose. The style is here for its background and nothing else,
        /// and handing it the name would let its own text metrics decide where the box ends.
        /// </summary>
        private static void DrawHighlight(Rect rect, bool focused)
        {
            if (_highlightStyle == null)
            {
                // No such style on this skin. A flat fill at least reads as a selection.
                EditorGUI.DrawRect(rect, _selectedColor);
                return;
            }

            _highlightStyle.Draw(rect, GUIContent.none, false, false, true, focused);
        }

        /// <summary>
        /// Taken from whichever state the highlight was drawn in, so the name reads against it. Some
        /// skins leave a state's colour unset, which arrives as transparent — drawing a name in
        /// nothing is worse than the plain rule this replaced, so that falls back to it.
        /// </summary>
        private static Color TextColor(bool selected, bool focused)
        {
            Color fallback = EditorGUIUtility.isProSkin || selected ? Color.white : Color.black;

            if (_highlightStyle == null) return fallback;

            GUIStyleState state = selected
                ? focused ? _highlightStyle.onFocused : _highlightStyle.onNormal
                : _highlightStyle.normal;

            return state != null && state.textColor.a > 0f ? state.textColor : fallback;
        }

        /// <summary>
        /// Only while this row hosts a live text field, which drawing over would cover what is being
        /// typed. It used to be enough to ask whether the active object was being edited, but
        /// EditorGUIUtility.editingTextField is global — a focused field anywhere in the editor sets
        /// it — so the selected row lost its name and extension whenever anything else held the caret,
        /// which is most of the time an asset is selected.
        ///
        /// The flag is still the first thing checked, as the cheap half: a rename is always a focused
        /// text field, so with nothing being typed anywhere there is nothing to look up. It is only
        /// the converse that does not hold, and the lookup is what settles that.
        ///
        /// Takes the path rather than the asset so the flag is reached before anything is loaded.
        /// The callers used to load the asset to hand it over, which meant every visible row of every
        /// repaint went through LoadMainAssetAtPath — and that deserialises anything not already in
        /// memory, so scrolling a folder of textures pulled the lot in to answer a question that is
        /// almost always no.
        /// </summary>
        private static bool IsRenaming(string assetPath)
        {
            if (!EditorGUIUtility.editingTextField) return false;

            Object asset = AssetDatabase.LoadMainAssetAtPath(assetPath);

            return asset && HelpfulEditorTreeReflection.IsProjectRenaming(HelpfulEditorObjectId.Raw(asset));
        }

        /// <summary>Break points and cut points were measured against a width, so they mean nothing once it changes.</summary>
        private static void EnsureCacheWidth(float maxWidth)
        {
            if (Mathf.Approximately(_lineCacheWidth, maxWidth)) return;

            _lineCacheWidth = maxWidth;

            LineCache.Clear();
            SingleLineCache.Clear();
        }

        /// <summary>The one-line form, cut to the cell so a long name does not run into its neighbour.</summary>
        private static string FitOneLine(string text, float maxWidth)
        {
            EnsureCacheWidth(maxWidth);

            if (SingleLineCache.TryGetValue(text, out string cached)) return cached;

            string fitted = Width(text) <= maxWidth ? text : Truncate(text, maxWidth);

            SingleLineCache[text] = fitted;
            return fitted;
        }

        private static (string first, string second) BuildLines(string text, float maxWidth)
        {
            EnsureCacheWidth(maxWidth);

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
