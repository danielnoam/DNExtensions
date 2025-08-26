

#if UNITY_EDITOR
using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEditor;
using System.Reflection;


namespace DNExtensions.Button
{
  

    public abstract class BaseButtonAttributeEditor : UnityEditor.Editor
    {
        private readonly Dictionary<string, object[]> _methodParameters = new Dictionary<string, object[]>();
        private readonly Dictionary<string, bool> _foldoutStates = new Dictionary<string, bool>();
        
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            DrawButtonsForTarget();
        }
        
        private void DrawButtonsForTarget()
        {
            MethodInfo[] methods = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            
            foreach (MethodInfo method in methods)
            {
                ButtonAttribute buttonAttr = method.GetCustomAttribute<ButtonAttribute>();
                if (buttonAttr != null)
                {
                    DrawButton(method, buttonAttr);
                }
            }
        }
        
        private void DrawButton(MethodInfo method, ButtonAttribute buttonAttr)
        {
            if (buttonAttr.Space > 0)
            {
                GUILayout.Space(buttonAttr.Space);
            }
            

            string buttonText = string.IsNullOrEmpty(buttonAttr.Name) 
                ? ObjectNames.NicifyVariableName(method.Name) 
                : buttonAttr.Name;
            
            bool shouldDisable;
            string playModeText = "";
            
            switch (buttonAttr.PlayMode)
            {
                case ButtonPlayMode.OnlyWhenPlaying:
                    shouldDisable = !Application.isPlaying;
                    if (shouldDisable) playModeText = "\n(Play Mode Only)";
                    break;
                case ButtonPlayMode.OnlyWhenNotPlaying:
                    shouldDisable = Application.isPlaying;
                    if (shouldDisable) playModeText = "\n(Edit Mode Only)";
                    break;
                case ButtonPlayMode.Both:
                default:
                    shouldDisable = false;
                    break;
            }
            
            if (shouldDisable)
            {
                buttonText += playModeText;
            }
            
            ParameterInfo[] parameters = method.GetParameters();
            string methodKey = target.GetInstanceID() + "_" + method.Name;
            
            if (!_methodParameters.ContainsKey(methodKey))
            {
                _methodParameters[methodKey] = new object[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    _methodParameters[methodKey][i] = GetMethodParameterDefaultValue(parameters[i]);
                }
            }
            
            _foldoutStates.TryAdd(methodKey, false);
            Color originalColor = GUI.backgroundColor;
            bool originalEnabled = GUI.enabled;
            
            if (shouldDisable)
            {
                GUI.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.8f); // Dark gray
                GUI.enabled = false;
            }
            else
            {
                GUI.backgroundColor = buttonAttr.Color;
            }
            

            bool buttonClicked;
            
            if (parameters.Length > 0)
            {
 
                EditorGUILayout.BeginHorizontal();
                
                bool newFoldoutState = GUILayout.Toggle(_foldoutStates[methodKey], "", EditorStyles.foldout, GUILayout.Width(15), GUILayout.Height(buttonAttr.Size));
                if (_foldoutStates != null && newFoldoutState != _foldoutStates[methodKey])
                {
                    _foldoutStates[methodKey] = newFoldoutState;
                }
                
                buttonClicked = GUILayout.Button(buttonText, GUILayout.Height(buttonAttr.Size), GUILayout.ExpandWidth(true));
                
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                buttonClicked = GUILayout.Button(buttonText, GUILayout.Height(buttonAttr.Size));
            }
            
            if (buttonClicked && !shouldDisable)
            {
                try
                {
                    method.Invoke(target, _methodParameters[methodKey]);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error invoking method {method.Name}: {e.Message}");
                }
            }
            
            GUI.backgroundColor = originalColor;
            GUI.enabled = originalEnabled;
            
            
            if (parameters.Length > 0 && _foldoutStates[methodKey])
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                
                for (int i = 0; i < parameters.Length; i++)
                {
                    _methodParameters[methodKey][i] = DrawParameterField(
                        parameters[i].Name, 
                        parameters[i].ParameterType, 
                        _methodParameters[methodKey][i]
                    );
                }
                
                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel--;
            }
        }
        
        private object DrawParameterField(string paramName, Type paramType, object currentValue)
        {
            string niceName = ObjectNames.NicifyVariableName(paramName);
            
            if (paramType == typeof(int))
            {
                return EditorGUILayout.IntField(niceName, currentValue != null ? (int)currentValue : 0);
            }
            else if (paramType == typeof(float))
            {
                return EditorGUILayout.FloatField(niceName, currentValue != null ? (float)currentValue : 0f);
            }
            else if (paramType == typeof(string))
            {
                return EditorGUILayout.TextField(niceName, currentValue != null ? (string)currentValue : "");
            }
            else if (paramType == typeof(bool))
            {
                return EditorGUILayout.Toggle(niceName, currentValue != null && (bool)currentValue);
            }
            else if (paramType == typeof(Vector2))
            {
                return EditorGUILayout.Vector2Field(niceName, currentValue != null ? (Vector2)currentValue : Vector2.zero);
            }
            else if (paramType == typeof(Vector3))
            {
                return EditorGUILayout.Vector3Field(niceName, currentValue != null ? (Vector3)currentValue : Vector3.zero);
            }
            else if (paramType == typeof(Color))
            {
                return EditorGUILayout.ColorField(niceName, currentValue != null ? (Color)currentValue : Color.white);
            }
            else if (paramType.IsEnum)
            {
                return EditorGUILayout.EnumPopup(niceName, currentValue != null ? (Enum)currentValue : (Enum)Enum.GetValues(paramType).GetValue(0));
            }
            else if (typeof(UnityEngine.Object).IsAssignableFrom(paramType))
            {
                return EditorGUILayout.ObjectField(niceName, (UnityEngine.Object)currentValue, paramType, true);
            }
            else
            {
                EditorGUILayout.LabelField(niceName, $"Unsupported type: {paramType.Name}");
                return currentValue;
            }
        }
        
        /// <summary>
        /// Gets the default value for a method parameter, using the method's default value if available
        /// </summary>
        private object GetMethodParameterDefaultValue(ParameterInfo parameter)
        {
            return parameter.HasDefaultValue 
                ? parameter.DefaultValue
                : GetTypeDefaultValue(parameter.ParameterType);
        }
        
        /// <summary>
        /// Gets the default value for a type
        /// </summary>
        private object GetTypeDefaultValue(Type type)
        {
            if (type == typeof(string)) return "";
            if (type == typeof(int)) return 0;
            if (type == typeof(float)) return 0f;
            if (type == typeof(bool)) return false;
            if (type == typeof(Vector2)) return Vector2.zero;
            if (type == typeof(Vector3)) return Vector3.zero;
            if (type == typeof(Color)) return Color.white;
            if (type.IsEnum) return Enum.GetValues(type).GetValue(0);
            if (typeof(UnityEngine.Object).IsAssignableFrom(type)) return null;
            
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }
    }
    

    [CustomEditor(typeof(MonoBehaviour), true)]
    public class ButtonAttributeEditor : BaseButtonAttributeEditor
    {

    }
    

    [CustomEditor(typeof(ScriptableObject), true)]
    public class ButtonAttributeScriptableObjectEditor : BaseButtonAttributeEditor
    {

    }
}

#endif