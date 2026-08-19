using UnityEngine;

namespace DNExtensions.HelpfulEditor.Inspector
{
    /// <summary>
    /// Orbit, pan and zoom for a preview camera, and the mouse handling that drives them. Shared by
    /// the two previews, which want the same gestures over cameras of opposite kinds — the framing is
    /// the only part that differs, and it differs entirely, hence the two Apply methods rather than
    /// one with a flag.
    ///
    /// The state is per preview rather than per target on purpose: dragging in one cell of a multiple
    /// selection turns every cell, which is the point of putting them side by side.
    /// </summary>
    internal sealed class PreviewViewControls
    {
        private const float ZoomStep = 0.05f;
        private const float MinZoom = 0.15f;
        private const float MaxZoom = 8f;

        private readonly Vector2 _defaultAngles;

        private int _dragButton = -1;

        /// <summary>Yaw in x, pitch in y, both in degrees.</summary>
        private Vector2 _angles;

        /// <summary>Fractions of the framed size, so panning feels the same whatever is being shown.</summary>
        private Vector2 _pan;

        private float _zoom = 1f;

        public PreviewViewControls(Vector2 defaultAngles)
        {
            _defaultAngles = defaultAngles;
            _angles = defaultAngles;
        }

        /// <summary>Whether the view has been moved off its default, which is when offering a reset means anything.</summary>
        public bool Moved => _angles != _defaultAngles || _pan != Vector2.zero || !Mathf.Approximately(_zoom, 1f);

        public void Reset()
        {
            _angles = _defaultAngles;
            _pan = Vector2.zero;
            _zoom = 1f;
        }

        /// <summary>
        /// Left drag orbits, middle drag or held Alt pans, the wheel zooms. Called with the rect the
        /// preview occupies, which for a multiple selection is one cell of the grid.
        /// </summary>
        public void Handle(Rect rect)
        {
            int id = GUIUtility.GetControlID(FocusType.Passive);
            Event evt = Event.current;

            switch (evt.GetTypeForControl(id))
            {
                case EventType.MouseDown when rect.Contains(evt.mousePosition) && (evt.button == 0 || evt.button == 2):
                    _dragButton = evt.button;
                    GUIUtility.hotControl = id;
                    evt.Use();
                    break;

                case EventType.MouseDrag when GUIUtility.hotControl == id:
                    if (_dragButton == 2 || evt.alt) Pan(evt.delta, rect);
                    else Orbit(evt.delta);

                    evt.Use();
                    break;

                case EventType.MouseUp when GUIUtility.hotControl == id:
                    GUIUtility.hotControl = 0;
                    _dragButton = -1;
                    evt.Use();
                    break;

                case EventType.ScrollWheel when rect.Contains(evt.mousePosition):
                    _zoom = Mathf.Clamp(_zoom * (1f + evt.delta.y * ZoomStep), MinZoom, MaxZoom);
                    evt.Use();
                    break;
            }
        }

        private void Orbit(Vector2 delta)
        {
            _angles.x += delta.x;

            // Stopped short of the poles, where the camera's up vector flips and the view rolls over.
            _angles.y = Mathf.Clamp(_angles.y + delta.y, -89f, 89f);
        }

        /// <summary>
        /// The camera moves against the drag, so the content goes with it. Vertically that means with
        /// the delta rather than against it, since GUI coordinates count downwards.
        /// </summary>
        private void Pan(Vector2 delta, Rect rect)
        {
            if (rect.width <= 0f || rect.height <= 0f) return;

            _pan.x -= delta.x / rect.width * 2f;
            _pan.y += delta.y / rect.height * 2f;
        }

        /// <summary>Flat content seen square-on, until it is orbited off that.</summary>
        public void ApplyOrthographic(Camera camera, Bounds bounds, float aspect, float padding)
        {
            camera.orthographic = true;

            float half = Mathf.Max(bounds.extents.y, bounds.extents.x / Mathf.Max(0.0001f, aspect));
            half = Mathf.Max(0.01f, half * padding * _zoom);

            camera.orthographicSize = half;

            Quaternion rotation = Quaternion.Euler(_angles.y, _angles.x, 0f);

            // Far enough out that the content stays past the near plane at any orbit angle. An
            // orthographic camera takes nothing from the distance but that.
            float distance = bounds.extents.magnitude + half * 4f + 1f;

            Vector3 focus = bounds.center + rotation * new Vector3(_pan.x * half * aspect, _pan.y * half, 0f);

            camera.transform.rotation = rotation;
            camera.transform.position = focus - rotation * Vector3.forward * distance;

            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = distance * 3f;
        }

        /// <summary>Something with depth, held at the distance that just fits it in the field of view.</summary>
        public void ApplyPerspective(Camera camera, Bounds bounds)
        {
            camera.orthographic = false;

            float radius = Mathf.Max(bounds.extents.magnitude, 0.05f);
            float distance = radius / Mathf.Sin(Mathf.Deg2Rad * camera.fieldOfView * 0.5f) * _zoom;

            Quaternion rotation = Quaternion.Euler(_angles.y, _angles.x, 0f);

            Vector3 focus = bounds.center + rotation * new Vector3(_pan.x * radius, _pan.y * radius, 0f);

            camera.transform.rotation = rotation;
            camera.transform.position = focus - rotation * Vector3.forward * distance;

            camera.nearClipPlane = Mathf.Max(0.01f, distance * 0.01f);
            camera.farClipPlane = distance * 10f;
        }
    }
}
