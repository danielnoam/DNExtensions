using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace DNExtensions.HelpfulEditor.GameView
{
    /// <summary>
    /// Buttons of our own in the Game View's toolbar, sitting in space Unity has been asked to leave
    /// for them rather than floating over the top of it.
    ///
    /// That toolbar is IMGUI with no extension point, so the way in is to run inside its layout pass:
    /// the host view's OnGUI delegate is appended to, which puts this code in the same GUILayout pass
    /// as the toolbar and hands it the live layout tree. On the layout event the flexible space in
    /// front of the mute button is pinned to the width of our strip, so Unity lays its own controls out
    /// around the gap; on the repaint event that gap's resolved rect says exactly where the strip goes.
    ///
    /// The alternative — working out where Unity would have put things and drawing there — was what
    /// this replaced. It has to model every conditional control in the row, and when the model is wrong
    /// it does not look wrong, it puts a live button on top of one of Unity's and eats its clicks.
    ///
    /// The reservation trick is the one the OH, SNAP! screenshot tool under Assets/Mirza uses, which is
    /// where it came from.
    /// </summary>
    [InitializeOnLoad]
    internal static class GameViewToolbar
    {
        private const double ScanInterval = 0.5;

        /// <summary>Width of the play mode dropdown, which is how its layout entry is recognised.</summary>
        private const float PlayModeBehaviorWidth = 110f;

        /// <summary>Stands in for the mute/shortcuts/stats/gizmos cluster if the layout cannot be read.</summary>
        private const float FallbackClusterWidth = 190f;

        private const float FallbackToolbarHeight = 21f;

        private const BindingFlags InstanceAny = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags StaticAny = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly List<Registration> Registrations = new List<Registration>();
        private static readonly Dictionary<EditorWindow, Strip> Strips = new Dictionary<EditorWindow, Strip>();
        private static readonly List<EditorWindow> Closed = new List<EditorWindow>();

        private static Type _gameViewType;
        private static FieldInfo _windowParentField;
        private static FieldInfo _hostViewOnGuiField;
        private static PropertyInfo _topLevelProperty;
        private static FieldInfo _groupEntriesField;
        private static FieldInfo _entryMinWidthField;
        private static FieldInfo _entryMaxWidthField;
        private static FieldInfo _entryRectField;
        private static FieldInfo _entryStretchWidthField;
        private static FieldInfo _entryConsideredForMarginField;
        private static PropertyInfo _entryStyleProperty;
        private static Type _groupType;
        private static bool _resolved;

        private static double _nextScan;

        static GameViewToolbar()
        {
            EditorApplication.update -= Scan;
            EditorApplication.update += Scan;

            EditorApplication.delayCall += Scan;

            AssemblyReloadEvents.beforeAssemblyReload -= DisposeAll;
            AssemblyReloadEvents.beforeAssemblyReload += DisposeAll;

            EditorApplication.quitting -= DisposeAll;
            EditorApplication.quitting += DisposeAll;
        }

        /// <summary>
        /// Adds an item to every Game View's strip. The provider is asked once per window and may return
        /// null to sit that one out; items are ordered by priority, lowest first.
        /// </summary>
        public static void RegisterProvider(Func<EditorWindow, GameViewToolbarItem> provider, int priority = 0)
        {
            if (provider == null) return;

            Registrations.Add(new Registration { Provider = provider, Priority = priority });
            Registrations.Sort((a, b) => a.Priority.CompareTo(b.Priority));

            // Providers register from static constructors, which can run after the first scan.
            foreach (Strip strip in Strips.Values) strip.RebuildItems();
        }

        /// <summary>Call after anything that changes what the items show or how wide they are.</summary>
        public static void Refresh()
        {
            foreach (Strip strip in Strips.Values) strip.Repaint();
        }

        private static void Scan()
        {
            if (EditorApplication.timeSinceStartup < _nextScan) return;
            _nextScan = EditorApplication.timeSinceStartup + ScanInterval;

            ResolveReflection();
            if (_gameViewType == null) return;

            Closed.Clear();

            foreach (KeyValuePair<EditorWindow, Strip> pair in Strips)
            {
                if (pair.Key) continue;

                pair.Value.Dispose();
                Closed.Add(pair.Key);
            }

            foreach (EditorWindow closed in Closed) Strips.Remove(closed);

            foreach (Object window in Resources.FindObjectsOfTypeAll(_gameViewType))
            {
                if (!(window is EditorWindow gameView)) continue;

                if (Strips.TryGetValue(gameView, out Strip strip))
                {
                    // Re-checked every scan: a host view holds several tabs and its OnGUI points at
                    // whichever is showing, so docking or switching tab silently drops the hook.
                    strip.UpdateAttachment();
                    continue;
                }

                Strips.Add(gameView, new Strip(gameView));
            }
        }

        private static void DisposeAll()
        {
            foreach (Strip strip in Strips.Values) strip.Dispose();

            Strips.Clear();
        }

        private static void ResolveReflection()
        {
            if (_resolved) return;
            _resolved = true;

            Assembly editor = typeof(Editor).Assembly;

            _gameViewType = editor.GetType("UnityEditor.GameView");

            Type hostViewType = editor.GetType("UnityEditor.HostView");
            _windowParentField = typeof(EditorWindow).GetField("m_Parent", InstanceAny);
            _hostViewOnGuiField = hostViewType?.GetField("m_OnGUI", InstanceAny);

            Assembly imgui = typeof(GUILayoutUtility).Assembly;

            _topLevelProperty = typeof(GUILayoutUtility).GetProperty("topLevel", StaticAny);
            _groupType = imgui.GetType("UnityEngine.GUILayoutGroup");
            Type entryType = imgui.GetType("UnityEngine.GUILayoutEntry");

            _groupEntriesField = _groupType?.GetField("entries", InstanceAny);
            _entryMinWidthField = entryType?.GetField("minWidth", InstanceAny);
            _entryMaxWidthField = entryType?.GetField("maxWidth", InstanceAny);
            _entryRectField = entryType?.GetField("rect", InstanceAny);
            _entryStretchWidthField = entryType?.GetField("stretchWidth", InstanceAny);
            _entryConsideredForMarginField = entryType?.GetField("consideredForMargin", InstanceAny);
            _entryStyleProperty = entryType?.GetProperty("style", InstanceAny);
        }

        /// <summary>Whether the layout can be read and written at all. Without it the strip falls back to floating.</summary>
        private static bool LayoutAvailable =>
            _windowParentField != null && _hostViewOnGuiField != null && _topLevelProperty != null &&
            _groupType != null && _groupEntriesField != null && _entryMinWidthField != null &&
            _entryMaxWidthField != null && _entryRectField != null && _entryStretchWidthField != null &&
            _entryConsideredForMarginField != null && _entryStyleProperty != null;

        /// <summary>
        /// The gap to sit in: the last stretchable entry after the play mode dropdown, which is the
        /// flexible space Unity puts between the middle controls and the mute button. Found by way of
        /// that dropdown rather than by counting, so an extra XR popup or frame debugger button — both
        /// of which come and go with the project — does not move the answer.
        /// </summary>
        private static bool TryFindGap(out object gapEntry, out Rect groupRect)
        {
            gapEntry = null;
            groupRect = default;

            object topLevel = SafeGet(_topLevelProperty);

            return topLevel != null && TryFindGapIn(topLevel, ref gapEntry, ref groupRect);
        }

        private static bool TryFindGapIn(object group, ref object gapEntry, ref Rect groupRect)
        {
            if (!_groupType.IsInstanceOfType(group)) return false;
            if (!(_groupEntriesField.GetValue(group) is IList entries)) return false;

            for (int i = 0; i < entries.Count; i++)
            {
                if (!IsPlayModeDropdown(entries[i])) continue;

                for (int j = entries.Count - 1; j > i; j--)
                {
                    if (!IsFlexibleSpace(entries[j])) continue;

                    gapEntry = entries[j];
                    groupRect = (Rect)_entryRectField.GetValue(group);

                    return true;
                }

                return false;
            }

            foreach (object child in entries)
            {
                if (TryFindGapIn(child, ref gapEntry, ref groupRect)) return true;
            }

            return false;
        }

        /// <summary>
        /// A flexible space, and not merely something that happens to stretch. Unity's toolbar buttons
        /// stretch too — taking the last stretchable entry found the gizmos dropdown, pinned its width
        /// and parked the strip on top of it. A flexible space is the one that also carries no style and
        /// is kept out of margin calculations, which no real control is.
        /// </summary>
        private static bool IsFlexibleSpace(object entry)
        {
            if (_groupType.IsInstanceOfType(entry)) return false;
            if ((int)_entryStretchWidthField.GetValue(entry) == 0) return false;
            if ((bool)_entryConsideredForMarginField.GetValue(entry)) return false;

            GUIStyle style = _entryStyleProperty.GetValue(entry) as GUIStyle;

            return style == null || string.IsNullOrEmpty(style.name);
        }

        private static bool IsPlayModeDropdown(object entry)
        {
            if (_groupType.IsInstanceOfType(entry)) return false;

            float min = (float)_entryMinWidthField.GetValue(entry);
            float max = (float)_entryMaxWidthField.GetValue(entry);

            if (!Mathf.Approximately(min, PlayModeBehaviorWidth) || !Mathf.Approximately(max, PlayModeBehaviorWidth)) return false;

            return _entryStyleProperty.GetValue(entry) is GUIStyle style &&
                   string.Equals(style.name, EditorStyles.toolbarDropDown.name, StringComparison.Ordinal);
        }

        private static object SafeGet(PropertyInfo property)
        {
            try
            {
                return property?.GetValue(null, null);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private sealed class Registration
        {
            public Func<EditorWindow, GameViewToolbarItem> Provider;
            public int Priority;
        }

        /// <summary>One Game View's worth: the strip element, its items, and the hook that places it.</summary>
        private sealed class Strip
        {
            private const string ElementName = "helpfuleditor-gameview-toolbar";

            private readonly EditorWindow _gameView;
            private readonly VisualElement _root;
            private readonly List<GameViewToolbarItem> _items = new List<GameViewToolbarItem>();
            private readonly Delegate _drawDelegate;

            private object _attachedHostView;
            private float _width;

            public Strip(EditorWindow gameView)
            {
                _gameView = gameView;

                VisualElement gameViewRoot = gameView.rootVisualElement;
                gameViewRoot?.Q<VisualElement>(ElementName)?.RemoveFromHierarchy();

                _root = new VisualElement { name = ElementName, pickingMode = PickingMode.Ignore };

                _root.style.position = Position.Absolute;
                _root.style.top = 0f;
                _root.style.height = FallbackToolbarHeight;
                _root.style.flexDirection = FlexDirection.Row;
                _root.style.display = DisplayStyle.None;

                gameViewRoot?.Add(_root);

                if (LayoutAvailable)
                {
                    try
                    {
                        MethodInfo draw = typeof(Strip).GetMethod(nameof(DrawStrip), InstanceAny);
                        _drawDelegate = Delegate.CreateDelegate(_hostViewOnGuiField.FieldType, this, draw);
                    }
                    catch (Exception)
                    {
                        // A changed delegate signature leaves the strip floating rather than absent.
                        _drawDelegate = null;
                    }
                }

                RebuildItems();
                UpdateAttachment();
            }

            public void RebuildItems()
            {
                foreach (GameViewToolbarItem item in _items) item.RemoveFromHierarchy();
                _items.Clear();

                foreach (Registration registration in Registrations)
                {
                    GameViewToolbarItem item = registration.Provider(_gameView);
                    if (item == null) continue;

                    _items.Add(item);
                    _root.Add(item);
                }

                // Without the hook there is no layout pass to place the strip from, so it is anchored to
                // the right edge at the cluster's usual width. Items start at their declared fallback
                // width — enough to be drawn at all, after which each corrects itself to its measured one.
                if (_drawDelegate != null) return;

                foreach (GameViewToolbarItem item in _items) item.style.width = item.FallbackWidth;

                _root.style.left = StyleKeyword.Auto;
                _root.style.right = FallbackClusterWidth;
                _root.style.display = _items.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            }

            public void Repaint()
            {
                if (_gameView) _gameView.Repaint();
            }

            /// <summary>
            /// Keeps our draw call on the host view that is currently showing this Game View, and only
            /// that one. A host view's OnGUI points at whichever of its tabs is on top, so this both
            /// attaches when ours comes forward and gets out of the way when another does.
            /// </summary>
            public void UpdateAttachment()
            {
                if (_drawDelegate == null || !_gameView) return;

                object hostView = _windowParentField.GetValue(_gameView);
                Delegate current = hostView == null ? null : _hostViewOnGuiField.GetValue(hostView) as Delegate;

                if (ReferenceEquals(hostView, _attachedHostView) && HasTarget(current, _gameView) && CountOf(current, _drawDelegate) == 1)
                {
                    return;
                }

                Detach();
                if (hostView == null) return;

                current = _hostViewOnGuiField.GetValue(hostView) as Delegate;
                Delegate cleaned = RemoveAll(current, _drawDelegate);

                if (!HasTarget(cleaned, _gameView))
                {
                    if (!ReferenceEquals(cleaned, current)) _hostViewOnGuiField.SetValue(hostView, cleaned);
                    return;
                }

                _hostViewOnGuiField.SetValue(hostView, Delegate.Combine(cleaned, _drawDelegate));
                _attachedHostView = hostView;
            }

            public void Dispose()
            {
                Detach();

                foreach (GameViewToolbarItem item in _items) item.Dispose();
                _items.Clear();

                _root.RemoveFromHierarchy();
            }

            private void Detach()
            {
                if (_attachedHostView != null)
                {
                    Delegate current = _hostViewOnGuiField.GetValue(_attachedHostView) as Delegate;
                    Delegate cleaned = RemoveAll(current, _drawDelegate);

                    if (!ReferenceEquals(cleaned, current)) _hostViewOnGuiField.SetValue(_attachedHostView, cleaned);
                }

                _attachedHostView = null;
                _root.style.display = DisplayStyle.None;
            }

            /// <summary>
            /// Runs as part of the Game View's own OnGUI. Layout asks for the space, repaint reads back
            /// where it ended up — the two halves of one frame, which is why the strip trails a resize
            /// by a single repaint and then settles.
            /// </summary>
            private void DrawStrip()
            {
                if (Event.current.type == EventType.Layout)
                {
                    _width = MeasureItems();
                    if (_width > 0f) Reserve(_width);

                    return;
                }

                if (Event.current.type != EventType.Repaint) return;

                if (_width <= 0f || !TryFindGap(out object gap, out Rect groupRect))
                {
                    _root.style.display = DisplayStyle.None;
                    return;
                }

                Rect gapRect = (Rect)_entryRectField.GetValue(gap);

                _root.style.display = DisplayStyle.Flex;
                _root.style.left = gapRect.x;
                _root.style.right = StyleKeyword.Auto;
                _root.style.width = _width;
                _root.style.top = groupRect.y;
                _root.style.height = groupRect.height > 1f ? groupRect.height : FallbackToolbarHeight;
            }

            private float MeasureItems()
            {
                float total = 0f;

                foreach (GameViewToolbarItem item in _items)
                {
                    float width = Mathf.Max(0f, item.MeasureWidth());

                    item.style.display = width > 0f ? DisplayStyle.Flex : DisplayStyle.None;
                    item.style.width = width;

                    total += width;
                }

                return total;
            }

            /// <summary>
            /// Pins the flexible space to our width. The row has three of them, so the other two still
            /// take up the slack and the mute cluster stays where it was — the gap simply appears in
            /// front of it, and Unity's own controls are laid out knowing about it.
            /// </summary>
            private static void Reserve(float width)
            {
                if (!TryFindGap(out object gap, out _)) return;

                _entryMinWidthField.SetValue(gap, width);
                _entryMaxWidthField.SetValue(gap, width);
            }

            private static int CountOf(Delegate source, Delegate invocation)
            {
                if (source == null) return 0;

                int count = 0;

                foreach (Delegate current in source.GetInvocationList())
                {
                    if (current.Equals(invocation)) count++;
                }

                return count;
            }

            private static bool HasTarget(Delegate source, object target)
            {
                if (source == null) return false;

                foreach (Delegate current in source.GetInvocationList())
                {
                    if (ReferenceEquals(current.Target, target)) return true;
                }

                return false;
            }

            private static Delegate RemoveAll(Delegate source, Delegate invocation)
            {
                while (CountOf(source, invocation) > 0) source = Delegate.Remove(source, invocation);

                return source;
            }
        }
    }

    /// <summary>
    /// One item in the Game View toolbar strip. Width is asked for during the toolbar's own layout
    /// pass, so measuring against editor styles is safe there; returning zero hides the item and gives
    /// its space back.
    /// </summary>
    internal abstract class GameViewToolbarItem : VisualElement
    {
        public abstract float MeasureWidth();

        /// <summary>
        /// Width to start at when the strip could not hook the toolbar and nothing is measuring for it.
        /// An item with no width is never drawn, so it would have no chance to correct itself.
        /// </summary>
        public virtual float FallbackWidth => 24f;

        /// <summary>
        /// Applied by an item from its own draw, for the floating fallback where nothing else is
        /// measuring. Comparing this way rather than with a difference test also catches the width
        /// before layout has resolved it, which is NaN.
        /// </summary>
        protected void ApplyMeasuredWidth(float measured)
        {
            if (measured > 0f && !(Mathf.Abs(resolvedStyle.width - measured) <= 0.5f)) style.width = measured;
        }

        public virtual void Dispose()
        {
            RemoveFromHierarchy();
        }
    }
}
