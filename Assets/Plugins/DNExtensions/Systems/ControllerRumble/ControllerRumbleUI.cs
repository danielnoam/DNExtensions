using UnityEngine;

namespace DNExtensions.Systems.ControllerRumble
{
    /// <summary>
    /// Displays rumble intensity overlay.
    /// </summary>
    [AddComponentMenu("DNExtensions/Controller Rumble/Controller Rumble UI")]
    [DisallowMultipleComponent]
    public class ControllerRumbleUI : MonoBehaviour
    {
        [SerializeField] private ControllerRumbleListener listener;
        [SerializeField] private bool editorOnly;
        [SerializeField] private float intensityThreshold = 0.01f;
        [SerializeField] private float shakeSpeed = 35f;
        [SerializeField] private float maxShakeOffset = 10f;
        [SerializeField] private TextAnchor anchor = TextAnchor.LowerRight;

        private GUIStyle _labelStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _iconStyle;
        private GUIStyle _barBackgroundStyle;
        private Texture2D _lowBarTex;
        private Texture2D _highBarTex;
        private Texture2D _barBgTex;

        private float _noiseTimer;
        private Vector2 _shakeOffset;

        private const float PanelWidth = 320f;
        private const float PanelHeight = 100f;
        private const float IconSize = PanelHeight;
        private const float IconGap = 6f;
        private const float Padding = 10f;
        private const float BarHeight = 24f;
        private const float LabelWidth = 16f;
        private const float ValueWidth = 36f;
        private const float HeaderHeight = 24f;
        private const int LabelFontSize = 13;
        private const int HeaderFontSize = 14;
        private const int IconFontSize = 65;
        private static readonly Color LowBarColor = new Color(0.3f, 0.6f, 1f);
        private static readonly Color HighBarColor = new Color(1f, 0.5f, 0.2f);
        private static readonly Color BarBgColor = new Color(0.15f, 0.15f, 0.15f, 0.9f);

        private void Awake()
        {
            if (editorOnly && !Application.isEditor)
            {
                enabled = false;
                return;
            }

            if (!listener) listener = FindFirstObjectByType<ControllerRumbleListener>();
        }

        private void Update()
        {
            if (!listener) return;

            float combinedIntensity = Mathf.Max(listener.CurrentCombinedLow, listener.CurrentCombinedHigh);

            if (combinedIntensity > intensityThreshold)
            {
                _noiseTimer += Time.deltaTime * shakeSpeed;
                _shakeOffset = new Vector2(
                    (Mathf.PerlinNoise(_noiseTimer, 0f) - 0.5f) * 2f * combinedIntensity * maxShakeOffset,
                    (Mathf.PerlinNoise(0f, _noiseTimer) - 0.5f) * 2f * combinedIntensity * maxShakeOffset
                );
            }
            else
            {
                _shakeOffset = Vector2.Lerp(_shakeOffset, Vector2.zero, Time.deltaTime * shakeSpeed);
            }
        }

        private void OnGUI()
        {
            if (!listener) return;

            _labelStyle ??= new GUIStyle(GUI.skin.label)
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

            _iconStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = IconFontSize,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            _barBgTex ??= MakeTex(1, 1, BarBgColor);
            _lowBarTex ??= MakeTex(1, 1, LowBarColor);
            _highBarTex ??= MakeTex(1, 1, HighBarColor);

            _barBackgroundStyle ??= new GUIStyle(GUI.skin.box)
            {
                normal = { background = _barBgTex }
            };

            var panelRect = GetAnchoredRect(new Vector2(PanelWidth, PanelHeight));
            var iconRect = GetIconRect(panelRect);

            // Icon shakes, panel stays still
            GUI.Label(
                new Rect(iconRect.x + _shakeOffset.x, iconRect.y + _shakeOffset.y, iconRect.width, iconRect.height),
                "🎮", _iconStyle
            );

            GUI.Box(panelRect, GUIContent.none);

            float x = panelRect.x + Padding;
            float y = panelRect.y + Padding;
            float barWidth = PanelWidth - Padding * 2f;

            GUI.Label(new Rect(x, y, barWidth, HeaderHeight), $"Rumble  ·  {listener.ActiveEffects} effects", _headerStyle);
            y += HeaderHeight;

            DrawBar(x, y, barWidth, listener.CurrentCombinedLow, _lowBarTex, "L");
            y += BarHeight + 6f;

            DrawBar(x, y, barWidth, listener.CurrentCombinedHigh, _highBarTex, "H");
        }

        private Rect GetIconRect(Rect panelRect)
        {
            return anchor switch
            {
                TextAnchor.UpperLeft  => new Rect(panelRect.xMax + IconGap, panelRect.y, IconSize, IconSize),
                TextAnchor.UpperRight => new Rect(panelRect.x - IconSize - IconGap, panelRect.y, IconSize, IconSize),
                TextAnchor.LowerLeft  => new Rect(panelRect.xMax + IconGap, panelRect.y, IconSize, IconSize),
                TextAnchor.LowerRight => new Rect(panelRect.x - IconSize - IconGap, panelRect.y, IconSize, IconSize),
                _ => new Rect(panelRect.xMax + IconGap, panelRect.y, IconSize, IconSize)
            };
        }

        private void DrawBar(float x, float y, float totalWidth, float value, Texture2D fillTex, string label)
        {
            float barX = x + LabelWidth + 4f;
            float barWidth = totalWidth - LabelWidth - ValueWidth - 8f;

            GUI.Label(new Rect(x, y, LabelWidth, BarHeight), label, _labelStyle);
            GUI.Box(new Rect(barX, y, barWidth, BarHeight), GUIContent.none, _barBackgroundStyle);

            if (value > 0f)
            {
                var fillStyle = new GUIStyle(GUI.skin.box) { normal = { background = fillTex } };
                GUI.Box(new Rect(barX, y, barWidth * value, BarHeight), GUIContent.none, fillStyle);
            }

            GUI.Label(new Rect(barX + barWidth + 4f, y, ValueWidth, BarHeight), $"{value:F2}", _labelStyle);
        }

        private Rect GetAnchoredRect(Vector2 size)
        {
            return anchor switch
            {
                TextAnchor.UpperLeft  => new Rect(Padding, Padding, size.x, size.y),
                TextAnchor.UpperRight => new Rect(Screen.width - size.x - Padding, Padding, size.x, size.y),
                TextAnchor.LowerLeft  => new Rect(Padding, Screen.height - size.y - Padding, size.x, size.y),
                TextAnchor.LowerRight => new Rect(Screen.width - size.x - Padding, Screen.height - size.y - Padding, size.x, size.y),
                _ => new Rect(Padding, Padding, size.x, size.y)
            };
        }

        private static Texture2D MakeTex(int width, int height, Color color)
        {
            var tex = new Texture2D(width, height);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }
    }
}