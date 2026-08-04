using UnityEngine;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// Displays FPS counter in screen overlay without requiring UI setup
    /// </summary>
    [AddComponentMenu("DNExtensions/FPS Counter", -1000)]
    public class FPSCounter : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float updateInterval = 0.5f;
        [SerializeField] private int targetFPS = 60;
        [SerializeField] private TextAnchor anchor = TextAnchor.UpperLeft;
        
        [Header("Style")]
        [SerializeField] private int fontSize = 13;
        [SerializeField] private string prefix = "FPS:";

        
        private float _elapsed;
        private int _frames;
        private float _fps;
        private GUIStyle _style;

        private void Awake()
        {
            _style = new GUIStyle
            {
                fontSize = fontSize,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }
        
        private void Update()
        {
            _elapsed += Time.deltaTime;
            _frames++;
            
            if (_elapsed >= updateInterval)
            {
                _fps = _frames / _elapsed;
                _elapsed = 0;
                _frames = 0;
            }
        }
        
        private void OnGUI()
        {
            _style.fontSize = fontSize;

            var percent10 = targetFPS * 0.1f;
            var percent40 = targetFPS * 0.4f;

            var fpsColor = _fps >= targetFPS - percent10
                ? new Color(0.4f, 0.8f, 0.4f)
                : _fps >= targetFPS - percent40
                    ? new Color(0.9f, 0.9f, 0.3f)
                    : new Color(0.9f, 0.4f, 0.4f);

            var text = string.IsNullOrEmpty(prefix) ? $"{_fps:F0}" : $"{prefix} {_fps:F0}";
            var size = _style.CalcSize(new GUIContent(text));
            var rect = GetAnchoredRect(size);

            _style.normal.textColor = Color.black;
            GUI.Label(new Rect(rect.x - 1, rect.y - 1, size.x, size.y), text, _style);
            GUI.Label(new Rect(rect.x + 1, rect.y - 1, size.x, size.y), text, _style);
            GUI.Label(new Rect(rect.x - 1, rect.y + 1, size.x, size.y), text, _style);
            GUI.Label(new Rect(rect.x + 1, rect.y + 1, size.x, size.y), text, _style);

            _style.normal.textColor = fpsColor;
            GUI.Label(rect, text, _style);
        }
        
        private Rect GetAnchoredRect(Vector2 size)
        {
            const float padding = 10f;
            
            return anchor switch
            {
                TextAnchor.UpperLeft => new Rect(padding, padding, size.x, size.y),
                TextAnchor.UpperRight => new Rect(Screen.width - size.x - padding, padding, size.x, size.y),
                TextAnchor.LowerLeft => new Rect(padding, Screen.height - size.y - padding, size.x, size.y),
                TextAnchor.LowerRight => new Rect(Screen.width - size.x - padding, Screen.height - size.y - padding, size.x, size.y),
                _ => new Rect(padding, padding, size.x, size.y)
            };
        }
    }
}