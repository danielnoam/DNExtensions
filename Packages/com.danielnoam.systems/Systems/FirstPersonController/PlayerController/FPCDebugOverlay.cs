using DNExtensions.Utilities;
using DNExtensions.Utilities.AutoGet;
using UnityEngine;

namespace DNExtensions.Systems.FirstPersonController
{
    /// <summary>
    /// On screen readout of what the player controller is doing: velocity and the locomotion state flags.
    /// Add it to the FPC and switch it on when something needs watching, including while in play mode.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(FpcManager))]
    [AddComponentMenu("DNExtensions/FPC Debug Overlay")]
    public class FPCDebugOverlay : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Draws the overlay. Can be toggled while playing")]
        [SerializeField] private bool showOverlay = true;
        [SerializeField, ShowIf(nameof(showOverlay))] private TextAnchor anchor = TextAnchor.UpperLeft;

        [SerializeField, AutoGetSelf, HideInInspector] private FpcManager manager;

        private GUIStyle _labelStyle;
        private GUIStyle _valueStyle;
        private GUIStyle _headerStyle;
        private Texture2D _panelTex;
        private GUIStyle _panelStyle;

        // Wide enough for the longest row, which is the three momentum state flags side by side —
        // DrawFlags drops any flag that would not fit rather than running past the panel edge.
        private const float PanelWidth = 320f;
        private const float Padding = 10f;
        private const float RowHeight = 18f;
        private const float HeaderHeight = 22f;
        private const float LabelWidth = 78f;
        private const int RowCount = 5;
        private const int LabelFontSize = 12;
        private const int HeaderFontSize = 13;

        private static readonly Color PanelColor = new Color(0.08f, 0.08f, 0.08f, 0.85f);
        private static readonly Color OnColor = new Color(0.45f, 0.9f, 0.5f);
        private static readonly Color OffColor = new Color(0.55f, 0.55f, 0.55f);

        private void OnValidate()
        {
            AutoGetSystem.Process(this);
        }

        private void OnGUI()
        {
            if (!showOverlay || !manager || !manager.FpcLocomotion) return;

            EnsureStyles();

            FPCLocomotionBase locomotion = manager.FpcLocomotion;
            Vector3 velocity = locomotion.Velocity;

            Rect panel = GetAnchoredRect(new Vector2(PanelWidth, HeaderHeight + RowHeight * RowCount + Padding * 2f));
            GUI.Box(panel, GUIContent.none, _panelStyle);

            float x = panel.x + Padding;
            float y = panel.y + Padding;
            float width = PanelWidth - Padding * 2f;

            GUI.Label(new Rect(x, y, width, HeaderHeight), $"FPC  ·  {manager.CurrentLocomotionMode}", _headerStyle);
            y += HeaderHeight;

            y = DrawRow(x, y, width, "Speed", $"{locomotion.HorizontalSpeed:F2} m/s");
            y = DrawRow(x, y, width, "Velocity", $"{velocity.x:F2}, {velocity.y:F2}, {velocity.z:F2}");
            y = DrawRow(x, y, width, "Vertical", $"{velocity.y:F2} m/s");

            // Grounded and falling are one line because they answer the same question, and the three
            // input driven states another, so a glance lands on whichever half is in question.
            y = DrawFlags(x, y, width, "Ground",
                ("Grounded", locomotion.IsGrounded),
                ("Falling", locomotion.IsFalling));

            // Sliding only exists on the momentum controller, and it is a crouch while it lasts — so
            // without it a slide and a plain crouch are the same two lit flags. Left off entirely on
            // the standard controller rather than shown permanently dark, where it would read as a
            // state that never happens instead of one that does not apply.
            if (locomotion is FPCMomentumLocomotion momentum)
            {
                DrawFlags(x, y, width, "State",
                    ("Running", locomotion.IsRunning),
                    ("Crouching", locomotion.IsCrouching),
                    ("Sliding", momentum.IsSliding));

                return;
            }

            DrawFlags(x, y, width, "State",
                ("Running", locomotion.IsRunning),
                ("Crouching", locomotion.IsCrouching));
        }

        private float DrawRow(float x, float y, float width, string label, string value)
        {
            GUI.Label(new Rect(x, y, LabelWidth, RowHeight), label, _labelStyle);
            GUI.Label(new Rect(x + LabelWidth, y, width - LabelWidth, RowHeight), value, _valueStyle);

            return y + RowHeight;
        }

        /// <summary>
        /// A row of state names, lit for the ones that are currently true. Every name is drawn whether
        /// or not it is on, so the row keeps its shape and a flag turning off reads as a change of
        /// colour rather than as text appearing and disappearing.
        /// </summary>
        private float DrawFlags(float x, float y, float width, string label, params (string Name, bool On)[] flags)
        {
            GUI.Label(new Rect(x, y, LabelWidth, RowHeight), label, _labelStyle);

            float flagX = x + LabelWidth;
            Color previous = GUI.color;

            foreach ((string name, bool on) in flags)
            {
                float flagWidth = _valueStyle.CalcSize(new GUIContent(name)).x + 8f;
                if (flagX + flagWidth > x + width) break;

                GUI.color = on ? OnColor : OffColor;
                GUI.Label(new Rect(flagX, y, flagWidth, RowHeight), name, _valueStyle);

                flagX += flagWidth;
            }

            GUI.color = previous;

            return y + RowHeight;
        }

        private void EnsureStyles()
        {
            _labelStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = LabelFontSize,
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
            };

            _valueStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = LabelFontSize,
                normal = { textColor = Color.white }
            };

            _headerStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = HeaderFontSize,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            if (!_panelTex)
            {
                _panelTex = new Texture2D(1, 1);
                _panelTex.SetPixel(0, 0, PanelColor);
                _panelTex.Apply();

                // Not saved with the scene and cleaned up with the component, since nothing else can
                // reach a texture built in OnGUI.
                _panelTex.hideFlags = HideFlags.HideAndDontSave;
                _panelStyle = null;
            }

            _panelStyle ??= new GUIStyle(GUI.skin.box)
            {
                normal = { background = _panelTex }
            };
        }

        private void OnDestroy()
        {
            if (_panelTex) DestroyImmediate(_panelTex);
        }

        private Rect GetAnchoredRect(Vector2 size)
        {
            return anchor switch
            {
                TextAnchor.UpperRight => new Rect(Screen.width - size.x - Padding, Padding, size.x, size.y),
                TextAnchor.LowerLeft => new Rect(Padding, Screen.height - size.y - Padding, size.x, size.y),
                TextAnchor.LowerRight => new Rect(Screen.width - size.x - Padding, Screen.height - size.y - Padding, size.x, size.y),
                _ => new Rect(Padding, Padding, size.x, size.y)
            };
        }
    }
}
