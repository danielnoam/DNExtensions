#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DNExtensions.Utilities {
    
    [InitializeOnLoad]
    public static class ComponentHeaderButtonInjector {
        
        private const string InspectorListClassName = "unity-inspector-editors-list";
        private const string InspectorTitlebarClassName = "unity-inspector-titlebar-foldout";
        private const string ButtonContainerName = "DNExtensions_HeaderButtons";
        
        private static EditorWindow currentInspector;
        private static VisualElement editorList;
        private static readonly Dictionary<int, List<ButtonInfo>> CachedButtons = new Dictionary<int, List<ButtonInfo>>();
        
        private class ButtonInfo {
            public string Icon;
            public string Tooltip;
            public int Priority;
            public MethodInfo Method;
            public Component Target;
        }
        
        static ComponentHeaderButtonInjector() {
            EditorApplication.update += Update;
            AssemblyReloadEvents.beforeAssemblyReload += ClearCache;
        }
        
        private static void Update() {
            if (!TryGetInspector(out EditorWindow inspector)) return;
            
            if (inspector != currentInspector) {
                currentInspector = inspector;
                editorList = null;
            }
            
            editorList ??= currentInspector.rootVisualElement.Q(null, InspectorListClassName);
            if (editorList == null) return;
            
            InjectButtons();
        }
        
        private static void InjectButtons() {
            if (Selection.activeGameObject == null) return;
            
            
            Component[] components = Selection.activeGameObject.GetComponents<Component>();
            
            foreach (Component comp in components) {
                if (comp == null) continue;
                
                int instanceId = comp.GetInstanceID();
                List<ButtonInfo> buttons = GetButtonsForComponent(comp);
                
                if (buttons.Count == 0) continue;
                
                VisualElement header = FindHeaderForComponent(instanceId);
                if (header == null) continue;
                
                VisualElement existingContainer = header.Q(ButtonContainerName);
                if (existingContainer != null) continue;
                
                VisualElement buttonContainer = CreateButtonContainer(buttons);
                header.Add(buttonContainer);
            }
        }
        
        public static void RegisterButton(Component comp, string icon, string tooltip, int priority, Action callback) {
            int instanceId = comp.GetInstanceID();
            
            if (!CachedButtons.ContainsKey(instanceId)) {
                CachedButtons[instanceId] = new List<ButtonInfo>();
            }
            
            CachedButtons[instanceId].Add(new ButtonInfo {
                Icon = icon,
                Tooltip = tooltip,
                Priority = priority,
                Method = null,
                Target = comp
            });
            
            CachedButtons[instanceId] = CachedButtons[instanceId].OrderBy(b => b.Priority).ToList();
        }
        
        private static List<ButtonInfo> GetButtonsForComponent(Component comp) {
            int instanceId = comp.GetInstanceID();
            
            if (CachedButtons.TryGetValue(instanceId, out List<ButtonInfo> cached)) {
                return cached;
            }
            
            List<ButtonInfo> buttons = new List<ButtonInfo>();
            Type type = comp.GetType();
            
            MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            
            foreach (MethodInfo method in methods) {
                object[] attrs = method.GetCustomAttributes(typeof(ComponentHeaderButtonAttribute), true);
                
                foreach (object attr in attrs) {
                    if (attr is ComponentHeaderButtonAttribute buttonAttr) {
                        buttons.Add(new ButtonInfo {
                            Icon = buttonAttr.Icon,
                            Tooltip = string.IsNullOrEmpty(buttonAttr.Tooltip) ? method.Name : buttonAttr.Tooltip,
                            Priority = buttonAttr.Priority,
                            Method = method,
                            Target = comp
                        });
                    }
                }
            }
            
            buttons = buttons.OrderBy(b => b.Priority).ToList();
            CachedButtons[instanceId] = buttons;
            
            return buttons;
        }
        
        private static VisualElement CreateButtonContainer(List<ButtonInfo> buttons) {
            VisualElement container = new VisualElement {
                name = ButtonContainerName
            };
            container.style.flexDirection = FlexDirection.Row;
            container.style.position = Position.Absolute;
            container.style.right = 20;
            container.style.top = 2;
            container.pickingMode = PickingMode.Position; // Allow clicks through to header
    
            foreach (ButtonInfo buttonInfo in buttons) {
                UnityEngine.UIElements.Button button = new UnityEngine.UIElements.Button(() => InvokeMethod(buttonInfo));
                button.text = buttonInfo.Icon;
                button.tooltip = buttonInfo.Tooltip;
        
                // Fix button size and text
                button.style.width = 18;
                button.style.height = 16;
                button.style.minWidth = 18;
                button.style.minHeight = 16;
                button.style.maxWidth = 18;
                button.style.maxHeight = 16;
        
                // Fix padding and margins
                button.style.marginLeft = 1;
                button.style.marginRight = 1;
                button.style.marginTop = 0;
                button.style.marginBottom = 0;
                button.style.paddingLeft = 0;
                button.style.paddingRight = 0;
                button.style.paddingTop = 0;
                button.style.paddingBottom = 0;
        
                // Fix text sizing
                button.style.fontSize = 10;
                button.style.unityTextAlign = TextAnchor.MiddleCenter;
        
                // Remove border if it's visible
                button.style.borderLeftWidth = 0;
                button.style.borderRightWidth = 0;
                button.style.borderTopWidth = 0;
                button.style.borderBottomWidth = 0;
        
                container.Add(button);
            }
    
            return container;
        }
        
        private static void InvokeMethod(ButtonInfo buttonInfo) {
            if (buttonInfo.Target == null) return;
            if (buttonInfo.Method == null) return;
            
            MethodInfo currentMethod = buttonInfo.Target.GetType()
                .GetMethod(buttonInfo.Method.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            
            if (currentMethod == null) return;
            
            try {
                currentMethod.Invoke(buttonInfo.Target, null);
            }
            catch (Exception e) {
                Debug.LogError($"Error invoking {buttonInfo.Method.Name}: {e.Message}");
            }
        }
        
        private static VisualElement FindHeaderForComponent(int instanceId) {
            foreach (VisualElement child in editorList.Children()) {
                // Check if this element's name contains the instance ID
                if (child.name.Contains(instanceId.ToString())) {
                    return child;
                }
            }
    
            return null;
        }

        
        private static bool TryGetInspector(out EditorWindow inspector) {
            inspector = EditorWindow.focusedWindow;
            
            if (inspector != null && inspector.GetType().Name == "InspectorWindow") {
                return true;
            }
            
            UnityEngine.Object[] inspectors = Resources.FindObjectsOfTypeAll(typeof(EditorWindow));
            foreach (UnityEngine.Object obj in inspectors) {
                if (obj.GetType().Name == "InspectorWindow") {
                    inspector = (EditorWindow)obj;
                    return true;
                }
            }
            
            inspector = null;
            return false;
        }
        
        private static void ClearCache() {
            CachedButtons.Clear();
        }

        public static void ClearButtonsForComponent(int instanceId) {
            CachedButtons.Remove(instanceId);
        }
    }
}

#endif