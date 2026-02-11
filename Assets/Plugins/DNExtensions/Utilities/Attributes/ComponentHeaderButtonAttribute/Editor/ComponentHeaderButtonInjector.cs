#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DNExtensions.Utilities {
    
    /// <summary>
    /// Injects custom buttons into Unity component headers using UI Toolkit.
    /// Supports both attribute-based buttons (<see cref="ComponentHeaderButtonAttribute"/>) and
    /// runtime-registered buttons via <see cref="RegisterButton"/>.
    /// <para>Only works with default Unity inspectors. Components with CustomEditor won't show injected buttons.</para>
    /// </summary>
    [InitializeOnLoad]
    public static class ComponentHeaderButtonInjector {
        
        private const string InspectorListClassName = "unity-inspector-editors-list";
        private const string ButtonContainerName = "DNExtensions_HeaderButtons";
        
        private static EditorWindow currentInspector;
        private static VisualElement editorList;
        private static readonly Dictionary<int, List<ButtonInfo>> CachedButtons = new Dictionary<int, List<ButtonInfo>>();
        
        /// <summary>
        /// Tracks the injected wrapper element and button count per component.
        /// The wrapper reference lets us detect when Unity has rebuilt the inspector
        /// (the wrapper becomes detached from the panel), so we know to re-inject.
        /// </summary>
        private static readonly Dictionary<int, InjectionState> InjectedState = new Dictionary<int, InjectionState>();
        
        private struct InjectionState {
            public int ButtonCount;
            public VisualElement Wrapper;
        }
        
        private class ButtonInfo {
            public string Icon;
            public string Tooltip;
            public int Priority;
            public MethodInfo Method;
            public Component Target;
            public Action Callback;
            public UnityEngine.UIElements.Button UIButton;
        }
        
        static ComponentHeaderButtonInjector() {
            EditorApplication.update += Update;
            AssemblyReloadEvents.beforeAssemblyReload += ClearCache;
        }
        
        /// <summary>
        /// Registers a button to appear in the component's header.
        /// If a button with the same priority already exists, it updates the existing button in place.
        /// </summary>
        /// <param name="comp">Target component to attach the button to.</param>
        /// <param name="icon">Unicode icon character displayed on the button.</param>
        /// <param name="tooltip">Tooltip text shown on hover.</param>
        /// <param name="priority">Sort order. Lower numbers appear first (left to right).</param>
        /// <param name="callback">Action invoked when the button is clicked.</param>
        /// <param name="buttonReference">
        /// Reference to the created UI button element, or null if the UI hasn't been built yet.
        /// </param>
        public static void RegisterButton(Component comp, string icon, string tooltip, int priority, Action callback, out UnityEngine.UIElements.Button buttonReference) {
            int instanceId = comp.GetInstanceID();
            
            if (!CachedButtons.ContainsKey(instanceId)) {
                CachedButtons[instanceId] = new List<ButtonInfo>();
            }
            
            ButtonInfo existing = CachedButtons[instanceId].FirstOrDefault(b => b.Priority == priority);
            
            if (existing != null) {
                existing.Icon = icon;
                existing.Tooltip = tooltip;
                existing.Callback = callback;
                
                if (existing.UIButton != null) {
                    existing.UIButton.text = icon;
                    existing.UIButton.tooltip = tooltip;
                }
                
                buttonReference = existing.UIButton;
            }
            else {
                ButtonInfo newButton = new ButtonInfo {
                    Icon = icon,
                    Tooltip = tooltip,
                    Priority = priority,
                    Method = null,
                    Target = comp,
                    Callback = callback,
                    UIButton = null
                };
                
                CachedButtons[instanceId].Add(newButton);
                CachedButtons[instanceId] = CachedButtons[instanceId].OrderBy(b => b.Priority).ToList();
                
                // Invalidate so the UI rebuilds on next update.
                InjectedState.Remove(instanceId);
                
                buttonReference = null;
            }
        }
        
        /// <summary>
        /// Removes a button with the specified priority from a component's cache.
        /// The UI will be rebuilt automatically on the next injection cycle.
        /// </summary>
        /// <param name="instanceId">Instance ID of the target component.</param>
        /// <param name="priority">Priority value of the button to remove.</param>
        public static void RemoveButton(int instanceId, int priority) {
            if (CachedButtons.TryGetValue(instanceId, out var buttons)) {
                buttons.RemoveAll(b => b.Priority == priority);
                
                // Invalidate so the injector rebuilds on next update.
                InjectedState.Remove(instanceId);
            }
        }
        
        /// <summary>
        /// Clears all cached buttons for a component (including attribute-discovered ones).
        /// The cache will be re-populated from attributes on the next injection cycle.
        /// </summary>
        /// <param name="instanceId">Instance ID of the target component.</param>
        public static void ClearButtonsForComponent(int instanceId) {
            CachedButtons.Remove(instanceId);
            InjectedState.Remove(instanceId);
        }
        
        private static void Update() {
            if (!TryGetInspector(out EditorWindow inspector)) return;
            
            if (inspector != currentInspector) {
                currentInspector = inspector;
                editorList = null;
                // Inspector window changed — all injected UI is gone.
                InjectedState.Clear();
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
        
                VisualElement editorElement = FindHeaderForComponent(instanceId);
                if (editorElement == null) continue;
                
                // Check if current injection is still valid:
                // - Wrapper must still be attached to a panel (not stale from inspector rebuild).
                // - Button count must match (no buttons added or removed since last inject).
                bool uiIsValid = false;
                if (InjectedState.TryGetValue(instanceId, out InjectionState state)) {
                    uiIsValid = state.Wrapper != null 
                                && state.Wrapper.panel != null 
                                && state.ButtonCount == buttons.Count;
                }
                
                if (uiIsValid) continue;
                
                // Clean up any existing wrapper (stale or count mismatch).
                RemoveWrapperFromElement(editorElement);
                
                // Clear UIButton references since we're rebuilding.
                foreach (ButtonInfo btn in buttons) {
                    btn.UIButton = null;
                }
                
                if (buttons.Count == 0) {
                    InjectedState.Remove(instanceId);
                    continue;
                }
        
                IMGUIContainer headerContainer = FindHeaderContainer(editorElement);
                if (headerContainer == null) continue;
        
                VisualElement wrapper = WrapHeaderWithButtons(editorElement, headerContainer, buttons);
                InjectedState[instanceId] = new InjectionState {
                    ButtonCount = buttons.Count,
                    Wrapper = wrapper
                };
            }
        }
        
        /// <summary>
        /// Removes the DNExtensions wrapper from an editor element, restoring the original header.
        /// Safe to call even if no wrapper exists.
        /// </summary>
        private static void RemoveWrapperFromElement(VisualElement editorElement) {
            VisualElement existingWrapper = editorElement.Q(ButtonContainerName);
            if (existingWrapper == null) return;
            
            IMGUIContainer header = null;
            foreach (VisualElement element in existingWrapper.Children()) {
                if (element is IMGUIContainer imgui) {
                    header = imgui;
                    break;
                }
            }
            
            int wrapperIndex = editorElement.IndexOf(existingWrapper);
            existingWrapper.RemoveFromHierarchy();
            
            if (header != null) {
                header.style.flexGrow = 0;
                editorElement.Insert(wrapperIndex, header);
            }
        }
        
        private static VisualElement WrapHeaderWithButtons(VisualElement editorElement, IMGUIContainer headerContainer, List<ButtonInfo> buttons) {
            VisualElement wrapper = new VisualElement
            {
                name = ButtonContainerName,
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center
                }
            };

            int headerIndex = editorElement.IndexOf(headerContainer);
            editorElement.Remove(headerContainer);
            wrapper.Add(headerContainer);
            
            headerContainer.style.flexGrow = 1;
            
            VisualElement buttonContainer = CreateButtonContainer(buttons);
            wrapper.Add(buttonContainer);
            
            editorElement.Insert(headerIndex, wrapper);
            
            return wrapper;
        }
        
        private static IMGUIContainer FindHeaderContainer(VisualElement editorElement) {
            foreach (VisualElement child in editorElement.Children()) {
                if (child is IMGUIContainer imgui && child.name.EndsWith("Header")) {
                    return imgui;
                }
            }
            return null;
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
            VisualElement container = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    marginRight = 5,
                    marginLeft = 5
                }
            };

            foreach (ButtonInfo buttonInfo in buttons) {
                UnityEngine.UIElements.Button button = new UnityEngine.UIElements.Button(() => InvokeMethod(buttonInfo)) {
                    text = buttonInfo.Icon,
                    tooltip = buttonInfo.Tooltip
                };
                
                StyleButton(button);
                buttonInfo.UIButton = button;
                container.Add(button);
            }
            
            return container;
        }
        
        private static void StyleButton(UnityEngine.UIElements.Button button) {
            button.style.width = 19;
            button.style.height = 19;
            button.style.minWidth = 19;
            button.style.minHeight = 19;
            button.style.maxWidth = 25;
            button.style.maxHeight = 25;
            button.style.marginLeft = 1;
            button.style.marginRight = 1;
            button.style.marginTop = 0;
            button.style.marginBottom = 0;
            button.style.paddingLeft = 2;
            button.style.paddingRight = 2;
            button.style.paddingTop = 2;
            button.style.paddingBottom = 2;
            button.style.fontSize = 9;
            button.style.unityTextAlign = TextAnchor.MiddleCenter;
            button.style.borderLeftWidth = 0;
            button.style.borderRightWidth = 0;
            button.style.borderTopWidth = 0;
            button.style.borderBottomWidth = 0;
        }
        
        private static void InvokeMethod(ButtonInfo buttonInfo) {
            if (buttonInfo.Target == null) return;
            
            if (buttonInfo.Callback != null) {
                try {
                    buttonInfo.Callback.Invoke();
                }
                catch (Exception e) {
                    Debug.LogError($"Error invoking button callback: {e.Message}");
                }
                return;
            }
            
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
            InjectedState.Clear();
        }
    }
}

#endif