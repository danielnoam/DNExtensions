using UnityEngine;

namespace DNExtensions
{
    /// <summary>
    /// Displays FPS counter in screen overlay without requiring UI setup
    /// </summary>
    public class FPSCounter : MonoBehaviour
    {
        [SerializeField] private bool editorOnly;
        [SerializeField] private float updateInterval = 0.5f;
        [SerializeField] private int targetFPS = 60;
        [SerializeField] private TextAnchor anchor = TextAnchor.UpperLeft;
        
        private float _elapsed;
        private int _frames;
        private float _fps;
        private GUIStyle _style;
        
        private void Awake()
        {
            if (editorOnly && !Application.isEditor)
            {
                enabled = false;
                return;
            }
            
            _style = new GUIStyle
            {
                fontSize = 20,
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
            var percent10 = targetFPS * 0.1f;
            var percent40 = targetFPS * 0.4f;
            
            _style.normal.textColor = _fps >= targetFPS - percent10 
                ? new Color(0.4f, 0.8f, 0.4f) 
                : _fps >= targetFPS - percent40 
                    ? new Color(0.9f, 0.9f, 0.3f) 
                    : new Color(0.9f, 0.4f, 0.4f);
            
            var text = $"FPS: {_fps:F0}";
            var size = _style.CalcSize(new GUIContent(text));
            var rect = GetAnchoredRect(size);
            
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