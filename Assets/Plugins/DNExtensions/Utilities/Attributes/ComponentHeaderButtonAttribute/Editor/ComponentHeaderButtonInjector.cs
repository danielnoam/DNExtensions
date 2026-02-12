using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DNExtensions.Utilities {
    
    /// <summary>
    /// Scans for methods marked with [ComponentHeaderButton] and injects a button bar below component headers.
    /// Uses UI Toolkit to insert elements without modifying Unity's default header.
    /// </summary>
    [InitializeOnLoad]
    internal static class ComponentHeaderButtonInjector {
        private const string ButtonBarName = "DNExtensions_ButtonBar";
        private const string InspectorListClassName = "unity-inspector-editors-list";

        private static readonly StyleLength ButtonBarPaddingLeft = 15;
        private static readonly StyleLength ButtonBarPaddingRight = 5;
        private static readonly StyleLength ButtonBarPaddingVertical = 2;
        private static readonly StyleLength ButtonBarMarginBottom = 2;

        private static readonly StyleLength ButtonMinWidth = 24;
        private static readonly StyleLength ButtonHeight = 18;
        private static readonly StyleLength ButtonFontSize = 11;
        private static readonly StyleLength ButtonPadding = 0;
        private static readonly StyleLength ButtonMargin = 1;

        private static Dictionary<Type, List<ButtonData>> _buttonsByType;
        private static readonly List<Func<Component, ButtonData>> DynamicButtonProviders = new List<Func<Component, ButtonData>>();
        private static readonly Dictionary<int, int> LastButtonHash = new Dictionary<int, int>();
        private static EditorWindow _currentInspector;
        private static VisualElement _editorList;
        
        static ComponentHeaderButtonInjector() {
            BuildButtonDatabase();
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }
        
        /// <summary>
        /// Registers a dynamic button provider that can add buttons to components at runtime.
        /// The provider function receives a component and returns a ButtonData or null.
        /// Multiple providers can be registered and all will be called for each component.
        /// </summary>
        public static void RegisterDynamicButtonProvider(Func<Component, ButtonData> provider) {
            if (provider != null && !DynamicButtonProviders.Contains(provider)) {
                DynamicButtonProviders.Add(provider);
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state) {
            // Force removal of all button bars before clearing references
            if (_editorList != null) {
                foreach (var child in _editorList.Children()) {
                    child.Q(ButtonBarName)?.RemoveFromHierarchy();
                }
            }
            
            _currentInspector = null;
            _editorList = null;
        }
        
        /// <summary>
        /// Scans all assemblies for methods with [ComponentHeaderButton] attribute.
        /// Builds a lookup dictionary grouped by declaring type and sorted by priority.
        /// </summary>
        private static void BuildButtonDatabase() {
            _buttonsByType = new Dictionary<Type, List<ButtonData>>();
            
            var methods = TypeCache.GetMethodsWithAttribute<ComponentHeaderButtonAttribute>();
            
            foreach (var method in methods) {
                if (!IsValidButtonMethod(method, out string error)) {
                    Debug.LogWarning($"[ComponentHeaderButton] Invalid method '{method.DeclaringType?.Name}.{method.Name}': {error}");
                    continue;
                }
                
                var attribute = method.GetCustomAttribute<ComponentHeaderButtonAttribute>();
                var buttonData = new ButtonData {
                    Method = method,
                    Icon = attribute.Icon,
                    Tooltip = attribute.Tooltip,
                    Priority = attribute.Priority,
                    IsStatic = method.IsStatic
                };
                
                if (!_buttonsByType.ContainsKey(method.DeclaringType))
                    _buttonsByType[method.DeclaringType] = new List<ButtonData>();
                
                _buttonsByType[method.DeclaringType].Add(buttonData);
            }
            
            foreach (var list in _buttonsByType.Values)
                list.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        }
        
        /// <summary>
        /// Validates that a method can be used as a header button.
        /// Must be void with no parameters.
        /// </summary>
        private static bool IsValidButtonMethod(MethodInfo method, out string error) {
            error = null;
            
            if (method.ReturnType != typeof(void)) {
                error = "Method must return void";
                return false;
            }
            
            if (method.GetParameters().Length > 0) {
                error = "Method must have no parameters";
                return false;
            }
            
            return true;
        }

        private static void Update() {
            if (!TryGetInspector(out EditorWindow inspector)) return;
            
            if (inspector != _currentInspector) {
                _currentInspector = inspector;
                _editorList = null;
            }
            
            _editorList ??= _currentInspector.rootVisualElement.Q(null, InspectorListClassName);
            if (_editorList == null) return;
            
            InjectButtons();
        }

        private static void InjectButtons() {
            if (Selection.activeGameObject == null) return;
            
            var components = Selection.activeGameObject.GetComponents<Component>();
            
            foreach (var component in components) {
                if (component == null) continue;
                
                List<ButtonData> buttons = new List<ButtonData>();
                
                // Call all registered dynamic button providers
                foreach (var provider in DynamicButtonProviders) {
                    try {
                        var buttonData = provider(component);
                        if (buttonData != null) {
                            buttons.Add(buttonData);
                        }
                    }
                    catch (Exception e) {
                        Debug.LogError($"Dynamic button provider failed: {e.Message}");
                    }
                }
                
                // Add attribute-based buttons
                if (_buttonsByType.TryGetValue(component.GetType(), out var attributeButtons)) {
                    buttons.AddRange(attributeButtons);
                }
                
                if (buttons.Count == 0) continue;
                
                buttons.Sort((a, b) => a.Priority.CompareTo(b.Priority));
                
                if (!TryFindEditorElements(component, out VisualElement editorElement, out IMGUIContainer header)) 
                    continue;
                
                VisualElement existingBar = editorElement.Q(ButtonBarName);
                
                // Check if we need to rebuild (button state changed or bar is stale)
                bool needsRebuild = false;
                if (existingBar == null || existingBar.panel == null) {
                    needsRebuild = true;
                } else {
                    int instanceId = component.GetInstanceID();
                    int currentHash = GetButtonHash(buttons);
                    
                    if (LastButtonHash.TryGetValue(instanceId, out int lastHash)) {
                        if (currentHash != lastHash) {
                            needsRebuild = true;
                        }
                    } else {
                        needsRebuild = true;
                    }
                    
                    LastButtonHash[instanceId] = currentHash;
                }
                
                if (!needsRebuild) continue;
                
                existingBar?.RemoveFromHierarchy();
                
                VisualElement buttonBar = CreateButtonBar(component, buttons);
                
                int headerIndex = editorElement.IndexOf(header);
                editorElement.Insert(headerIndex + 1, buttonBar);
            }
        }

        private static bool TryFindEditorElements(Component component, out VisualElement editorElement, out IMGUIContainer header) {
            editorElement = null;
            header = null;
            
            if (_editorList == null) return false;
            
            foreach (var child in _editorList.Children()) {
                if (child.GetType().Name != "EditorElement") continue;
                
                IMGUIContainer foundHeader = null;
                Editor foundEditor = null;
                
                foreach (var subChild in child.Children()) {
                    if (subChild is IMGUIContainer imgui && subChild.name.EndsWith("Header")) {
                        foundHeader = imgui;
                    }
                    
                    if (subChild.GetType().Name == "InspectorElement") {
                        var editor = subChild.GetType()
                            .GetField("m_Editor", BindingFlags.Instance | BindingFlags.NonPublic)
                            ?.GetValue(subChild) as Editor;
                        
                        if (editor?.target == component) {
                            foundEditor = editor;
                        }
                    }
                }
                
                if (foundHeader != null && foundEditor != null) {
                    editorElement = child;
                    header = foundHeader;
                    return true;
                }
            }
            
            return false;
        }

        private static VisualElement CreateButtonBar(Component component, List<ButtonData> buttons) {
            int componentInstanceID = component.GetInstanceID();
            
            var container = new VisualElement {
                name = ButtonBarName,
                style = {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    paddingLeft = ButtonBarPaddingLeft,
                    paddingRight = ButtonBarPaddingRight,
                    paddingTop = ButtonBarPaddingVertical,
                    paddingBottom = ButtonBarPaddingVertical,
                    marginBottom = ButtonBarMarginBottom
                }
            };
            
            foreach (var buttonData in buttons) {
                var button = new UnityEngine.UIElements.Button(() => {
                    var currentComponent = UnityEditor.EditorUtility.EntityIdToObject(componentInstanceID) as Component;
                    if (currentComponent != null) {
                        InvokeMethod(currentComponent, buttonData);
                    }
                }) {
                    text = buttonData.Icon,
                    tooltip = buttonData.Tooltip
                };
                
                StyleButton(button);
                buttonData.StyleCallback?.Invoke(button);
                container.Add(button);
            }
            
            return container;
        }

        private static void StyleButton(UnityEngine.UIElements.Button button) {
            button.style.minWidth = ButtonMinWidth;
            button.style.height = ButtonHeight;
            button.style.fontSize = ButtonFontSize;
            button.style.paddingLeft = ButtonPadding;
            button.style.paddingRight = ButtonPadding;
            button.style.paddingTop = ButtonPadding;
            button.style.paddingBottom = ButtonPadding;
            button.style.marginLeft = ButtonMargin;
            button.style.marginRight = ButtonMargin;
            button.style.unityTextAlign = TextAnchor.MiddleCenter;
        }

        private static void InvokeMethod(Component component, ButtonData buttonData) {
            if (component == null) return;
            
            // Priority: callback over method
            if (buttonData.Callback != null) {
                try {
                    buttonData.Callback(component);
                }
                catch (Exception e) {
                    var innerException = e.InnerException ?? e;
                    Debug.LogError($"ComponentHeaderButton callback failed: {innerException.Message}", component);
                }
                return;
            }
            
            if (buttonData.Method == null) return;
            
            try {
                if (buttonData.IsStatic)
                    buttonData.Method.Invoke(null, null);
                else
                    buttonData.Method.Invoke(component, null);
            }
            catch (Exception e) {
                var innerException = e.InnerException ?? e;
                Debug.LogError($"ComponentHeaderButton '{buttonData.Method.Name}' failed: {innerException.Message}", component);
            }
        }

        private static bool TryGetInspector(out EditorWindow inspector) {
            inspector = EditorWindow.focusedWindow;
            
            if (inspector != null && inspector.GetType().Name == "InspectorWindow") {
                return true;
            }
            
            var inspectors = Resources.FindObjectsOfTypeAll<EditorWindow>();
            foreach (var window in inspectors) {
                if (window.GetType().Name == "InspectorWindow") {
                    inspector = window;
                    return true;
                }
            }
            
            inspector = null;
            return false;
        }

        private static int GetButtonHash(List<ButtonData> buttons) {
            unchecked {
                int hash = 17;
                foreach (var button in buttons) {
                    hash = hash * 31 + (button.Icon?.GetHashCode() ?? 0);
                    hash = hash * 31 + (button.Tooltip?.GetHashCode() ?? 0);
                    hash = hash * 31 + button.Priority;
                }
                return hash;
            }
        }
        
        public class ButtonData {
            public MethodInfo Method;
            public Action<Component> Callback;
            public Action<UnityEngine.UIElements.Button> StyleCallback;
            public string Icon;
            public string Tooltip;
            public int Priority;
            public bool IsStatic;
        }
    }
}