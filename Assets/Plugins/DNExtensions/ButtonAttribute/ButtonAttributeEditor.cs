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

            GUILayout.Space(5);
            
            var groupStyle = new GUIStyle(EditorStyles.foldout)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 12
            };

            // Get the rect that the foldout will occupy
            Rect foldoutRect = GUILayoutUtility.GetRect(new GUIContent(groupName), groupStyle);
            
            // Handle hover effect like Unity's default lists
            bool isHovering = foldoutRect.Contains(Event.current.mousePosition);
            
            if (isHovering)
            {
                // Request repaint to ensure smooth hover effect
                if (Event.current.type == EventType.MouseMove)
                {
                    Event.current.Use();
                    GUI.changed = true;
                }
            }
            
            // Draw the foldout
            _groupFoldoutStates[groupKey] = EditorGUI.Foldout(
                foldoutRect, 
                _groupFoldoutStates[groupKey], 
                groupName, 
                true,
                groupStyle
            );
            
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
                        _methodParameters[methodKey][i],
                        parameters[i]  // Pass the ParameterInfo for Range attribute detection
                    );
                }
    
                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel--;
            }
        }
        
    /// <summary>
    /// Draws appropriate GUI field for method parameter based on its type.
    /// </summary>
    private object DrawParameterField(string paramName, Type paramType, object currentValue, ParameterInfo paramInfo = null)
    {
        string niceName = ObjectNames.NicifyVariableName(paramName);
        
        // Check for Range attribute on the parameter
        RangeAttribute rangeAttr = paramInfo?.GetCustomAttribute<RangeAttribute>();
        
        // Basic types with Range support
        if (paramType == typeof(int))
        {
            if (rangeAttr != null)
            {
                return EditorGUILayout.IntSlider(niceName, currentValue != null ? (int)currentValue : 0, (int)rangeAttr.min, (int)rangeAttr.max);
            }
            return EditorGUILayout.IntField(niceName, currentValue != null ? (int)currentValue : 0);
        }
        else if (paramType == typeof(float))
        {
            if (rangeAttr != null)
            {
                return EditorGUILayout.Slider(niceName, currentValue != null ? (float)currentValue : 0f, rangeAttr.min, rangeAttr.max);
            }
            return EditorGUILayout.FloatField(niceName, currentValue != null ? (float)currentValue : 0f);
        }
        else if (paramType == typeof(double))
        {
            // Note: No slider support for double in Unity's EditorGUILayout
            return EditorGUILayout.DoubleField(niceName, currentValue != null ? (double)currentValue : 0.0);
        }
        else if (paramType == typeof(long))
        {
            return EditorGUILayout.LongField(niceName, currentValue != null ? (long)currentValue : 0L);
        }
        else if (paramType == typeof(string))
        {
            return EditorGUILayout.TextField(niceName, currentValue != null ? (string)currentValue : "");
        }
        else if (paramType == typeof(bool))
        {
            return EditorGUILayout.Toggle(niceName, currentValue != null && (bool)currentValue);
        }
        
        // Vector types
        else if (paramType == typeof(Vector2))
        {
            return EditorGUILayout.Vector2Field(niceName, currentValue != null ? (Vector2)currentValue : Vector2.zero);
        }
        else if (paramType == typeof(Vector3))
        {
            return EditorGUILayout.Vector3Field(niceName, currentValue != null ? (Vector3)currentValue : Vector3.zero);
        }
        else if (paramType == typeof(Vector4))
        {
            return EditorGUILayout.Vector4Field(niceName, currentValue != null ? (Vector4)currentValue : Vector4.zero);
        }
        else if (paramType == typeof(Vector2Int))
        {
            return EditorGUILayout.Vector2IntField(niceName, currentValue != null ? (Vector2Int)currentValue : Vector2Int.zero);
        }
        else if (paramType == typeof(Vector3Int))
        {
            return EditorGUILayout.Vector3IntField(niceName, currentValue != null ? (Vector3Int)currentValue : Vector3Int.zero);
        }
        
        // Color types
        else if (paramType == typeof(Color))
        {
            return EditorGUILayout.ColorField(niceName, currentValue != null ? (Color)currentValue : Color.white);
        }
        else if (paramType == typeof(Color32))
        {
            Color32 color32 = currentValue != null ? (Color32)currentValue : Color.white;
            Color color = EditorGUILayout.ColorField(niceName, color32);
            return (Color32)color;
        }
        
        // Rect types
        else if (paramType == typeof(Rect))
        {
            return EditorGUILayout.RectField(niceName, currentValue != null ? (Rect)currentValue : new Rect(0, 0, 100, 100));
        }
        else if (paramType == typeof(RectInt))
        {
            return EditorGUILayout.RectIntField(niceName, currentValue != null ? (RectInt)currentValue : new RectInt(0, 0, 100, 100));
        }
        
        // Bounds types
        else if (paramType == typeof(Bounds))
        {
            return EditorGUILayout.BoundsField(niceName, currentValue != null ? (Bounds)currentValue : new Bounds());
        }
        else if (paramType == typeof(BoundsInt))
        {
            return EditorGUILayout.BoundsIntField(niceName, currentValue != null ? (BoundsInt)currentValue : new BoundsInt());
        }
        
        // Curves and Gradients
        else if (paramType == typeof(AnimationCurve))
        {
            return EditorGUILayout.CurveField(niceName, currentValue != null ? (AnimationCurve)currentValue : AnimationCurve.Linear(0, 0, 1, 1));
        }
        else if (paramType == typeof(Gradient))
        {
            return EditorGUILayout.GradientField(niceName, currentValue != null ? (Gradient)currentValue : new Gradient());
        }
        
        // Text area for multiline strings
        else if (paramType == typeof(string) && paramName.ToLower().Contains("text") || paramName.ToLower().Contains("description"))
        {
            // Use text area for parameters that might need multiple lines
            return EditorGUILayout.TextArea((string)currentValue ?? "", GUILayout.Height(60));
        }
        
        // LayerMask
        else if (paramType == typeof(LayerMask))
        {
            LayerMask mask = currentValue != null ? (LayerMask)currentValue : 0;
            return EditorGUILayout.MaskField(niceName, mask, UnityEditorInternal.InternalEditorUtility.layers);
        }
        
        // Enums
        else if (paramType.IsEnum)
        {
            return EditorGUILayout.EnumPopup(niceName, currentValue != null ? (Enum)currentValue : (Enum)Enum.GetValues(paramType).GetValue(0));
        }
        
        // Unity Object references
        else if (typeof(UnityEngine.Object).IsAssignableFrom(paramType))
        {
            return EditorGUILayout.ObjectField(niceName, (UnityEngine.Object)currentValue, paramType, true);
        }
        
        // Generic array support (limited)
        else if (paramType.IsArray && paramType.GetElementType() == typeof(string))
        {
            string[] array = (string[])currentValue ?? new string[0];
            EditorGUILayout.LabelField(niceName + " (String Array)");
            EditorGUI.indentLevel++;
            
            // Simple array editor - you might want to make this more sophisticated
            int newSize = EditorGUILayout.IntField("Size", array.Length);
            if (newSize != array.Length)
            {
                Array.Resize(ref array, newSize);
            }
            
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = EditorGUILayout.TextField($"Element {i}", array[i] ?? "");
            }
            
            EditorGUI.indentLevel--;
            return array;
        }
        
        // Fallback for unsupported types
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
            // Basic types
            if (type == typeof(string)) return "";
            if (type == typeof(int)) return 0;
            if (type == typeof(float)) return 0f;
            if (type == typeof(double)) return 0.0;
            if (type == typeof(long)) return 0L;
            if (type == typeof(bool)) return false;
    
            // Vector types
            if (type == typeof(Vector2)) return Vector2.zero;
            if (type == typeof(Vector3)) return Vector3.zero;
            if (type == typeof(Vector4)) return Vector4.zero;
            if (type == typeof(Vector2Int)) return Vector2Int.zero;
            if (type == typeof(Vector3Int)) return Vector3Int.zero;
    
            // Color types
            if (type == typeof(Color)) return Color.white;
            if (type == typeof(Color32)) return (Color32)Color.white;
    
            // Rect types
            if (type == typeof(Rect)) return new Rect(0, 0, 100, 100);
            if (type == typeof(RectInt)) return new RectInt(0, 0, 100, 100);
    
            // Bounds types
            if (type == typeof(Bounds)) return new Bounds();
            if (type == typeof(BoundsInt)) return new BoundsInt();
    
            // Curves and Gradients
            if (type == typeof(AnimationCurve)) return AnimationCurve.Linear(0, 0, 1, 1);
            if (type == typeof(Gradient)) return new Gradient();
    
            // LayerMask
            if (type == typeof(LayerMask)) return (LayerMask)0;
    
            // Enums
            if (type.IsEnum) return Enum.GetValues(type).GetValue(0);
    
            // Unity Objects
            if (typeof(UnityEngine.Object).IsAssignableFrom(type)) return null;
    
            // Arrays
            if (type.IsArray) return Array.CreateInstance(type.GetElementType() ?? throw new InvalidOperationException(), 0);
    
            // Generic fallback for value types
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