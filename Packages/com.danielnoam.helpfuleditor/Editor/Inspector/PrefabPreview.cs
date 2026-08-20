using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.HelpfulEditor.Inspector
{
    /// <summary>
    /// One preview covering what Unity's own leaves out — UI, which it shows nothing for, and
    /// particles, which it shows a single unsimulated frame of.
    ///
    /// One rather than two because the inspector shows a single preview at a time: two entries meant a
    /// prefab carrying both could only be seen half at once, and a selection of a UI prefab beside an
    /// effect could only be seen as whichever kind the first of them happened to be. Each object works
    /// out for itself what it is, so a mixed selection draws every cell as what it actually is.
    ///
    /// Registered as an additional preview rather than as an editor, so Unity's own GameObject
    /// inspector is left alone and switching this off restores exactly what was there.
    /// </summary>
    [CustomPreview(typeof(GameObject))]
    internal sealed class PrefabPreview : ObjectPreview
    {
        /// <summary>Repaint rate while playing. The editor update loop runs several times faster than
        /// anything worth showing, and every tick of it is a repaint of every inspector.</summary>
        private const double FrameInterval = 1.0 / 30.0;

        /// <summary>A stall — a compile, a domain reload, a modal dialog — must not arrive as a
        /// half-second jump through the effect.</summary>
        private const float MaxDelta = 0.1f;

        /// <summary>Past this a seek steps in one go rather than in fixed increments. Accuracy is worth
        /// a few hundred steps at two seconds and is not worth several thousand at twenty.</summary>
        private const float AccurateSeekLimit = 5f;

        /// <summary>Wide enough for the readout at any length it is likely to show. Fixed, because the
        /// settings strip is right-aligned — a label that grows by a digit shoves the scrub bar along
        /// with it, and a scrub bar that moves while you watch it is one you cannot aim at.</summary>
        private const float TimeLabelWidth = 46f;

        private const float ScrubWidth = 110f;
        private const float SwatchWidth = 38f;

        /// <summary>
        /// The area a canvas is given when it has no usable size of its own, and what a prefab that
        /// stretches to its parent ends up filling. A fixed-size prefab keeps its own size.
        /// </summary>
        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

        /// <summary>Square-on for flat things; a three-quarter view for everything else, since square-on
        /// hides the depth of a burst.</summary>
        private static readonly Vector2 FlatAngles = Vector2.zero;
        private static readonly Vector2 SpatialAngles = new Vector2(25f, 15f);

        /// <summary>What one selected object needs to be drawn and kept in step with the clock.</summary>
        private sealed class Entry
        {
            public PreviewStage Stage;

            /// <summary>Drawn through a canvas and framed flat-on. Decided per object, not per selection.</summary>
            public bool Ui;

            public ParticleSystem Root;
            public Bounds Bounds;

            /// <summary>Whether <see cref="Bounds"/> holds an effect measured inside a canvas, which is
            /// the mixed case — the flat frame comes from the canvas and this is folded into it.</summary>
            public bool HasParticleBounds;

            /// <summary>Where this copy's simulation has got to, which is not where the clock is until
            /// it has been drawn. Negative until it has been simulated at all.</summary>
            public float SimulatedTo = -1f;
        }

        private readonly Dictionary<GameObject, Entry> _entries = new Dictionary<GameObject, Entry>();

        private PreviewViewControls _view = new PreviewViewControls(SpatialAngles);

        private bool _anyUi;
        private bool _anyParticles;
        private Object _claimed;

        private float _time;
        private float _length = 1f;
        private bool _playing;
        private bool _subscribed;
        private double _lastFrame;
        private double _lastRepaint;

        /// <summary>
        /// Every target is looked at, not just the reference one, or a selection whose first object is
        /// an ordinary mesh would hide the preview from the UI prefab sitting next to it. Answered once
        /// here rather than on demand: HasPreviewGUI is asked several times a repaint, and the question
        /// walks the whole hierarchy of everything selected.
        /// </summary>
        public override void Initialize(Object[] targets)
        {
            base.Initialize(targets);

            _anyUi = false;
            _anyParticles = false;

            foreach (Object candidate in targets)
            {
                if (!(candidate is GameObject go)) continue;

                if (go.GetComponentInChildren<RectTransform>(true)) _anyUi = true;
                if (go.GetComponentInChildren<ParticleSystem>(true)) _anyParticles = true;
            }
        }

        public override bool HasPreviewGUI()
        {
            InspectorSettings settings = HelpfulEditorSettings.Inspector;

            if (!settings.moduleEnabled) return false;
            if (!settings.uiPreviewEnabled && !settings.particlePreviewEnabled) return false;
            if (!EditorUtility.IsPersistent(target)) return false;

            bool wanted = (settings.uiPreviewEnabled && _anyUi) || (settings.particlePreviewEnabled && _anyParticles);
            if (!wanted) return false;

            // Once per selection rather than per call — this runs several times a repaint, and the
            // work behind it walks every open window.
            if (_claimed != target)
            {
                _claimed = target;
                PreviewStage.ClaimSelection(this);
            }

            return true;
        }

        public override GUIContent GetPreviewTitle() => new GUIContent("Live Preview");

        public override void OnInteractivePreviewGUI(Rect rect, GUIStyle background)
        {
            _view.Handle(rect);

            OnPreviewGUI(rect, background);
        }

        public override void OnPreviewGUI(Rect rect, GUIStyle background)
        {
            if (Event.current.type != EventType.Repaint) return;

            if (!TryGetEntry(target as GameObject, out Entry entry))
            {
                EditorGUI.DropShadowLabel(rect, $"{PreviewStage.MaxTargets} at a time");
                return;
            }

            if (entry.Stage == null || !entry.Stage.Ready)
            {
                EditorGUI.DropShadowLabel(rect, "Nothing to preview");
                return;
            }

            if (entry.Root) SyncToClock(entry);

            InspectorSettings settings = HelpfulEditorSettings.Inspector;

            entry.Stage.Camera.backgroundColor = entry.Ui
                ? settings.uiPreviewBackground
                : settings.particlePreviewBackground;

            entry.Stage.Begin(rect, background);

            if (entry.Ui)
            {
                // The rects are measured from built geometry, and on the first render of a selection
                // there is none until the canvas has been through a rebuild.
                Canvas.ForceUpdateCanvases();

                Bounds bounds = ContentBounds(entry.Stage.Instance);

                if (entry.HasParticleBounds) bounds.Encapsulate(entry.Bounds);

                float aspect = rect.height > 0f ? rect.width / rect.height : 1f;
                _view.ApplyOrthographic(entry.Stage.Camera, bounds, aspect, 1.1f);
            }
            else
            {
                _view.ApplyPerspective(entry.Stage.Camera, entry.Bounds);
            }

            entry.Stage.Render(rect);
        }

        /// <summary>
        /// The controls, in the strip the preview area keeps for them — and where the clock is
        /// advanced, because this strip is drawn once a repaint whereas OnPreviewGUI is drawn once per
        /// cell of the grid, and a clock ticked per cell would run a selection of six at six times
        /// speed. The scrub bar sits here for the same reason.
        /// </summary>
        public override void OnPreviewSettings()
        {
            if (_entries.Count == 0) return;

            InspectorSettings settings = HelpfulEditorSettings.Inspector;

            // Repaint only, or the layout pass would advance the effect a second time each frame.
            if (Event.current.type == EventType.Repaint) Tick();

            if (AnyPlayable()) DrawPlaybackControls();

            using (new EditorGUI.DisabledScope(!_view.Moved))
            {
                GUIContent reset = HelpfulEditorGUI.IconContent("Reset the view", "ViewToolOrbit", "RotateTool")
                                   ?? new GUIContent("View", "Reset the view");

                if (GUILayout.Button(reset, EditorStyles.toolbarButton)) _view.Reset();
            }

            DrawBackgroundSwatch(settings);
        }

        private void DrawPlaybackControls()
        {
            EditorGUI.BeginChangeCheck();

            float time = GUILayout.HorizontalSlider(_time, 0f, _length, GUILayout.Width(ScrubWidth));

            // Touching it pauses: scrubbing against a clock that is still running fights the hand
            // holding it.
            if (EditorGUI.EndChangeCheck())
            {
                SetPlaying(false);
                Seek(time);
            }

            GUILayout.Label($"{_time:0.00}s", EditorStyles.miniLabel, GUILayout.Width(TimeLabelWidth));

            GUIContent restart = HelpfulEditorGUI.IconContent("Restart from the beginning", "Refresh", "RotateTool")
                                 ?? new GUIContent("R", "Restart from the beginning");

            if (GUILayout.Button(restart, EditorStyles.toolbarButton)) Seek(0f);

            GUIContent play = _playing
                ? HelpfulEditorGUI.IconContent("Pause", "PauseButton") ?? new GUIContent("II", "Pause")
                : HelpfulEditorGUI.IconContent("Play", "PlayButton") ?? new GUIContent(">", "Play");

            if (GUILayout.Button(play, EditorStyles.toolbarButton)) SetPlaying(!_playing);
        }

        /// <summary>
        /// Edits whichever of the two backgrounds the object on show uses, so the swatch always changes
        /// the colour under it. They are kept apart because a UI screen is often checked against
        /// something light and an additive effect is lost on anything but dark.
        /// </summary>
        private void DrawBackgroundSwatch(InspectorSettings settings)
        {
            bool ui = _entries.TryGetValue(target as GameObject, out Entry entry) && entry.Ui;

            Color current = ui ? settings.uiPreviewBackground : settings.particlePreviewBackground;

            Color picked = EditorGUILayout.ColorField(GUIContent.none, current,
                false, false, false, GUILayout.Width(SwatchWidth));

            if (picked == current) return;

            if (ui) settings.uiPreviewBackground = picked;
            else settings.particlePreviewBackground = picked;

            HelpfulEditorSettings.SaveInspector();
        }

        /// <summary>
        /// The base call is only GC.SuppressFinalize, but leaving it out is what makes ObjectPreview's
        /// finalizer log that this was never disposed — once per instance, and the inspector builds a
        /// fresh one per selection.
        /// </summary>
        public override void Cleanup()
        {
            SetPlaying(false);

            foreach (Entry entry in _entries.Values)
            {
                entry.Stage?.Destroy();
            }

            _entries.Clear();

            base.Cleanup();
        }

        private bool TryGetEntry(GameObject source, out Entry entry)
        {
            entry = null;
            if (!source) return false;

            if (_entries.TryGetValue(source, out entry)) return true;
            if (_entries.Count >= PreviewStage.MaxTargets) return false;

            entry = new Entry { Stage = new PreviewStage() };
            _entries.Add(source, entry);

            entry.Stage.Rebuild(source);
            Build(entry);

            return true;
        }

        private void Build(Entry entry)
        {
            if (!entry.Stage.Ready) return;

            InspectorSettings settings = HelpfulEditorSettings.Inspector;
            GameObject instance = entry.Stage.Instance;

            entry.Ui = settings.uiPreviewEnabled && instance.GetComponentInChildren<RectTransform>(true);

            if (settings.particlePreviewEnabled)
            {
                entry.Root = instance.GetComponentInChildren<ParticleSystem>(true);

                // Before anything simulates, including the bounds sample below — a seed changed after
                // that sample would mean the effect was framed on a take other than the one played.
                if (entry.Root) FixSeeds(instance);
            }

            Camera camera = entry.Stage.Camera;
            camera.clearFlags = CameraClearFlags.SolidColor;

            if (entry.Ui) BuildCanvas(entry);
            else BuildSpatial(entry);

            // A canvas prefab with an effect in it is simulated and controllable either way, but the
            // frame it is drawn in comes from CanvasRenderers and a particle system is not one — so
            // without this the effect plays outside the view. Sampled once, and after the canvas has
            // settled where everything sits, since that is what puts the particles somewhere.
            if (entry.Ui && entry.Root)
            {
                entry.Bounds = SampleBounds(entry);
                entry.HasParticleBounds = true;
            }

            // The opening angle follows whatever the first object turned out to be. Flat content seen
            // from three-quarters is a sliver, and a burst seen square-on is a smudge.
            if (_entries.Count == 1) _view = new PreviewViewControls(entry.Ui ? FlatAngles : SpatialAngles);

            if (!entry.Root) return;

            // The bar spans the longest of the selection, so a short effect sits still at the end
            // rather than the long one being cut off.
            _length = Mathf.Max(_length, Length(entry.Root));

            SetPlaying(_playing || settings.particlePreviewAutoPlay);
        }

        /// <summary>
        /// Pins the seeds so a restart reproduces the take it just showed, rather than reading as a
        /// randomiser. Everything is stopped first because a system refuses the seed while it is
        /// playing — and a freshly instantiated prefab is playing, since Play On Awake fires the
        /// moment the copy lands in the scene. Two passes, not one: stopping a system whose parent is
        /// still running leaves it running with it.
        /// </summary>
        private static void FixSeeds(GameObject instance)
        {
            ParticleSystem[] systems = instance.GetComponentsInChildren<ParticleSystem>(true);

            foreach (ParticleSystem system in systems)
            {
                system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            foreach (ParticleSystem system in systems)
            {
                system.useAutoRandomSeed = false;
            }
        }

        private void BuildCanvas(Entry entry)
        {
            entry.Stage.Camera.orthographic = true;

            // UI shaders are unlit, so the preview's lights have nothing to do here. Ambient is set
            // full anyway: anything in the prefab that is lit should not come out black.
            entry.Stage.AmbientColor = Color.white;

            Canvas own = entry.Stage.Instance.GetComponent<Canvas>();

            if (own) UseOwnCanvas(entry.Stage, own);
            else WrapInCanvas(entry.Stage);
        }

        private static void BuildSpatial(Entry entry)
        {
            Camera camera = entry.Stage.Camera;
            camera.orthographic = false;
            camera.fieldOfView = 30f;

            entry.Stage.AmbientColor = new Color(0.6f, 0.6f, 0.6f, 1f);

            entry.Bounds = entry.Root ? SampleBounds(entry) : StaticBounds(entry.Stage.Instance);
        }

        /// <summary>
        /// A prefab whose root is already a Canvas is driven rather than wrapped. A Canvas drives its
        /// own RectTransform to the screen only while it is a root canvas in a loaded scene, so the
        /// same prefab that previews from the hierarchy — where it has been given a screen-sized rect —
        /// has a degenerate one as an asset, and lays its children out inside nothing. World space
        /// stops the driving and lets the size be set.
        /// </summary>
        private static void UseOwnCanvas(PreviewStage stage, Canvas canvas)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = stage.Camera;
            canvas.scaleFactor = 1f;

            if (!(canvas.transform is RectTransform rect)) return;

            Rect area = rect.rect;
            if (area.width >= 1f && area.height >= 1f) return;

            rect.sizeDelta = ReferenceResolution;
        }

        /// <summary>
        /// A canvas of our own for everything else, because uGUI draws nothing without one and a
        /// prefab is as often a single Button as it is a whole screen. World space rather than either
        /// screen mode: a world-space canvas is ordinary geometry in the XY plane, so it asks nothing
        /// of the camera beyond being pointed at it.
        /// </summary>
        private static void WrapInCanvas(PreviewStage stage)
        {
            GameObject root = stage.CreateObject("HelpfulEditor UI Preview", typeof(Canvas));
            if (!root) return;

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = stage.Camera;
            canvas.scaleFactor = 1f;

            if (root.transform is RectTransform rect) rect.sizeDelta = ReferenceResolution;

            // Keeps the prefab's own anchoring, which is the point of giving it a screen-sized canvas
            // to anchor against.
            stage.Instance.transform.SetParent(root.transform, false);
        }

        /// <summary>
        /// Framed once, from where the effect gets to rather than where it starts: a burst is a point
        /// at t=0, and a camera framed on that would sit inside the effect it is meant to show. Fixed
        /// afterwards, because a camera that refits itself every frame reads as the effect shrinking.
        /// </summary>
        private static Bounds SampleBounds(Entry entry)
        {
            float span = Mathf.Max(0.1f, Mathf.Min(entry.Root.main.duration, 3f));

            Bounds bounds = default;
            bool any = false;

            // A fixed step here, unlike playback: this runs once per selection, and the point of the
            // sample is that the particles have genuinely travelled rather than jumped.
            foreach (float fraction in new[] { 0.25f, 0.6f, 1f })
            {
                entry.Root.Simulate(span * fraction, true, true);

                Encapsulate(entry.Stage.Instance, ref bounds, ref any);
            }

            if (!any) return new Bounds(entry.Stage.Instance.transform.position, Vector3.one);

            // Nothing to look at otherwise, and a zero radius divides through the framing maths.
            if (bounds.size.magnitude < 0.01f) bounds.size = Vector3.one;

            return bounds;
        }

        /// <summary>For the plain objects a mixed selection drags along — no particles, no canvas.</summary>
        private static Bounds StaticBounds(GameObject instance)
        {
            Bounds bounds = default;
            bool any = false;

            Encapsulate(instance, ref bounds, ref any);

            return any ? bounds : new Bounds(instance.transform.position, Vector3.one);
        }

        private static void Encapsulate(GameObject instance, ref Bounds bounds, ref bool any)
        {
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(false))
            {
                Bounds current = renderer.bounds;
                if (current.size == Vector3.zero) continue;

                if (!any)
                {
                    bounds = current;
                    any = true;
                    continue;
                }

                bounds.Encapsulate(current);
            }
        }

        private bool AnyPlayable()
        {
            foreach (Entry entry in _entries.Values)
            {
                if (entry.Root) return true;
            }

            return false;
        }

        /// <summary>Advances the one clock every copy follows. Called once a repaint, not once a cell.</summary>
        private void Tick()
        {
            double now = EditorApplication.timeSinceStartup;
            float delta = (float)(now - _lastFrame);
            _lastFrame = now;

            if (!_playing || delta <= 0f) return;
            if (delta > MaxDelta) delta = MaxDelta;

            _time += delta;

            // Run again rather than left as a dead frame — a preview that has to be restarted by hand
            // to be seen twice is a preview nobody watches twice.
            if (_time > _length) _time = 0f;
        }

        /// <summary>
        /// Catches one copy up to the clock. Forward is stepped, which is a single step whatever the
        /// distance; backwards — a scrub, or the loop coming round — has to start over, and that is
        /// the expensive path, so it is the one that gets a step limit.
        /// </summary>
        private void SyncToClock(Entry entry)
        {
            if (Mathf.Approximately(entry.SimulatedTo, _time)) return;

            if (_time > entry.SimulatedTo && entry.SimulatedTo >= 0f)
            {
                entry.Root.Simulate(_time - entry.SimulatedTo, true, false, false);
            }
            else
            {
                entry.Root.Simulate(_time, true, true, _time <= AccurateSeekLimit);
            }

            entry.SimulatedTo = _time;
        }

        private static float Length(ParticleSystem root)
        {
            ParticleSystem.MainModule main = root.main;

            return Mathf.Max(0.1f, main.duration + main.startLifetime.constantMax);
        }

        private void Seek(float time)
        {
            _time = Mathf.Clamp(time, 0f, _length);
            _lastFrame = EditorApplication.timeSinceStartup;
        }

        /// <summary>
        /// Written to be safe to call with the state it is already in, since Cleanup calls it to make
        /// sure the update hook is gone. Guarding on the flag instead would leave the subscription
        /// behind in the one case that matters — paused at teardown, which is most of them.
        /// </summary>
        private void SetPlaying(bool playing)
        {
            _playing = playing;
            _lastFrame = EditorApplication.timeSinceStartup;

            if (_subscribed == playing) return;

            if (playing) EditorApplication.update += OnEditorUpdate;
            else EditorApplication.update -= OnEditorUpdate;

            _subscribed = playing;
        }

        /// <summary>
        /// Only asks for a repaint; the clock and the simulation are driven from the draw, where a
        /// frame that is never shown is never simulated.
        /// </summary>
        private void OnEditorUpdate()
        {
            double now = EditorApplication.timeSinceStartup;
            if (now - _lastRepaint < FrameInterval) return;

            _lastRepaint = now;

            PreviewStage.RepaintInspectors();
        }

        /// <summary>
        /// Measured from what actually draws — every uGUI graphic carries a CanvasRenderer, and both
        /// it and Canvas are engine modules, so this costs the suite no package reference. Taking every
        /// RectTransform instead frames the screen a full-size root stretches to rather than the button
        /// sitting in the middle of it, which is the whole difference between a useful preview and one
        /// that is always the size of the canvas.
        /// </summary>
        private static Bounds ContentBounds(GameObject root)
        {
            Bounds bounds = default;
            bool any = false;

            foreach (CanvasRenderer renderer in root.GetComponentsInChildren<CanvasRenderer>(false))
            {
                Encapsulate(renderer.transform as RectTransform, ref bounds, ref any);
            }

            // Nothing draws — a layout of empty containers, say. The rects themselves are all there is
            // left to frame on, and an empty preview would say less than their outline does.
            if (!any)
            {
                foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(false))
                {
                    Encapsulate(rect, ref bounds, ref any);
                }
            }

            return any ? bounds : new Bounds(root.transform.position, ReferenceResolution);
        }

        private static void Encapsulate(RectTransform rect, ref Bounds bounds, ref bool any)
        {
            if (!rect) return;

            Rect area = rect.rect;
            if (area.width <= 0f || area.height <= 0f) return;

            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);

            foreach (Vector3 corner in corners)
            {
                if (!any)
                {
                    bounds = new Bounds(corner, Vector3.zero);
                    any = true;
                    continue;
                }

                bounds.Encapsulate(corner);
            }
        }
    }
}
