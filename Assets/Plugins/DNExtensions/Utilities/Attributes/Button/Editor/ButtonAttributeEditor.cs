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
        public readonly object InvokeTarget;
        public readonly string MethodKey;

        public ButtonInfo(MethodInfo method, ButtonAttribute attribute, object invokeTarget, string methodKey)
        {
            Method = method;
            Attribute = attribute;
            InvokeTarget = invokeTarget;
            MethodKey = methodKey;
        }
    }

    /// <summary>
    /// Base editor for drawing buttons from ButtonAttribute-decorated methods.
    /// Supports parameter input, grouping, play mode restrictions, and nested serialized class buttons.
    /// </summary>
    public abstract class BaseButtonAttributeEditor : Editor
    {
        // NOTE: These dictionaries persist for the lifetime of the editor instance.
        // Memory usage is minimal since we only store parameter values for visible inspectors.
        // If you select 1000 different objects rapidly, you might accumulate ~1KB of data.
        private readonly Dictionary<string, object[]> _methodParameters = new Dictionary<string, object[]>();
        private readonly Dictionary<string, bool> _foldoutStates = new Dictionary<string, bool>();
        private readonly Dictionary<string, bool> _groupFoldoutStates = new Dictionary<string, bool>();
        private readonly HashSet<string> _loggedValidationErrors = new HashSet<string>();
        
        private void OnDisable()
        {
            _methodParameters.Clear();
            _foldoutStates.Clear();
            _groupFoldoutStates.Clear();
            _loggedValidationErrors.Clear();
        }
        
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            DrawButtonsForTarget();
            DrawButtonsForNestedFields();
        }

        /// <summary>
        /// Scans one level of serialized fields on the target for Button-decorated methods
        /// and draws them, injecting the parent component when the method signature requires it.
        /// </summary>
        private void DrawButtonsForNestedFields()
        {
            var fields = target.GetType().GetFields(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var field in fields)
            {
                var value = field.GetValue(target);
                if (value == null) continue;

                var fieldType = field.FieldType;
                if (fieldType.IsPrimitive || fieldType == typeof(string)) continue;
                if (typeof(UnityEngine.Object).IsAssignableFrom(fieldType)) continue;

                if (fieldType.IsArray)
                {
                    var array = (Array)value;
                    for (int i = 0; i < array.Length; i++)
                        DrawButtonsForNestedInstance(array.GetValue(i), $"{field.Name}[{i}]", i, array.Length);
                }
                else if (value is System.Collections.IList list)
                {
                    for (int i = 0; i < list.Count; i++)
                        DrawButtonsForNestedInstance(list[i], $"{field.Name}[{i}]", i, list.Count);
                }
                else
                {
                    DrawButtonsForNestedInstance(value, field.Name, 0, 1);
                }
            }
        }

        /// <summary>
        /// Draws Button-decorated methods found on a single nested serialized instance.
        /// Always shows a label with the class name above the buttons, including the index when part of a multi-element array.
        /// </summary>
        private void DrawButtonsForNestedInstance(object instance, string fieldPath, int index = 0, int collectionSize = 1)
        {
            if (instance == null) return;

            var buttons = new List<ButtonInfo>();
            var methods = instance.GetType().GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var method in methods)
            {
                var buttonAttr = method.GetCustomAttribute<ButtonAttribute>();
                if (buttonAttr == null) continue;
                if (!ValidateNestedMethod(method, fieldPath)) continue;

                string methodKey = $"{target.GetInstanceID()}_{fieldPath}_{method.Name}";
                buttons.Add(new ButtonInfo(method, buttonAttr, instance, methodKey));
            }

            if (buttons.Count == 0) return;

            string className = ObjectNames.NicifyVariableName(instance.GetType().Name);
            string label = collectionSize > 1 ? $"{className} [{index}]" : className;
            EditorGUILayout.LabelField(label, EditorStyles.miniLabel);

            DrawButtonsForType(buttons);
        }

        /// <summary>
        /// Validates a method on a nested serialized class. Allows parameters that are
        /// injectable from the parent component in addition to standard supported types.
        /// </summary>
        private bool ValidateNestedMethod(MethodInfo method, string fieldPath)
        {
            var unsupportedParams = method.GetParameters()
                .Where(p => !p.ParameterType.IsAssignableFrom(target.GetType()) && !IsTypeSupported(p.ParameterType))
                .Select(p => $"{p.Name} ({p.ParameterType.Name})")
                .ToList();

            if (unsupportedParams.Count == 0) return true;

            string warningKey = $"{fieldPath}.{method.Name}";
            if (_loggedValidationErrors.Add(warningKey))
            {
                Debug.LogWarning(
                    $"[Button] Method '{method.Name}' on nested field '{fieldPath}' has unsupported parameters: " +
                    $"{string.Join(", ", unsupportedParams)}.",
                    target
                );
            }
            return false;
        }
        
        /// <summary>
        /// Finds all ButtonAttribute-decorated methods on the target and draws them grouped appropriately.
        /// </summary>
        private void DrawButtonsForTarget()
        {
            var buttonsByType = new Dictionary<Type, List<ButtonInfo>>();
            
            Type inspectedType = target.GetType();
            while (inspectedType != null && inspectedType != typeof(MonoBehaviour) && 
                   inspectedType != typeof(ScriptableObject))
            {
                var buttons = new List<ButtonInfo>();
                foreach (var method in inspectedType.GetMethods(
                    BindingFlags.Instance | BindingFlags.Static | 
                    BindingFlags.Public | BindingFlags.NonPublic | 
                    BindingFlags.DeclaredOnly))
                {
                    var buttonAttr = method.GetCustomAttribute<ButtonAttribute>();
                    if (buttonAttr == null) continue;
                    if (!ValidateMethod(method)) continue;

                    string methodKey = $"{target.GetInstanceID()}_{method.Name}";
                    buttons.Add(new ButtonInfo(method, buttonAttr, null, methodKey));
                }
                
                if (buttons.Count > 0)
                    buttonsByType[inspectedType] = buttons;
                
                inspectedType = inspectedType.BaseType;
            }
            
            foreach (var type in buttonsByType.Keys.OrderBy(GetInheritanceDepth))
                DrawButtonsForType(buttonsByType[type]);
        }

        /// <summary>
        /// Validates that a method's parameters are all supported types.
        /// Logs warnings for unsupported methods (once per session).
        /// </summary>
        private bool ValidateMethod(MethodInfo method)
        {
            var unsupportedParams = method.GetParameters()
                .Where(p => !IsTypeSupported(p.ParameterType))
                .Select(p => $"{p.Name} ({p.ParameterType.Name})")
                .ToList();

            if (unsupportedParams.Count == 0) return true;

            string warningKey = $"{target.GetType().Name}.{method.Name}";
            if (_loggedValidationErrors.Add(warningKey))
            {
                Debug.LogWarning(
                    $"[Button] Method '{method.Name}' in '{target.GetType().Name}' has unsupported parameter types and will not be shown: " +
                    $"{string.Join(", ", unsupportedParams)}. " +
                    $"Supported types: primitives, vectors, colors, Unity Objects, enums, curves, gradients.",
                    target
                );
            }
            return false;
        }

        /// <summary>
        /// Checks if a parameter type is supported by the editor.
        /// </summary>
        private bool IsTypeSupported(Type type)
        {
            if (type == typeof(int) || type == typeof(float) || type == typeof(double) || 
                type == typeof(long) || type == typeof(string) || type == typeof(bool))
                return true;
            if (type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Vector4) ||
                type == typeof(Vector2Int) || type == typeof(Vector3Int))
                return true;
            if (type == typeof(Color) || type == typeof(Color32)) return true;
            if (type == typeof(Rect) || type == typeof(RectInt)) return true;
            if (type == typeof(Bounds) || type == typeof(BoundsInt)) return true;
            if (type == typeof(AnimationCurve) || type == typeof(Gradient)) return true;
            if (type == typeof(LayerMask)) return true;
            if (type.IsEnum) return true;
            if (typeof(UnityEngine.Object).IsAssignableFrom(type)) return true;
            if (type.IsArray && type.GetElementType() == typeof(string)) return true;
            return false;
        }

        private int GetInheritanceDepth(Type type)
        {
            int depth = 0;
            Type current = type;
            while (current != null && current != typeof(MonoBehaviour) && current != typeof(ScriptableObject))
            {
                depth++;
                current = current.BaseType;
            }
            return depth;
        }

        private void DrawButtonsForType(List<ButtonInfo> buttonInfos)
        {
            var groupedButtons = buttonInfos
                .GroupBy(b => string.IsNullOrEmpty(b.Attribute.Group) ? "" : b.Attribute.Group)
                .OrderBy(g => g.Key);
            
            foreach (var group in groupedButtons)
            {
                if (string.IsNullOrEmpty(group.Key))
                {
                    foreach (var buttonInfo in group.OrderBy(b => b.Method.Name))
                        DrawButton(buttonInfo);
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
            string groupKey = $"{target.GetInstanceID()}_group_{groupName}";
            _groupFoldoutStates.TryAdd(groupKey, true);

            GUILayout.Space(5);
            
            var groupStyle = new GUIStyle(EditorStyles.foldout)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 12
            };

            _groupFoldoutStates[groupKey] = EditorGUILayout.Foldout(
                _groupFoldoutStates[groupKey], groupName, true, groupStyle);
            
            if (_groupFoldoutStates[groupKey])
            {
                EditorGUI.indentLevel++;
                foreach (var buttonInfo in buttons.OrderBy(b => b.Method.Name))
                    DrawButton(buttonInfo, isInGroup: true);
                EditorGUI.indentLevel--;
                GUILayout.Space(3);
            }
        }
        
        /// <summary>
        /// Draws an individual button with parameter support and play mode validation.
        /// Injectable parameters (assignable from the parent component) are resolved automatically
        /// and are not shown in the UI.
        /// </summary>
        private void DrawButton(ButtonInfo buttonInfo, bool isInGroup = false)
        {
            var method = buttonInfo.Method;
            var buttonAttr = buttonInfo.Attribute;
            var methodKey = buttonInfo.MethodKey;

            int actualHeight = buttonAttr.Height >= 0 ? buttonAttr.Height : ButtonSettings.Instance.ButtonHeight;
            int actualSpace = buttonAttr.Space >= 0 ? buttonAttr.Space : ButtonSettings.Instance.ButtonSpace;
            ButtonPlayMode actualPlayMode = buttonAttr.PlayMode != ButtonPlayMode.UseDefault 
                ? buttonAttr.PlayMode 
                : ButtonSettings.Instance.ButtonPlayMode;
            Color actualColor = buttonAttr.Color != Color.clear ? buttonAttr.Color : ButtonSettings.Instance.ButtonColor;
            
            if (isInGroup && actualSpace > 0)
                actualSpace = Math.Max(1, actualSpace - 2);
            
            if (actualSpace > 0)
                GUILayout.Space(actualSpace);
            
            string buttonText = string.IsNullOrEmpty(buttonAttr.Name) 
                ? ObjectNames.NicifyVariableName(method.Name) 
                : buttonAttr.Name;
            
            bool shouldDisable = false;
            switch (actualPlayMode)
            {
                case ButtonPlayMode.OnlyWhenPlaying:
                    shouldDisable = !Application.isPlaying;
                    if (shouldDisable) buttonText += "\n(Play Mode Only)";
                    break;
                case ButtonPlayMode.OnlyWhenNotPlaying:
                    shouldDisable = Application.isPlaying;
                    if (shouldDisable) buttonText += "\n(Edit Mode Only)";
                    break;
            }
            
            var allParameters = method.GetParameters();

            // Injectable parameters are resolved from the parent component — not shown in UI
            var visibleParameters = allParameters
                .Where(p => !p.ParameterType.IsAssignableFrom(target.GetType()))
                .ToArray();
            
            if (!_methodParameters.ContainsKey(methodKey))
            {
                _methodParameters[methodKey] = new object[visibleParameters.Length];
                for (int i = 0; i < visibleParameters.Length; i++)
                    _methodParameters[methodKey][i] = GetMethodParameterDefaultValue(visibleParameters[i]);
            }
            
            _foldoutStates.TryAdd(methodKey, false);

            Color originalColor = GUI.backgroundColor;
            bool originalEnabled = GUI.enabled;
            
            GUI.backgroundColor = shouldDisable ? new Color(0.5f, 0.5f, 0.5f, 0.8f) : actualColor;
            GUI.enabled = !shouldDisable;
            
            bool buttonClicked;
            if (visibleParameters.Length > 0)
            {
                EditorGUILayout.BeginHorizontal();
                bool newFoldout = GUILayout.Toggle(_foldoutStates[methodKey], "", EditorStyles.foldout, GUILayout.Width(15), GUILayout.Height(actualHeight));
                if (newFoldout != _foldoutStates[methodKey])
                    _foldoutStates[methodKey] = newFoldout;
                buttonClicked = GUILayout.Button(buttonText, GUILayout.Height(actualHeight), GUILayout.ExpandWidth(true));
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                buttonClicked = GUILayout.Button(buttonText, GUILayout.Height(actualHeight));
            }
            
            if (buttonClicked && !shouldDisable)
            {
                object invokeTarget = buttonInfo.InvokeTarget ?? target;

                if (!method.IsStatic)
                    Undo.RecordObject(target, $"Button: {method.Name}");
                
                try
                {
                    // Build full parameter array, injecting parent component where assignable
                    int visibleIndex = 0;
                    object[] resolvedParams = new object[allParameters.Length];
                    for (int i = 0; i < allParameters.Length; i++)
                    {
                        if (allParameters[i].ParameterType.IsAssignableFrom(target.GetType()))
                            resolvedParams[i] = target;
                        else
                            resolvedParams[i] = _methodParameters[methodKey][visibleIndex++];
                    }

                    method.Invoke(invokeTarget, resolvedParams);
                    
                    if (!Application.isPlaying && target != null)
                        EditorUtility.SetDirty(target);
                }
                catch (TargetInvocationException e)
                {
                    Exception inner = e.InnerException ?? e;
                    Debug.LogError(
                        $"[Button] Error invoking '{method.Name}' on '{target.name}': {inner.GetType().Name}: {inner.Message}\n" +
                        $"Parameters used: {FormatParameters(_methodParameters[methodKey])}\n" +
                        $"Stack trace:\n{inner.StackTrace}",
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
            
            if (visibleParameters.Length > 0 && _foldoutStates[methodKey])
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                for (int i = 0; i < visibleParameters.Length; i++)
                {
                    _methodParameters[methodKey][i] = DrawParameterField(
                        visibleParameters[i].Name, 
                        visibleParameters[i].ParameterType, 
                        _methodParameters[methodKey][i],
                        visibleParameters[i]
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
            if (parameters == null || parameters.Length == 0) return "none";
            return string.Join(", ", parameters.Select((p, i) => $"[{i}] = {(p != null ? p.ToString() : "null")}"));
        }
        
        /// <summary>
        /// Draws the appropriate GUI field for a method parameter based on its type.
        /// </summary>
        private object DrawParameterField(string paramName, Type paramType, object currentValue, ParameterInfo paramInfo = null)
        {
            string niceName = ObjectNames.NicifyVariableName(paramName);
            RangeAttribute rangeAttr = paramInfo?.GetCustomAttribute<RangeAttribute>();
            
            if (paramType == typeof(int))
                return rangeAttr != null
                    ? EditorGUILayout.IntSlider(niceName, currentValue != null ? (int)currentValue : 0, (int)rangeAttr.min, (int)rangeAttr.max)
                    : EditorGUILayout.IntField(niceName, currentValue != null ? (int)currentValue : 0);
            
            if (paramType == typeof(float))
                return rangeAttr != null
                    ? EditorGUILayout.Slider(niceName, currentValue != null ? (float)currentValue : 0f, rangeAttr.min, rangeAttr.max)
                    : EditorGUILayout.FloatField(niceName, currentValue != null ? (float)currentValue : 0f);
            
            if (paramType == typeof(double))
                return EditorGUILayout.DoubleField(niceName, currentValue != null ? (double)currentValue : 0.0);
            if (paramType == typeof(long))
                return EditorGUILayout.LongField(niceName, currentValue != null ? (long)currentValue : 0L);
            if (paramType == typeof(string))
                return EditorGUILayout.TextField(niceName, currentValue != null ? (string)currentValue : "");
            if (paramType == typeof(bool))
                return EditorGUILayout.Toggle(niceName, currentValue != null && (bool)currentValue);
            if (paramType == typeof(Vector2))
                return EditorGUILayout.Vector2Field(niceName, currentValue != null ? (Vector2)currentValue : Vector2.zero);
            if (paramType == typeof(Vector3))
                return EditorGUILayout.Vector3Field(niceName, currentValue != null ? (Vector3)currentValue : Vector3.zero);
            if (paramType == typeof(Vector4))
                return EditorGUILayout.Vector4Field(niceName, currentValue != null ? (Vector4)currentValue : Vector4.zero);
            if (paramType == typeof(Vector2Int))
                return EditorGUILayout.Vector2IntField(niceName, currentValue != null ? (Vector2Int)currentValue : Vector2Int.zero);
            if (paramType == typeof(Vector3Int))
                return EditorGUILayout.Vector3IntField(niceName, currentValue != null ? (Vector3Int)currentValue : Vector3Int.zero);
            if (paramType == typeof(Color))
                return EditorGUILayout.ColorField(niceName, currentValue != null ? (Color)currentValue : Color.white);
            if (paramType == typeof(Color32))
            {
                Color32 c = currentValue != null ? (Color32)currentValue : (Color32)Color.white;
                return (Color32)EditorGUILayout.ColorField(niceName, c);
            }
            if (paramType == typeof(Rect))
                return EditorGUILayout.RectField(niceName, currentValue != null ? (Rect)currentValue : new Rect(0, 0, 100, 100));
            if (paramType == typeof(RectInt))
                return EditorGUILayout.RectIntField(niceName, currentValue != null ? (RectInt)currentValue : new RectInt(0, 0, 100, 100));
            if (paramType == typeof(Bounds))
                return EditorGUILayout.BoundsField(niceName, currentValue != null ? (Bounds)currentValue : new Bounds());
            if (paramType == typeof(BoundsInt))
                return EditorGUILayout.BoundsIntField(niceName, currentValue != null ? (BoundsInt)currentValue : new BoundsInt());
            if (paramType == typeof(AnimationCurve))
                return EditorGUILayout.CurveField(niceName, currentValue != null ? (AnimationCurve)currentValue : AnimationCurve.Linear(0, 0, 1, 1));
            if (paramType == typeof(Gradient))
                return EditorGUILayout.GradientField(niceName, currentValue != null ? (Gradient)currentValue : new Gradient());
            if (paramType == typeof(LayerMask))
                return EditorGUILayout.MaskField(niceName, currentValue != null ? (LayerMask)currentValue : (LayerMask)0, UnityEditorInternal.InternalEditorUtility.layers);
            if (paramType.IsEnum)
                return EditorGUILayout.EnumPopup(niceName, currentValue != null ? (Enum)currentValue : (Enum)Enum.GetValues(paramType).GetValue(0));
            if (typeof(UnityEngine.Object).IsAssignableFrom(paramType))
                return EditorGUILayout.ObjectField(niceName, (UnityEngine.Object)currentValue, paramType, true);
            if (paramType.IsArray && paramType.GetElementType() == typeof(string))
            {
                string[] array = (string[])currentValue ?? Array.Empty<string>();
                EditorGUILayout.LabelField($"{niceName} (String Array)");
                EditorGUI.indentLevel++;
                int newSize = EditorGUILayout.IntField("Size", array.Length);
                if (newSize != array.Length) Array.Resize(ref array, newSize);
                for (int i = 0; i < array.Length; i++)
                    array[i] = EditorGUILayout.TextField($"Element {i}", array[i] ?? "");
                EditorGUI.indentLevel--;
                return array;
            }

            EditorGUILayout.HelpBox($"Unsupported type: {paramType.Name}", MessageType.Error);
            return currentValue;
        } 
        
        /// <summary>
        /// Gets the default value for a method parameter, using the declared default if available.
        /// </summary>
        private object GetMethodParameterDefaultValue(ParameterInfo parameter)
        {
            return parameter.HasDefaultValue ? parameter.DefaultValue : GetTypeDefaultValue(parameter.ParameterType);
        }
        
        /// <summary>
        /// Gets the default value for a given type.
        /// </summary>
        private object GetTypeDefaultValue(Type type)
        {
            if (type == typeof(string)) return "";
            if (type == typeof(int)) return 0;
            if (type == typeof(float)) return 0f;
            if (type == typeof(double)) return 0.0;
            if (type == typeof(long)) return 0L;
            if (type == typeof(bool)) return false;
            if (type == typeof(Vector2)) return Vector2.zero;
            if (type == typeof(Vector3)) return Vector3.zero;
            if (type == typeof(Vector4)) return Vector4.zero;
            if (type == typeof(Vector2Int)) return Vector2Int.zero;
            if (type == typeof(Vector3Int)) return Vector3Int.zero;
            if (type == typeof(Color)) return Color.white;
            if (type == typeof(Color32)) return (Color32)Color.white;
            if (type == typeof(Rect)) return new Rect(0, 0, 100, 100);
            if (type == typeof(RectInt)) return new RectInt(0, 0, 100, 100);
            if (type == typeof(Bounds)) return new Bounds();
            if (type == typeof(BoundsInt)) return new BoundsInt();
            if (type == typeof(AnimationCurve)) return AnimationCurve.Linear(0, 0, 1, 1);
            if (type == typeof(Gradient)) return new Gradient();
            if (type == typeof(LayerMask)) return (LayerMask)0;
            if (type.IsEnum) return Enum.GetValues(type).GetValue(0);
            if (typeof(UnityEngine.Object).IsAssignableFrom(type)) return null;
            if (type.IsArray) return Array.CreateInstance(type.GetElementType() ?? throw new InvalidOperationException(), 0);
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }
    }
    
    /// <summary>
    /// Custom editor for MonoBehaviour classes that adds button functionality.
    /// </summary>
    [CustomEditor(typeof(MonoBehaviour), true)]
    public class ButtonAttributeEditor : BaseButtonAttributeEditor { }
    
    /// <summary>
    /// Custom editor for ScriptableObject classes that adds button functionality.
    /// </summary>
    [CustomEditor(typeof(ScriptableObject), true)]
    public class ButtonAttributeScriptableObjectEditor : BaseButtonAttributeEditor { }
}