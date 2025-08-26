#if UNITY_EDITOR
using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEditor;
using System.Reflection;
using System.Linq;

namespace DNExtensions.Button
{
    /// <summary>
    /// Contains method and attribute information for button drawing.
    /// </summary>
    public struct ButtonInfo
    {
        public readonly MethodInfo Method;
        public readonly ButtonAttribute Attribute;
        
        public ButtonInfo(MethodInfo method, ButtonAttribute attribute)
        {
            Method = method;
            Attribute = attribute;
        }
    }

    /// <summary>
    /// Base editor for drawing buttons from ButtonAttribute-decorated methods.
    /// Supports parameter input, grouping, and play mode restrictions.
    /// </summary>
    public abstract class BaseButtonAttributeEditor : UnityEditor.Editor
    {
        private readonly Dictionary<string, object[]> _methodParameters = new Dictionary<string, object[]>();
        private readonly Dictionary<string, bool> _foldoutStates = new Dictionary<string, bool>();
        private readonly Dictionary<string, bool> _groupFoldoutStates = new Dictionary<string, bool>();
        
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            DrawButtonsForTarget();
        }
        
        /// <summary>
        /// Finds all ButtonAttribute-decorated methods and draws them grouped appropriately.
        /// </summary>
        private void DrawButtonsForTarget()
        {
            MethodInfo[] methods = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            
            // Collect all button methods
            List<ButtonInfo> buttonInfos = new List<ButtonInfo>();
            foreach (MethodInfo method in methods)
            {
                ButtonAttribute buttonAttr = method.GetCustomAttribute<ButtonAttribute>();
                if (buttonAttr != null)
                {
                    buttonInfos.Add(new ButtonInfo(method, buttonAttr));
                }
            }
            
            // Group buttons by their Group property
            var groupedButtons = buttonInfos.GroupBy(b => string.IsNullOrEmpty(b.Attribute.Group) ? "" : b.Attribute.Group)
                                           .OrderBy(g => g.Key);
            
            foreach (var group in groupedButtons)
            {
                if (string.IsNullOrEmpty(group.Key))
                {
                    // Draw ungrouped buttons normally
                    foreach (var buttonInfo in group.OrderBy(b => b.Method.Name))
                    {
                        DrawButton(buttonInfo.Method, buttonInfo.Attribute);
                    }
                }
                else
                {
                    // Draw grouped buttons in a foldout
                    DrawButtonGroup(group.Key, group.ToList());
                }
            }
        }
        
        /// <summary>
        /// Draws a collapsible group of buttons with enhanced foldout interaction and hover effects.
        /// </summary>
        private void DrawButtonGroup(string groupName, List<ButtonInfo> buttons)
        {
            string groupKey = target.GetInstanceID() + "_group_" + groupName;
            
            _groupFoldoutStates.TryAdd(groupKey, true);

            // Groups start expanded by default
            // Draw group header with some spacing
            GUILayout.Space(5);
            
            // Custom group header style
            var groupStyle = new GUIStyle(EditorStyles.foldout)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 12
            };

            // Create a rect for the entire foldout area to handle clicks and hover
            Rect foldoutRect = GUILayoutUtility.GetRect(new GUIContent(groupName), groupStyle);
            
            // Check if mouse is hovering over the foldout area
            bool isHovering = foldoutRect.Contains(Event.current.mousePosition);
            
            // Draw hover background effect
            if (isHovering)
            {
                Color hoverColor = EditorGUIUtility.isProSkin 
                    ? new Color(1f, 1f, 1f, 0.1f)  // Light overlay for dark theme
                    : new Color(0f, 0f, 0f, 0.05f); // Dark overlay for light theme
                
                EditorGUI.DrawRect(foldoutRect, hoverColor);
                
                // Change cursor to pointer when hovering
                EditorGUIUtility.AddCursorRect(foldoutRect, MouseCursor.Link);
            }
            
            // Handle mouse clicks on the entire foldout area (both arrow and text)
            if (Event.current.type == EventType.MouseDown && foldoutRect.Contains(Event.current.mousePosition))
            {
                if (Event.current.button == 0) // Left mouse button
                {
                    _groupFoldoutStates[groupKey] = !_groupFoldoutStates[groupKey];
                    Event.current.Use();
                    GUI.changed = true;
                }
            }
            
            // Modify text color slightly when hovering for additional feedback
            if (isHovering)
            {
                Color originalTextColor = groupStyle.normal.textColor;
                groupStyle.normal.textColor = EditorGUIUtility.isProSkin 
                    ? Color.white 
                    : new Color(0.2f, 0.2f, 0.2f);
                
                // Draw the foldout with hover styling
                _groupFoldoutStates[groupKey] = EditorGUI.Foldout(foldoutRect, _groupFoldoutStates[groupKey], groupName, groupStyle);
                
                // Restore original text color
                groupStyle.normal.textColor = originalTextColor;
            }
            else
            {
                // Draw the foldout normally
                _groupFoldoutStates[groupKey] = EditorGUI.Foldout(foldoutRect, _groupFoldoutStates[groupKey], groupName, groupStyle);
            }
            
            // Force repaint on mouse move for smooth hover effects
            if (Event.current.type == EventType.MouseMove)
            {
                SceneView.RepaintAll();
            }
            
            if (_groupFoldoutStates[groupKey])
            {
                EditorGUI.indentLevel++;
                
                // Draw all buttons in the group
                foreach (var buttonInfo in buttons.OrderBy(b => b.Method.Name))
                {
                    // Reduce space for grouped buttons to make them more compact
                    var modifiedAttr = new ButtonAttribute(
                        buttonInfo.Attribute.Group,
                        buttonInfo.Attribute.Height,
                        Math.Max(1, buttonInfo.Attribute.Space - 2), // Reduce space but keep minimum of 1
                        buttonInfo.Attribute.Color,
                        buttonInfo.Attribute.PlayMode,
                        buttonInfo.Attribute.Name
                    );
                    
                    DrawButton(buttonInfo.Method, modifiedAttr);
                }
                
                EditorGUI.indentLevel--;
                GUILayout.Space(3); // Add some space after the group
            }
        }
        
        /// <summary>
        /// Draws an individual button with parameter support and play mode validation.
        /// </summary>
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
            var playModeText = "";
            
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
            
            var parameters = method.GetParameters();
            var methodKey = target.GetInstanceID() + "_" + method.Name;
            
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
                
                bool newFoldoutState = GUILayout.Toggle(_foldoutStates[methodKey], "", EditorStyles.foldout, GUILayout.Width(15), GUILayout.Height(buttonAttr.Height));
                if (_foldoutStates != null && newFoldoutState != _foldoutStates[methodKey])
                {
                    _foldoutStates[methodKey] = newFoldoutState;
                }
                
                buttonClicked = GUILayout.Button(buttonText, GUILayout.Height(buttonAttr.Height), GUILayout.ExpandWidth(true));
                
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                buttonClicked = GUILayout.Button(buttonText, GUILayout.Height(buttonAttr.Height));
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
        
        /// <summary>
        /// Draws appropriate GUI field for method parameter based on its type.
        /// </summary>
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
        /// Gets the default value for a method parameter, using the method's default value if available.
        /// </summary>
        private object GetMethodParameterDefaultValue(ParameterInfo parameter)
        {
            return parameter.HasDefaultValue 
                ? parameter.DefaultValue
                : GetTypeDefaultValue(parameter.ParameterType);
        }
        
        /// <summary>
        /// Gets the default value for a type.
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
    
    /// <summary>
    /// Custom editor for MonoBehaviour classes that adds button functionality.
    /// </summary>
    [CustomEditor(typeof(MonoBehaviour), true)]
    public class ButtonAttributeEditor : BaseButtonAttributeEditor
    {
    }
    
    /// <summary>
    /// Custom editor for ScriptableObject classes that adds button functionality.
    /// </summary>
    [CustomEditor(typeof(ScriptableObject), true)]
    public class ButtonAttributeScriptableObjectEditor : BaseButtonAttributeEditor
    {
    }
}

#endif