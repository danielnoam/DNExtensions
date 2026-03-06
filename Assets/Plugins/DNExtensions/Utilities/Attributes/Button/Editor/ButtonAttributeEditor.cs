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
        private readonly Dictionary<string, object[]> _methodParameters = new();
        private readonly Dictionary<string, bool> _foldoutStates = new();
        private readonly Dictionary<string, bool> _groupFoldoutStates = new();
        private readonly HashSet<string> _loggedValidationErrors = new();

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

            if (!HasAnyButtons()) return;

            if (ButtonSettings.Instance.DrawSeparator)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            }

            DrawButtonsForTarget();

            if (ButtonSettings.Instance.DrawNestedButtons)
            {
                DrawButtonsForNestedFields();
            }
        }

        /// <summary>
        /// Scans serialized fields on the target for nested serializable classes with
        /// Button-decorated methods, and draws them below the default inspector.
        /// </summary>
        private void DrawButtonsForNestedFields()
        {
            var fields = target.GetType().GetFields(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var field in fields)
            {
                if (!field.IsSerializedByUnity()) continue;

                var value = field.GetValue(target);
                if (value == null) continue;

                var fieldType = field.FieldType;
                if (fieldType.IsPrimitive || fieldType == typeof(string)) continue;
                if (typeof(UnityEngine.Object).IsAssignableFrom(fieldType)) continue;

                if (fieldType.IsArray)
                {
                    var elementType = fieldType.GetElementType();
                    if (elementType == null || typeof(UnityEngine.Object).IsAssignableFrom(elementType)) continue;

                    var array = (Array)value;
                    if (array.Length == 0) continue;

                    DrawNestedCollectionWithFoldout(field.Name, array.Cast<object>());
                }
                else if (value is System.Collections.IList list)
                {
                    var genericArgs = fieldType.GetGenericArguments();
                    if (genericArgs.Length > 0 && typeof(UnityEngine.Object).IsAssignableFrom(genericArgs[0])) continue;
                    if (list.Count == 0) continue;

                    DrawNestedCollectionWithFoldout(field.Name, list.Cast<object>());
                }
                else
                {
                    DrawButtonsForNestedInstance(value, field.Name, 0, 1);
                }
            }
        }

        private void DrawNestedCollectionWithFoldout(string fieldName, IEnumerable<object> elements)
        {
            var validElements = elements
                .Select((element, i) => (element, i))
                .Where(pair => pair.element != null && pair.element.HasButtonMethods())
                .ToList();

            if (validElements.Count == 0) return;

            string foldoutKey = $"{target.GetInstanceID()}_arrayfoldout_{fieldName}";
            _groupFoldoutStates.TryAdd(foldoutKey, true);

            _groupFoldoutStates[foldoutKey] = EditorGUILayout.Foldout(
                _groupFoldoutStates[foldoutKey],
                $"{ObjectNames.NicifyVariableName(fieldName)} Buttons",
                true);

            if (!_groupFoldoutStates[foldoutKey]) return;

            EditorGUI.indentLevel++;
            foreach (var (element, i) in validElements)
            {
                DrawButtonsForNestedInstance(element, $"{fieldName}[{i}]", i, validElements.Count);
            }
            EditorGUI.indentLevel--;
        }

        private void DrawButtonsForNestedInstance(object instance, string fieldPath, int index = 0, int collectionSize = 1)
        {
            if (instance == null) return;
            if (!instance.GetType().IsSerializableClass()) return;

            var buttons = new List<ButtonInfo>();

            foreach (var method in instance.GetType().GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
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
        /// Finds all ButtonAttribute-decorated methods on the target (including base types)
        /// and draws them grouped appropriately.
        /// </summary>
        private void DrawButtonsForTarget()
        {
            var buttonsByType = new Dictionary<Type, List<ButtonInfo>>();

            var inspectedType = target.GetType();
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
                {
                    buttonsByType[inspectedType] = buttons;
                }

                inspectedType = inspectedType.BaseType;
            }

            foreach (var type in buttonsByType.Keys.OrderBy(GetInheritanceDepth))
            {
                DrawButtonsForType(buttonsByType[type]);
            }
        }
        
        private bool HasAnyButtons()
        {
            var inspectedType = target.GetType();
            while (inspectedType != null && inspectedType != typeof(MonoBehaviour) &&
                   inspectedType != typeof(ScriptableObject))
            {
                if (inspectedType.GetMethods(
                        BindingFlags.Instance | BindingFlags.Static |
                        BindingFlags.Public | BindingFlags.NonPublic |
                        BindingFlags.DeclaredOnly)
                    .Any(m => m.GetCustomAttribute<ButtonAttribute>() != null))
                {
                    return true;
                }

                inspectedType = inspectedType.BaseType;
            }

            if (!ButtonSettings.Instance.DrawNestedButtons) return false;

            foreach (var field in target.GetType().GetFields(
                         BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!field.IsSerializedByUnity()) continue;

                var value = field.GetValue(target);
                if (value == null) continue;

                if (value is System.Collections.IList list)
                {
                    if (list.Cast<object>().Any(e => e != null && e.HasButtonMethods())) return true;
                }
                else if (value is Array array)
                {
                    if (array.Cast<object>().Any(e => e != null && e.HasButtonMethods())) return true;
                }
                else if (value.HasButtonMethods())
                {
                    return true;
                }
            }

            return false;
        }

        private bool ValidateMethod(MethodInfo method)
        {
            var unsupported = method.GetParameters()
                .Where(p => !p.ParameterType.IsButtonSupportedType())
                .Select(p => $"{p.Name} ({p.ParameterType.Name})")
                .ToList();

            if (unsupported.Count == 0) return true;

            string key = $"{target.GetType().Name}.{method.Name}";
            if (_loggedValidationErrors.Add(key))
            {
                Debug.LogWarning(
                    $"[Button] '{method.Name}' in '{target.GetType().Name}' has unsupported parameter types and will not be shown: " +
                    $"{string.Join(", ", unsupported)}. " +
                    $"Supported types: primitives, vectors, colors, Unity Objects, enums, curves, gradients.",
                    target);
            }
            return false;
        }

        private bool ValidateNestedMethod(MethodInfo method, string fieldPath)
        {
            var unsupported = method.GetParameters()
                .Where(p => !p.ParameterType.IsAssignableFrom(target.GetType()) && !p.ParameterType.IsButtonSupportedType())
                .Select(p => $"{p.Name} ({p.ParameterType.Name})")
                .ToList();

            if (unsupported.Count == 0) return true;

            string key = $"{fieldPath}.{method.Name}";
            if (_loggedValidationErrors.Add(key))
            {
                Debug.LogWarning(
                    $"[Button] '{method.Name}' on nested field '{fieldPath}' has unsupported parameters: " +
                    $"{string.Join(", ", unsupported)}.",
                    target);
            }
            return false;
        }

        private void DrawButtonsForType(List<ButtonInfo> buttonInfos)
        {
            var groups = buttonInfos
                .GroupBy(b => string.IsNullOrEmpty(b.Attribute.Group) ? "" : b.Attribute.Group)
                .OrderBy(g => g.Key);

            foreach (var group in groups)
            {
                if (string.IsNullOrEmpty(group.Key))
                {
                    foreach (var buttonInfo in group.OrderBy(b => b.Method.Name))
                    {
                        DrawButton(buttonInfo);
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

            if (!_groupFoldoutStates[groupKey]) return;

            EditorGUI.indentLevel++;
            foreach (var buttonInfo in buttons.OrderBy(b => b.Method.Name))
            {
                DrawButton(buttonInfo, isInGroup: true);
            }
            EditorGUI.indentLevel--;
            GUILayout.Space(3);
        }

        /// <summary>
        /// Draws an individual button with parameter support and play mode validation.
        /// Injectable parameters (assignable from the parent component) are resolved automatically.
        /// </summary>
        private void DrawButton(ButtonInfo buttonInfo, bool isInGroup = false)
        {
            var method = buttonInfo.Method;
            var attr = buttonInfo.Attribute;
            var methodKey = buttonInfo.MethodKey;

            int actualHeight = attr.Height >= 0 ? attr.Height : ButtonSettings.Instance.ButtonHeight;
            int actualSpace = attr.Space >= 0 ? attr.Space : ButtonSettings.Instance.ButtonSpace;
            ButtonPlayMode actualPlayMode = attr.PlayMode != ButtonPlayMode.UseDefault ? attr.PlayMode : ButtonSettings.Instance.ButtonPlayMode;
            Color actualColor = attr.Color != Color.clear ? attr.Color : ButtonSettings.Instance.ButtonColor;

            if (isInGroup && actualSpace > 0)
            {
                actualSpace = Math.Max(1, actualSpace - 2);
            }

            if (actualSpace > 0)
            {
                GUILayout.Space(actualSpace);
            }

            string buttonText = string.IsNullOrEmpty(attr.Name)
                ? ObjectNames.NicifyVariableName(method.Name)
                : attr.Name;

            bool shouldDisable = false;
            if (actualPlayMode == ButtonPlayMode.OnlyWhenPlaying)
            {
                shouldDisable = !Application.isPlaying;
                if (shouldDisable) buttonText += "\n(Play Mode Only)";
            }
            else if (actualPlayMode == ButtonPlayMode.OnlyWhenNotPlaying)
            {
                shouldDisable = Application.isPlaying;
                if (shouldDisable) buttonText += "\n(Edit Mode Only)";
            }

            var allParameters = method.GetParameters();
            var visibleParameters = allParameters
                .Where(p => !p.ParameterType.IsAssignableFrom(target.GetType()))
                .ToArray();

            if (!_methodParameters.ContainsKey(methodKey))
            {
                _methodParameters[methodKey] = new object[visibleParameters.Length];
                for (int i = 0; i < visibleParameters.Length; i++)
                {
                    _methodParameters[methodKey][i] = visibleParameters[i].GetDefaultValue();
                }
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
                {
                    _foldoutStates[methodKey] = newFoldout;
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
                InvokeButton(buttonInfo, allParameters, visibleParameters, methodKey);
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
                        visibleParameters[i]);
                }
                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel--;
            }
        }

        private void InvokeButton(ButtonInfo buttonInfo, ParameterInfo[] allParameters, ParameterInfo[] visibleParameters, string methodKey)
        {
            object invokeTarget = buttonInfo.InvokeTarget ?? target;

            if (!buttonInfo.Method.IsStatic)
            {
                Undo.RecordObject(target, $"Button: {buttonInfo.Method.Name}");
            }

            try
            {
                int visibleIndex = 0;
                object[] resolvedParams = new object[allParameters.Length];
                for (int i = 0; i < allParameters.Length; i++)
                {
                    if (allParameters[i].ParameterType.IsAssignableFrom(target.GetType()))
                    {
                        resolvedParams[i] = target;
                    }
                    else
                    {
                        resolvedParams[i] = _methodParameters[methodKey][visibleIndex++];
                    }
                }

                buttonInfo.Method.Invoke(invokeTarget, resolvedParams);

                if (!Application.isPlaying && target != null)
                {
                    EditorUtility.SetDirty(target);
                }
            }
            catch (TargetInvocationException e)
            {
                Exception inner = e.InnerException ?? e;
                Debug.LogError(
                    $"[Button] Error invoking '{buttonInfo.Method.Name}' on '{target.name}': {inner.GetType().Name}: {inner.Message}\n" +
                    $"Parameters used: {FormatParameters(_methodParameters[methodKey])}\n" +
                    $"Stack trace:\n{inner.StackTrace}",
                    target);
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[Button] Unexpected error invoking '{buttonInfo.Method.Name}' on '{target.name}': {e.GetType().Name}: {e.Message}",
                    target);
            }
        }

        /// <summary>
        /// Draws the appropriate GUI field for a method parameter based on its type.
        /// </summary>
        private object DrawParameterField(string paramName, Type paramType, object currentValue, ParameterInfo paramInfo = null)
        {
            string niceName = ObjectNames.NicifyVariableName(paramName);
            RangeAttribute rangeAttr = paramInfo?.GetCustomAttribute<RangeAttribute>();

            if (paramType == typeof(int))
            {
                return rangeAttr != null
                    ? EditorGUILayout.IntSlider(niceName, currentValue != null ? (int)currentValue : 0, (int)rangeAttr.min, (int)rangeAttr.max)
                    : EditorGUILayout.IntField(niceName, currentValue != null ? (int)currentValue : 0);
            }

            if (paramType == typeof(float))
            {
                return rangeAttr != null
                    ? EditorGUILayout.Slider(niceName, currentValue != null ? (float)currentValue : 0f, rangeAttr.min, rangeAttr.max)
                    : EditorGUILayout.FloatField(niceName, currentValue != null ? (float)currentValue : 0f);
            }

            if (paramType == typeof(double)) { return EditorGUILayout.DoubleField(niceName, currentValue != null ? (double)currentValue : 0.0); }
            if (paramType == typeof(long)) { return EditorGUILayout.LongField(niceName, currentValue != null ? (long)currentValue : 0L); }
            if (paramType == typeof(string)) { return EditorGUILayout.TextField(niceName, currentValue != null ? (string)currentValue : ""); }
            if (paramType == typeof(bool)) { return EditorGUILayout.Toggle(niceName, currentValue != null && (bool)currentValue); }
            if (paramType == typeof(Vector2)) { return EditorGUILayout.Vector2Field(niceName, currentValue != null ? (Vector2)currentValue : Vector2.zero); }
            if (paramType == typeof(Vector3)) { return EditorGUILayout.Vector3Field(niceName, currentValue != null ? (Vector3)currentValue : Vector3.zero); }
            if (paramType == typeof(Vector4)) { return EditorGUILayout.Vector4Field(niceName, currentValue != null ? (Vector4)currentValue : Vector4.zero); }
            if (paramType == typeof(Vector2Int)) { return EditorGUILayout.Vector2IntField(niceName, currentValue != null ? (Vector2Int)currentValue : Vector2Int.zero); }
            if (paramType == typeof(Vector3Int)) { return EditorGUILayout.Vector3IntField(niceName, currentValue != null ? (Vector3Int)currentValue : Vector3Int.zero); }
            if (paramType == typeof(Color)) { return EditorGUILayout.ColorField(niceName, currentValue != null ? (Color)currentValue : Color.white); }
            if (paramType == typeof(Color32))
            {
                Color32 c = currentValue != null ? (Color32)currentValue : (Color32)Color.white;
                return (Color32)EditorGUILayout.ColorField(niceName, c);
            }
            if (paramType == typeof(Rect)) { return EditorGUILayout.RectField(niceName, currentValue != null ? (Rect)currentValue : new Rect(0, 0, 100, 100)); }
            if (paramType == typeof(RectInt)) { return EditorGUILayout.RectIntField(niceName, currentValue != null ? (RectInt)currentValue : new RectInt(0, 0, 100, 100)); }
            if (paramType == typeof(Bounds)) { return EditorGUILayout.BoundsField(niceName, currentValue != null ? (Bounds)currentValue : new Bounds()); }
            if (paramType == typeof(BoundsInt)) { return EditorGUILayout.BoundsIntField(niceName, currentValue != null ? (BoundsInt)currentValue : new BoundsInt()); }
            if (paramType == typeof(AnimationCurve)) { return EditorGUILayout.CurveField(niceName, currentValue != null ? (AnimationCurve)currentValue : AnimationCurve.Linear(0, 0, 1, 1)); }
            if (paramType == typeof(Gradient)) { return EditorGUILayout.GradientField(niceName, currentValue != null ? (Gradient)currentValue : new Gradient()); }
            if (paramType == typeof(LayerMask)) { return EditorGUILayout.MaskField(niceName, currentValue != null ? (LayerMask)currentValue : (LayerMask)0, UnityEditorInternal.InternalEditorUtility.layers); }
            if (paramType.IsEnum) { return EditorGUILayout.EnumPopup(niceName, currentValue != null ? (Enum)currentValue : (Enum)Enum.GetValues(paramType).GetValue(0)); }
            if (typeof(UnityEngine.Object).IsAssignableFrom(paramType)) { return EditorGUILayout.ObjectField(niceName, (UnityEngine.Object)currentValue, paramType, true); }
            if (paramType.IsArray && paramType.GetElementType() == typeof(string))
            {
                string[] array = (string[])currentValue ?? Array.Empty<string>();
                EditorGUILayout.LabelField($"{niceName} (String Array)");
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

            EditorGUILayout.HelpBox($"Unsupported type: {paramType.Name}", MessageType.Error);
            return currentValue;
        }

        private static int GetInheritanceDepth(Type type)
        {
            int depth = 0;
            var current = type;
            while (current != null && current != typeof(MonoBehaviour) && current != typeof(ScriptableObject))
            {
                depth++;
                current = current.BaseType;
            }
            return depth;
        }

        private static string FormatParameters(object[] parameters)
        {
            if (parameters == null || parameters.Length == 0) return "none";
            return string.Join(", ", parameters.Select((p, i) => $"[{i}] = {(p != null ? p.ToString() : "null")}"));
        }
    }

    /// <summary>
    /// Custom editor for MonoBehaviour classes that adds button functionality.
    /// </summary>
    [CustomEditor(typeof(MonoBehaviour), true)]
    internal class ButtonAttributeEditor : BaseButtonAttributeEditor { }

    /// <summary>
    /// Custom editor for ScriptableObject classes that adds button functionality.
    /// </summary>
    [CustomEditor(typeof(ScriptableObject), true)]
    internal class ButtonAttributeScriptableObjectEditor : BaseButtonAttributeEditor { }
}