
using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEditor;
using System.Reflection;
using System.Linq;

namespace DNExtensions.Utilities.Button
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
            this.Method = method;
            this.Attribute = attribute;
        }
    }

    /// <summary>
    /// Base editor for drawing buttons from ButtonAttribute-decorated methods.
    /// Supports parameter input, grouping, and play mode restrictions.
    /// </summary>
    public abstract class BaseButtonAttributeEditor : UnityEditor.Editor
    {
        // NOTE: These dictionaries persist for the lifetime of the editor instance.
        // Memory usage is minimal since we only store parameter values for visible inspectors.
        // If you select 1000 different objects rapidly, you might accumulate ~1KB of data.
        private readonly Dictionary<string, object[]> _methodParameters = new Dictionary<string, object[]>();
        private readonly Dictionary<string, bool> _foldoutStates = new Dictionary<string, bool>();
        private readonly Dictionary<string, bool> _groupFoldoutStates = new Dictionary<string, bool>();
        
        // Track validation errors to avoid spamming console
        private readonly HashSet<string> _loggedValidationErrors = new HashSet<string>();
        
        private void OnDisable()
        {
            // Clean up our dictionaries when this editor is destroyed
            _methodParameters.Clear();
            _foldoutStates.Clear();
            _groupFoldoutStates.Clear();
            _loggedValidationErrors.Clear();
        }
        
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
            Type currentType = target.GetType();
            
            // Collect buttons organized by declaring type (base to derived)
            var buttonsByType = new Dictionary<Type, List<ButtonInfo>>();
            
            // Walk up the inheritance chain
            Type inspectedType = currentType;
            while (inspectedType != null && inspectedType != typeof(MonoBehaviour) && 
                   inspectedType != typeof(ScriptableObject))
            {
                MethodInfo[] methods = inspectedType.GetMethods(
                    BindingFlags.Instance | BindingFlags.Static | 
                    BindingFlags.Public | BindingFlags.NonPublic | 
                    BindingFlags.DeclaredOnly);
                
                List<ButtonInfo> buttonsForType = new List<ButtonInfo>();
                foreach (MethodInfo method in methods)
                {
                    ButtonAttribute buttonAttr = method.GetCustomAttribute<ButtonAttribute>();
                    if (buttonAttr != null)
                    {
                        // Validate the method - filter out ones with unsupported parameters
                        if (ValidateMethod(method))
                        {
                            buttonsForType.Add(new ButtonInfo(method, buttonAttr));
                        }
                    }
                }
                
                if (buttonsForType.Count > 0)
                {
                    buttonsByType[inspectedType] = buttonsForType;
                }
                
                inspectedType = inspectedType.BaseType;
            }
            
            // Draw buttons from base to derived
            var sortedTypes = buttonsByType.Keys.OrderBy(GetInheritanceDepth).ToList();
            
            foreach (Type type in sortedTypes)
            {
                DrawButtonsForType(buttonsByType[type]);
            }
        }

        /// <summary>
        /// Validates that a method's parameters are all supported types.
        /// Logs warnings for unsupported methods (once per session).
        /// </summary>
        private bool ValidateMethod(MethodInfo method)
        {
            var parameters = method.GetParameters();
            var unsupportedParams = new List<string>();
            
            foreach (var param in parameters)
            {
                if (!IsTypeSupported(param.ParameterType))
                {
                    unsupportedParams.Add($"{param.Name} ({param.ParameterType.Name})");
                }
            }
            
            if (unsupportedParams.Count > 0)
            {
                string warningKey = $"{target.GetType().Name}.{method.Name}";
                if (!_loggedValidationErrors.Contains(warningKey))
                {
                    _loggedValidationErrors.Add(warningKey);
                    Debug.LogWarning(
                        $"[Button] Method '{method.Name}' in '{target.GetType().Name}' has unsupported parameter types and will not be shown: " +
                        $"{string.Join(", ", unsupportedParams)}. " +
                        $"Supported types: primitives, vectors, colors, Unity Objects, enums, curves, gradients.",
                        target
                    );
                }
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// Checks if a parameter type is supported by the editor.
        /// </summary>
        private bool IsTypeSupported(Type type)
        {
            // Basic types
            if (type == typeof(int) || type == typeof(float) || type == typeof(double) || 
                type == typeof(long) || type == typeof(string) || type == typeof(bool))
                return true;
            
            // Vector types
            if (type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Vector4) ||
                type == typeof(Vector2Int) || type == typeof(Vector3Int))
                return true;
            
            // Color types
            if (type == typeof(Color) || type == typeof(Color32))
                return true;
            
            // Rect types
            if (type == typeof(Rect) || type == typeof(RectInt))
                return true;
            
            // Bounds types
            if (type == typeof(Bounds) || type == typeof(BoundsInt))
                return true;
            
            // Curves and Gradients
            if (type == typeof(AnimationCurve) || type == typeof(Gradient))
                return true;
            
            // LayerMask
            if (type == typeof(LayerMask))
                return true;
            
            // Enums
            if (type.IsEnum)
                return true;
            
            // Unity Object references
            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                return true;
            
            // String arrays (basic array support)
            if (type.IsArray && type.GetElementType() == typeof(string))
                return true;
            
            return false;
        }

        private int GetInheritanceDepth(Type type)
        {
            int depth = 0;
            Type current = type;
            while (current != null && current != typeof(MonoBehaviour) && 
                   current != typeof(ScriptableObject))
            {
                depth++;
                current = current.BaseType;
            }
            return depth;
        }

        private void DrawButtonsForType(List<ButtonInfo> buttonInfos)
        {
            // Group buttons by their Group property
            var groupedButtons = buttonInfos
                .GroupBy(b => string.IsNullOrEmpty(b.Attribute.Group) ? "" : b.Attribute.Group)
                .OrderBy(g => g.Key);
            
            foreach (var group in groupedButtons)
            {
                if (string.IsNullOrEmpty(group.Key))
                {
                    foreach (var buttonInfo in group.OrderBy(b => b.Method.Name))
                    {
                        DrawButton(buttonInfo.Method, buttonInfo.Attribute);
                    }
                }
                else
                {
                    DrawButtonGroup(group.Key, group.ToList());
                }
            }
        }
        
        /// <summary>
        /// Draws a collapsible group of buttons.
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

            // Draw the foldout - Unity handles hover states automatically
            _groupFoldoutStates[groupKey] = EditorGUILayout.Foldout(
                _groupFoldoutStates[groupKey], 
                groupName, 
                true,
                groupStyle
            );
            
            if (_groupFoldoutStates[groupKey])
            {
                EditorGUI.indentLevel++;
                
                // Draw all buttons in the group with reduced spacing
                foreach (var buttonInfo in buttons.OrderBy(b => b.Method.Name))
                {
                    DrawButton(buttonInfo.Method, buttonInfo.Attribute, isInGroup: true);
                }
                
                EditorGUI.indentLevel--;
                GUILayout.Space(3);
            }
        }
        
        /// <summary>
        /// Draws an individual button with parameter support and play mode validation.
        /// </summary>
        private void DrawButton(MethodInfo method, ButtonAttribute buttonAttr, bool isInGroup = false)
        {
            // Resolve actual values from settings where not explicitly set
            int actualHeight = buttonAttr.Height >= 0 ? buttonAttr.Height : ButtonSettings.ButtonHeight;
            int actualSpace = buttonAttr.Space >= 0 ? buttonAttr.Space : ButtonSettings.ButtonSpace;
            ButtonPlayMode actualPlayMode = buttonAttr.PlayMode != ButtonPlayMode.UseDefault 
                ? buttonAttr.PlayMode 
                : ButtonSettings.ButtonPlayMode;
            Color actualColor = buttonAttr.Color != Color.clear ? buttonAttr.Color : ButtonSettings.ButtonColor;
            string actualGroup = !string.IsNullOrEmpty(buttonAttr.Group) ? buttonAttr.Group : ButtonSettings.ButtonGroup;
            
            // Reduce space for grouped buttons
            if (isInGroup && actualSpace > 0)
            {
                actualSpace = Math.Max(1, actualSpace - 2);
            }
            
            if (actualSpace > 0)
            {
                GUILayout.Space(actualSpace);
            }
            
            string buttonText = string.IsNullOrEmpty(buttonAttr.Name) 
                ? ObjectNames.NicifyVariableName(method.Name) 
                : buttonAttr.Name;
            
            bool shouldDisable;
            var playModeText = "";
            
            switch (actualPlayMode)
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
                GUI.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);
                GUI.enabled = false;
            }
            else
            {
                GUI.backgroundColor = actualColor;
            }
            
            bool buttonClicked;
            
            if (parameters.Length > 0)
            {
                EditorGUILayout.BeginHorizontal();
                
                bool newFoldoutState = GUILayout.Toggle(_foldoutStates[methodKey], "", EditorStyles.foldout, GUILayout.Width(15), GUILayout.Height(actualHeight));
                if (_foldoutStates != null && newFoldoutState != _foldoutStates[methodKey])
                {
                    _foldoutStates[methodKey] = newFoldoutState;
                }
                
                buttonClicked = GUILayout.Button(buttonText, GUILayout.Height(actualHeight), GUILayout.ExpandWidth(true));
                
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                buttonClicked = GUILayout.Button(buttonText, GUILayout.Height(actualHeight));
            }
            
            if (buttonClicked && !shouldDisable)
            {
                // Record undo for methods that modify serialized state
                if (!method.IsStatic)
                {
                    Undo.RecordObject(target, $"Button: {method.Name}");
                }
                
                try
                {
                    method.Invoke(target, _methodParameters[methodKey]);
                    
                    // Mark dirty if we modified a non-scene object (like a ScriptableObject)
                    if (!Application.isPlaying && target != null)
                    {
                        EditorUtility.SetDirty(target);
                    }
                }
                catch (TargetInvocationException e)
                {
                    // Unwrap the actual exception
                    Exception innerException = e.InnerException ?? e;
                    Debug.LogError(
                        $"[Button] Error invoking '{method.Name}' on '{target.name}': {innerException.GetType().Name}: {innerException.Message}\n" +
                        $"Parameters used: {FormatParameters(_methodParameters[methodKey])}\n" +
                        $"Stack trace:\n{innerException.StackTrace}",
                        target
                    );
                }
                catch (Exception e)
                {
                    Debug.LogError(
                        $"[Button] Unexpected error invoking '{method.Name}' on '{target.name}': {e.GetType().Name}: {e.Message}",
                        target
                    );
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
                        parameters[i]
                    );
                }
    
                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel--;
            }
        }

        /// <summary>
        /// Formats parameters for error logging.
        /// </summary>
        private string FormatParameters(object[] parameters)
        {
            if (parameters == null || parameters.Length == 0)
                return "none";
            
            return string.Join(", ", parameters.Select((p, i) => 
                $"[{i}] = {(p != null ? p.ToString() : "null")}"));
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
            else if (paramType == typeof(string) && (paramName.ToLower().Contains("text") || paramName.ToLower().Contains("description")))
            {
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
            
            // This should never be reached due to validation, but just in case
            else
            {
                EditorGUILayout.HelpBox($"Unsupported type: {paramType.Name}", MessageType.Error);
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

