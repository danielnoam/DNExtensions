using UnityEditor;
using UnityEngine;

namespace DNExtensions.Utilities.TubeRenderer
{
    [CustomEditor(typeof(TubeRenderer))]
    public class TubeRendererEditor : UnityEditor.Editor
    {
        private SerializedProperty _sides;
        private SerializedProperty _closeStartCap;
        private SerializedProperty _closeEndCap;
        private SerializedProperty _radiusMode;
        private SerializedProperty _radiusOne;
        private SerializedProperty _radiusTwo;
        private SerializedProperty _radiusCurve;
        private SerializedProperty _enableCornerSmoothing;
        private SerializedProperty _sharpAngleThreshold;
        private SerializedProperty _cornerSmoothingSegments;
        private SerializedProperty _cornerSmoothingExtent;
        private SerializedProperty _useStableUpVector;
        private SerializedProperty _upVector;
        private SerializedProperty _positions;

        private void OnEnable()
        {
            _sides = serializedObject.FindProperty("sides");
            _closeStartCap = serializedObject.FindProperty("closeStartCap");
            _closeEndCap = serializedObject.FindProperty("closeEndCap");
            _radiusMode = serializedObject.FindProperty("radiusMode");
            _radiusOne = serializedObject.FindProperty("radiusOne");
            _radiusTwo = serializedObject.FindProperty("radiusTwo");
            _radiusCurve = serializedObject.FindProperty("radiusCurve");
            _enableCornerSmoothing = serializedObject.FindProperty("enableCornerSmoothing");
            _sharpAngleThreshold = serializedObject.FindProperty("sharpAngleThreshold");
            _cornerSmoothingSegments = serializedObject.FindProperty("cornerSmoothingSegments");
            _cornerSmoothingExtent = serializedObject.FindProperty("cornerSmoothingExtent");
            _useStableUpVector = serializedObject.FindProperty("useStableUpVector");
            _upVector = serializedObject.FindProperty("upVector");
            _positions = serializedObject.FindProperty("positions");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Tube Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_sides);
            EditorGUILayout.PropertyField(_closeStartCap);
            EditorGUILayout.PropertyField(_closeEndCap);
            EditorGUILayout.PropertyField(_radiusMode);

            RadiusMode mode = (RadiusMode)_radiusMode.enumValueIndex;
            
            if (mode == RadiusMode.Single)
            {
                EditorGUILayout.PropertyField(_radiusOne, new GUIContent("Radius"));
            }
            else if (mode == RadiusMode.StartEnd)
            {
                EditorGUILayout.PropertyField(_radiusOne, new GUIContent("Start Radius"));
                EditorGUILayout.PropertyField(_radiusTwo, new GUIContent("End Radius"));
            }
            else if (mode == RadiusMode.Curve)
            {
                EditorGUILayout.PropertyField(_radiusCurve, new GUIContent("Radius Curve"));
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Corner Smoothing", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_enableCornerSmoothing);

            if (_enableCornerSmoothing.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Slider(_sharpAngleThreshold, 20f, 160f, new GUIContent("Sharp Angle Threshold"));
                EditorGUILayout.IntSlider(_cornerSmoothingSegments, 1, 8, new GUIContent("Smoothing Segments"));
                EditorGUILayout.Slider(_cornerSmoothingExtent, 0.1f, 0.5f, new GUIContent("Smoothing Extent"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Orientation", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_useStableUpVector);

            if (_useStableUpVector.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_upVector);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Path", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_positions);

            serializedObject.ApplyModifiedProperties();
        }
    }
}