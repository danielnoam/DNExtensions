using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace DNExtensions.HelpfulEditor.Viewport
{
    /// <summary>
    /// Photoshop-style guides for laying UI out, drawn on the canvas rather than on the window.
    ///
    /// A guide is held as a fraction of the canvas rect, which is what keeps it where it was put when
    /// the reference resolution changes. World coordinates would not: a screen space canvas is rebuilt
    /// to match the Game View's resolution every time that changes, so a guide pinned to a world
    /// position would drift against the very thing it is there to line up.
    ///
    /// The work is split across the two event systems along the line the content falls on. The guide
    /// lines are in the scene, so they are drawn here with Handles in world space and track the camera.
    /// The rulers are furniture on the window, so they live in <see cref="SceneViewGuidesDrawer"/> as
    /// a VisualElement and hold still. Every gesture belongs to the drawer, which keeps the split from
    /// cutting through the middle of one.
    /// </summary>
    internal static class SceneViewGuides
    {
        private const float DragThicknessMultiplier = 3f;
        private const float OffAngleAlpha = 0.35f;

        /// <summary>Below this the grid stops being a grid and becomes a wash, at a line per pixel to draw it.</summary>
        private const float MinGridScreenSpacing = 5f;

        private const int MaxGridLines = 400;

        /// <summary>How close two guides have to be before generation treats them as the one guide.</summary>
        private const float DuplicateEpsilon = 0.0005f;

        private static readonly SceneViewGuideGeometry Geometry = new SceneViewGuideGeometry();

        public static void Process(SceneView sceneView, SceneViewSettings settings)
        {
            SceneViewGuidesDrawer drawer = SyncDrawer(sceneView, settings);
            if (drawer == null) return;

            Geometry.Update();
            drawer.SetGeometry(Geometry.ScreenRect, Geometry.ReferenceSize, Geometry.IsAxisAligned, Geometry.HasTarget);

            if (!Geometry.HasTarget) return;

            SceneViewGuideSnapping.Process(settings, Geometry);

            if (Event.current.type != EventType.Repaint) return;

            // The two have their own toggles and neither gates the other: a grid is a background to
            // work over, and wanting one without a set of guides drawn across it is the ordinary case.
            if (settings.gridEnabled) DrawGrid(settings);

            // Drawn second, so a guide sitting on a grid line still reads as the guide.
            if (settings.showRulers) DrawGuides(settings, drawer);
        }

        /// <summary>
        /// Attaches, detaches and relays out every open view. Call after a settings change, not per
        /// frame — and it has to do the detaching, because switching the module off stops the scene
        /// pass running at all, which would otherwise leave the last overlay attached for good.
        /// </summary>
        public static void Refresh()
        {
            SceneViewSettings settings = HelpfulEditorSettings.SceneView;

            foreach (SceneView sceneView in SceneView.sceneViews)
            {
                SyncDrawer(sceneView, settings)?.RefreshLayout();
            }

            SceneViewGuidesOverlay.RefreshVisibility();
            SceneView.RepaintAll();
        }

        /// <summary>
        /// The guide menu, reached by right-clicking a ruler or the toolbar's Guides button. Adding
        /// from here places the guide down the middle, which is the one position that needs no aiming.
        /// </summary>
        public static void ShowGuideMenu()
        {
            SceneViewSettings settings = HelpfulEditorSettings.SceneView;
            GenericMenu menu = new GenericMenu();

            // Adding while the rulers are off would drop a guide nobody can see, so the menu says no
            // rather than looking broken. Clearing stays available either way.
            if (settings.showRulers)
            {
                menu.AddItem(new GUIContent("Add Vertical Guide"), false, () => AddCentred(false));
                menu.AddItem(new GUIContent("Add Horizontal Guide"), false, () => AddCentred(true));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Add Vertical Guide"));
                menu.AddDisabledItem(new GUIContent("Add Horizontal Guide"));
            }

            if (settings.showRulers && HasRectSelection())
            {
                menu.AddItem(new GUIContent("From Selection/Edges"), false, () => AddGuidesFromSelection(true, false));
                menu.AddItem(new GUIContent("From Selection/Centre"), false, () => AddGuidesFromSelection(false, true));
                menu.AddItem(new GUIContent("From Selection/Edges and Centre"), false, () => AddGuidesFromSelection(true, true));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("From Selection/Edges"));
            }

            if (settings.guides.Count > 0) menu.AddItem(new GUIContent("Clear All Guides"), false, ClearGuides);
            else menu.AddDisabledItem(new GUIContent("Clear All Guides"));

            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Show Grid"), settings.gridEnabled, ToggleGrid);

            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Settings…"), false, HelpfulEditorSettingsProvider.OpenSceneViewSettings);

            menu.ShowAsContext();
        }

        public static void ClearGuides()
        {
            HelpfulEditorSettings.SceneView.guides.Clear();
            HelpfulEditorSettings.SaveSceneView();

            Refresh();
        }

        private static void AddCentred(bool horizontal)
        {
            HelpfulEditorSettings.SceneView.guides.Add(new SceneViewGuide
            {
                isHorizontal = horizontal,
                normalizedPosition = 0.5f
            });

            HelpfulEditorSettings.SaveSceneView();
            Refresh();
        }

        /// <summary>
        /// The rulers and the guides are one feature, so turning it off takes the whole overlay away
        /// rather than leaving furniture behind with nothing to place.
        /// </summary>
        private static SceneViewGuidesDrawer SyncDrawer(SceneView sceneView, SceneViewSettings settings)
        {
            VisualElement root = sceneView ? sceneView.rootVisualElement : null;
            if (root == null) return null;

            SceneViewGuidesDrawer existing = root.Q<SceneViewGuidesDrawer>(SceneViewGuidesDrawer.OverlayName);

            if (!settings.moduleEnabled || !settings.guidesEnabled)
            {
                existing?.RemoveFromHierarchy();
                return null;
            }

            if (existing != null) return existing;

            SceneViewGuidesDrawer drawer = new SceneViewGuidesDrawer(sceneView);
            root.Add(drawer);
            PlaceBelowOverlays(root, drawer);

            return drawer;
        }

        /// <summary>
        /// Slid under the Scene View's own overlays where they can be found, so a docked toolbar keeps
        /// its clicks instead of losing them to a ruler painted across it. The overlay canvas is not
        /// public API and is only matched by name, so this is best effort — on top is a workable
        /// second best, and is where the Game View's rulers sit anyway.
        /// </summary>
        private static void PlaceBelowOverlays(VisualElement root, VisualElement drawer)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                VisualElement child = root[i];
                if (child == drawer) continue;

                string childName = child.name ?? string.Empty;

                bool isOverlay = childName.IndexOf("overlay", StringComparison.OrdinalIgnoreCase) >= 0
                                 || child.GetType().Name.IndexOf("Overlay", StringComparison.Ordinal) >= 0;

                if (!isOverlay) continue;

                drawer.PlaceBehind(child);
                return;
            }
        }

        /// <summary>
        /// A grid in the canvas' own units, drawn on the canvas plane with everything else rather than
        /// on the window — so it sits under the UI it is there to measure, and holds still against it.
        /// </summary>
        private static void DrawGrid(SceneViewSettings settings)
        {
            Color major = settings.gridColor;
            Color minor = settings.gridSubdivisionColor;

            if (!Geometry.IsAxisAligned)
            {
                major.a *= OffAngleAlpha;
                minor.a *= OffAngleAlpha;
            }

            float cell = Mathf.Max(1f, settings.gridCellSize);
            int subdivisions = Mathf.Clamp(settings.gridSubdivisions, 0, 16);

            Color previousColor = Handles.color;
            CompareFunction previousZTest = Handles.zTest;

            Handles.zTest = CompareFunction.Always;

            // Subdivisions first, so a major line paints over the one it shares a position with.
            if (subdivisions > 0) DrawGridLines(cell / (subdivisions + 1), minor);
            DrawGridLines(cell, major);

            Handles.color = previousColor;
            Handles.zTest = previousZTest;
        }

        private static void DrawGridLines(float spacing, Color color)
        {
            if (color.a <= 0f) return;

            DrawGridAxis(false, spacing, color);
            DrawGridAxis(true, spacing, color);
        }

        private static void DrawGridAxis(bool horizontal, float spacing, Color color)
        {
            float size = horizontal ? Geometry.ReferenceSize.y : Geometry.ReferenceSize.x;

            // Dropped rather than drawn dense: at this zoom the lines would be closer together than
            // they are wide, which reads as a flat tint and costs a draw call per pixel to say it.
            if (spacing * Mathf.Abs(Geometry.ScreenScale(horizontal)) < MinGridScreenSpacing) return;

            int count = Mathf.FloorToInt(size / spacing);
            if (count > MaxGridLines) return;

            Handles.color = color;

            for (int i = 0; i <= count; i++)
            {
                float normalized = i * spacing / size;
                if (normalized > 1f) break;

                Geometry.GetWorldEndpoints(horizontal, normalized, out Vector3 from, out Vector3 to);
                Handles.DrawAAPolyLine(1f, from, to);
            }
        }

        /// <summary>
        /// Guides along the edges and centre of whatever is selected, which is the quick way to get a
        /// layout to line up against something that already exists rather than against a number.
        /// </summary>
        public static void AddGuidesFromSelection(bool edges, bool centres)
        {
            SceneViewSettings settings = HelpfulEditorSettings.SceneView;
            if (!Geometry.HasTarget) return;

            int added = 0;

            foreach (Transform transform in Selection.transforms)
            {
                if (!(transform is RectTransform rectTransform)) continue;
                if (!Geometry.TryGetLocalBounds(rectTransform, out Bounds bounds)) continue;

                if (edges)
                {
                    added += TryAddGuide(settings, false, bounds.min.x);
                    added += TryAddGuide(settings, false, bounds.max.x);
                    added += TryAddGuide(settings, true, bounds.min.y);
                    added += TryAddGuide(settings, true, bounds.max.y);
                }

                if (centres)
                {
                    added += TryAddGuide(settings, false, bounds.center.x);
                    added += TryAddGuide(settings, true, bounds.center.y);
                }
            }

            if (added == 0) return;

            HelpfulEditorSettings.SaveSceneView();
            Refresh();
        }

        /// <summary>
        /// Skipped where one already sits, so running this twice over the same selection does not stack
        /// guides on top of each other — and where the rect falls outside the canvas, because a guide
        /// clamped back onto the edge would not be where it was asked for.
        /// </summary>
        private static int TryAddGuide(SceneViewSettings settings, bool horizontal, float local)
        {
            float normalized = Geometry.LocalToNormalized(horizontal, local);
            if (normalized < 0f || normalized > 1f) return 0;

            foreach (SceneViewGuide guide in settings.guides)
            {
                if (guide.isHorizontal != horizontal) continue;
                if (Mathf.Abs(guide.normalizedPosition - normalized) < DuplicateEpsilon) return 0;
            }

            settings.guides.Add(new SceneViewGuide { isHorizontal = horizontal, normalizedPosition = normalized });

            return 1;
        }

        public static bool HasRectSelection()
        {
            foreach (Transform transform in Selection.transforms)
            {
                if (transform is RectTransform) return true;
            }

            return false;
        }

        public static void ToggleGrid()
        {
            SceneViewSettings settings = HelpfulEditorSettings.SceneView;

            settings.gridEnabled = !settings.gridEnabled;
            HelpfulEditorSettings.SaveSceneView();

            Refresh();
        }

        private static void DrawGuides(SceneViewSettings settings, SceneViewGuidesDrawer drawer)
        {
            Color guideColor = settings.guideColor;
            Color outlineColor = settings.guideOutlineColor;

            // Off angle the guides still say where they are, but nothing can be placed against them
            // from here — so they read as inactive rather than as something to aim at.
            if (!Geometry.IsAxisAligned)
            {
                guideColor.a *= OffAngleAlpha;
                outlineColor.a *= OffAngleAlpha;
            }

            float width = Mathf.Max(0.5f, settings.guideWidth);
            float outline = Mathf.Max(0f, settings.guideOutlineWidth);

            bool previewHorizontal = false;
            float previewNormalized = 0f;
            int movedIndex = -1;
            bool dragging = drawer.TryGetPreview(out previewHorizontal, out previewNormalized, out movedIndex);
            int hover = drawer.HoverIndex;

            Color previousColor = Handles.color;
            CompareFunction previousZTest = Handles.zTest;

            // A guide lies exactly on the canvas plane, so depth testing it against the UI it is drawn
            // over is a coin toss that comes up z-fighting.
            Handles.zTest = CompareFunction.Always;

            for (int i = 0; i < settings.guides.Count; i++)
            {
                if (i == movedIndex) continue;

                SceneViewGuide guide = settings.guides[i];
                bool snapped = SceneViewGuideSnapping.IsSnapped(i);

                Color color = snapped ? settings.guideSnapColor : guideColor;
                if (snapped && !Geometry.IsAxisAligned) color.a *= OffAngleAlpha;

                DrawGuideLine(guide.isHorizontal, guide.normalizedPosition, color, outlineColor,
                    snapped || i == hover ? width + 1f : width, outline);
            }

            if (dragging)
            {
                DrawGuideLine(previewHorizontal, previewNormalized, settings.guideColor, settings.guideOutlineColor,
                    Mathf.Max(width * DragThicknessMultiplier, width + 2f), outline);
            }

            Handles.color = previousColor;
            Handles.zTest = previousZTest;
        }

        /// <summary>
        /// The outline goes down first as a wider line of the same shape, so what shows either side of
        /// the guide is a border rather than a second line. Without it a cyan guide disappears into
        /// cyan UI, which is exactly when a guide is most needed.
        /// </summary>
        private static void DrawGuideLine(bool horizontal, float normalized, Color color, Color outline,
            float thickness, float outlineWidth)
        {
            Geometry.GetWorldEndpoints(horizontal, normalized, out Vector3 from, out Vector3 to);

            if (outlineWidth > 0f && outline.a > 0f)
            {
                Handles.color = outline;
                Handles.DrawAAPolyLine(thickness + outlineWidth * 2f, from, to);
            }

            Handles.color = color;
            Handles.DrawAAPolyLine(thickness, from, to);
        }
    }
}
