using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DNExtensions.Utilities
{
    [AddComponentMenu("DNExtensions/Debug Overlay")]
    public class DebugOverlay : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool enabledOnStart = true;
        [SerializeField] private bool showTimestamp = true;
        [SerializeField] private int maxLines = 20;
        [SerializeField] private Key toggleKey = Key.F1;
        [SerializeField] private GUIStyle labelStyle;
        [SerializeField] private FPSCounter fpsCounter;

        private readonly Queue<string> _lines = new();
        private readonly StringBuilder _sb = new();
        private string _log = "";
        private bool _dirty;
        private bool _visible;

        private void Start() => _visible = enabledOnStart;

        private void OnEnable() => Application.logMessageReceived += HandleLog;
        private void OnDisable() => Application.logMessageReceived -= HandleLog;

        private void Update()
        {
            if (Keyboard.current[toggleKey].wasPressedThisFrame)
            {
                _visible = !_visible;
                if (fpsCounter) fpsCounter.enabled = _visible;
                if (_visible) Debug.Log("Debug overlay enabled");
            }
        }

        private void HandleLog(string message, string stackTrace, LogType type)
        {
            string timestamp = showTimestamp ? $"[{DateTime.Now:HH:mm:ss}] " : "";
            _lines.Enqueue($"{timestamp}[{type}] {message}");

            if (_lines.Count > maxLines) _lines.Dequeue();

            _dirty = true;
        }

        private void RebuildLog()
        {
            _sb.Clear();
            foreach (string line in _lines)
            {
                _sb.Append(line);
                _sb.Append('\n');
            }
            _log = _sb.ToString();
            _dirty = false;
        }

        private void OnGUI()
        {
            if (!_visible) return;
            if (_dirty) RebuildLog();
            GUI.Label(new Rect(10, 10, Screen.width - 20, Screen.height - 20), _log, labelStyle);
        }
    }
}
