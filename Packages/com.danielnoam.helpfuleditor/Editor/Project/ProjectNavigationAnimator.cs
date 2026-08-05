using UnityEditor;
using UnityEngine;

namespace DNExtensions.HelpfulEditor.Project
{
    /// <summary>
    /// Smooths out the two moments the Project window jumps on its own: arriving somewhere via
    /// back/forward, and collapsing the tree. Navigation also flashes the row it landed on, which is
    /// what actually answers "where did that put me" — a folder several screens away otherwise
    /// arrives with nothing to say it moved.
    /// </summary>
    [InitializeOnLoad]
    internal static class ProjectNavigationAnimator
    {
        private const float ScrollSmoothTime = 0.12f;
        private const float ScrollArrivalDistance = 0.5f;
        private const float HighlightSeconds = 0.9f;

        /// <summary>Below this the window has not really moved, and replaying it would only add a wobble.</summary>
        private const float MinimumCapturedJump = 8f;

        private const double MaxTickSeconds = 0.05;
        private const double FallbackTickSeconds = 1.0 / 60.0;

        private static float _targetScroll;
        private static float _scrollVelocity;
        private static bool _scrolling;

        private static string _highlightPath;
        private static float _highlightAmount;

        private static int _captureTicks;
        private static float _captureFrom;

        private static double _lastTick;

        public static string HighlightPath => _highlightAmount > 0f ? _highlightPath : null;

        /// <summary>Eased 0..1, brightest on arrival.</summary>
        public static float HighlightAmount
        {
            get
            {
                float t = Mathf.Clamp01(_highlightAmount);
                return t * t * (3f - 2f * t);
            }
        }

        static ProjectNavigationAnimator()
        {
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
        }

        public static void ScrollTo(float scroll)
        {
            if (!HelpfulEditorSettings.Project.animatedFoldsEnabled) return;
            if (!HelpfulEditorTreeReflection.TryGetProjectScroll(out float current)) return;
            if (Mathf.Abs(current - scroll) < ScrollArrivalDistance) return;

            _targetScroll = scroll;
            _scrollVelocity = 0f;
            _scrolling = true;
        }

        /// <summary>
        /// Call immediately before something moves the window. Where it ends up is read back a
        /// couple of ticks later and replayed as a glide from where it was, because the editor gives
        /// no way to ask for the destination in advance.
        /// </summary>
        public static void CaptureJump(string highlightPath)
        {
            _highlightPath = highlightPath;
            _highlightAmount = string.IsNullOrEmpty(highlightPath) ? 0f : 1f;

            _scrolling = false;

            if (!HelpfulEditorSettings.Project.animatedFoldsEnabled) return;
            if (!HelpfulEditorTreeReflection.TryGetProjectScroll(out _captureFrom)) return;

            // Two ticks, so the window has certainly drawn once and settled wherever it is going.
            _captureTicks = 2;
        }

        private static void OnUpdate()
        {
            float deltaTime = TickDeltaTime();

            bool active = _captureTicks > 0 || _scrolling || _highlightAmount > 0f;
            if (!active) return;

            ResolveCapture();
            UpdateScroll(deltaTime);
            UpdateHighlight(deltaTime);

            EditorApplication.RepaintProjectWindow();
        }

        private static void ResolveCapture()
        {
            if (_captureTicks <= 0) return;

            _captureTicks--;
            if (_captureTicks > 0) return;

            // The tree animates its own framing in some situations. That is already the smooth
            // motion this would provide, so it is left alone rather than fought over.
            if (HelpfulEditorTreeReflection.IsProjectTreeFraming()) return;

            if (!HelpfulEditorTreeReflection.TryGetProjectScroll(out float landed)) return;
            if (Mathf.Abs(landed - _captureFrom) < MinimumCapturedJump) return;

            if (!HelpfulEditorTreeReflection.SetProjectScroll(_captureFrom)) return;

            _targetScroll = landed;
            _scrollVelocity = 0f;
            _scrolling = true;
        }

        private static void UpdateScroll(float deltaTime)
        {
            if (!_scrolling) return;

            if (!HelpfulEditorTreeReflection.TryGetProjectScroll(out float current))
            {
                _scrolling = false;
                return;
            }

            float next = Mathf.SmoothDamp(current, _targetScroll, ref _scrollVelocity, ScrollSmoothTime, Mathf.Infinity, deltaTime);

            if (Mathf.Abs(next - _targetScroll) < ScrollArrivalDistance)
            {
                next = _targetScroll;
                _scrolling = false;
            }

            if (!HelpfulEditorTreeReflection.SetProjectScroll(next)) _scrolling = false;
        }

        private static void UpdateHighlight(float deltaTime)
        {
            if (_highlightAmount <= 0f) return;

            _highlightAmount -= deltaTime / HighlightSeconds;

            if (_highlightAmount > 0f) return;

            _highlightAmount = 0f;
            _highlightPath = null;
        }

        /// <summary>
        /// Capped rather than measured raw: the editor's update loop stalls for whole seconds during
        /// imports and compiles, and one of those frames would otherwise finish every animation in a
        /// single step.
        /// </summary>
        private static float TickDeltaTime()
        {
            double now = EditorApplication.timeSinceStartup;
            double elapsed = now - _lastTick;
            _lastTick = now;

            if (elapsed <= 0.0 || elapsed > MaxTickSeconds) elapsed = FallbackTickSeconds;

            return (float)elapsed;
        }
    }
}
