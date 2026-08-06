using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.HelpfulEditor.Inspector
{
    /// <summary>
    /// The header bar's field search. Walks the whole serialized tree of every component, so fields
    /// nested in structs, in serializable classes and in array elements are findable — matching only
    /// top-level properties leaves most of an object's interesting state unreachable.
    ///
    /// Results are built off the GUI, on a debounce, and cached until the query or the object
    /// changes. The walk is far too expensive to repeat per repaint, which is the only reason the
    /// top-level version could get away with running inline.
    /// </summary>
    [InitializeOnLoad]
    internal static class InspectorFieldSearch
    {
        /// <summary>Quiet period after the last keystroke before the tree is walked.</summary>
        private const double DebounceSeconds = 0.15;

        /// <summary>
        /// Depth limit for the walk. Plain serialized data cannot nest indefinitely, but a
        /// SerializeReference graph can, and a search box is not where that should be discovered.
        /// </summary>
        private const int MaxDepth = 8;

        private const string ScriptProperty = "m_Script";

        private static readonly List<ComponentMatches> Results = new List<ComponentMatches>();
        private static readonly List<Match> Buffer = new List<Match>();

        private static GameObject _target;
        private static string _query = string.Empty;
        private static List<string> _excludedTypes;

        private static GameObject _pendingTarget;
        private static string _pendingQuery = string.Empty;
        private static double _pendingSince;
        private static bool _pending;

        static InspectorFieldSearch()
        {
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
        }

        /// <summary>One component's matches, kept with the SerializedObject the properties belong to.</summary>
        private sealed class ComponentMatches
        {
            public Component Component;
            public SerializedObject SerializedObject;
            public readonly List<Match> Matches = new List<Match>();
        }

        /// <summary>
        /// A row to draw. Ancestors of a match are included with <see cref="DrawChildren"/> off: they
        /// are there to say where the match sits, and drawing their children would pull in the whole
        /// branch the search was meant to narrow down.
        /// </summary>
        private sealed class Match
        {
            public SerializedProperty Property;
            public int Depth;
            public bool DrawChildren;
        }

        /// <summary>
        /// Draws the matches for this object and query, queueing a rebuild when either has changed.
        /// The previous results keep drawing until the new ones are ready, so the list does not blink
        /// empty on every keystroke.
        /// </summary>
        public static void Draw(GameObject gameObject, InspectorSettings settings, string query)
        {
            RequestRebuild(gameObject, settings, query);

            // Results belong to one object. A second Inspector showing something else draws nothing
            // rather than the first one's fields.
            if (_target != gameObject) return;

            foreach (ComponentMatches entry in Results)
            {
                if (!entry.Component) continue;

                entry.SerializedObject.Update();

                EditorGUILayout.LabelField(ObjectNames.NicifyVariableName(entry.Component.GetType().Name), EditorStyles.miniBoldLabel);

                int baseIndent = EditorGUI.indentLevel;

                EditorGUI.BeginChangeCheck();

                foreach (Match match in entry.Matches)
                {
                    EditorGUI.indentLevel = baseIndent + match.Depth;
                    DrawMatch(match);
                }

                EditorGUI.indentLevel = baseIndent;

                if (EditorGUI.EndChangeCheck()) entry.SerializedObject.ApplyModifiedProperties();

                EditorGUILayout.Space(2);
            }
        }

        /// <summary>
        /// A matched property is drawn open regardless of how it was left in the Inspector — it was
        /// searched for, so its contents are the point. The fold state is put back afterwards so a
        /// search does not quietly reorganise the object it was run against.
        /// </summary>
        private static void DrawMatch(Match match)
        {
            SerializedProperty property = match.Property;
            if (property == null) return;

            if (!match.DrawChildren)
            {
                EditorGUILayout.PropertyField(property, false);
                return;
            }

            bool wasExpanded = property.isExpanded;
            property.isExpanded = true;

            EditorGUILayout.PropertyField(property, true);

            property.isExpanded = wasExpanded;
        }

        private static void RequestRebuild(GameObject gameObject, InspectorSettings settings, string query)
        {
            _excludedTypes = settings.excludedComponentTypes;

            // Showing this already, with nothing queued that would replace it.
            if (!_pending && _target == gameObject && _query == query) return;

            // Queued for exactly this — re-stamping the clock would push the rebuild back for as
            // long as the Inspector keeps repainting.
            if (_pending && _pendingTarget == gameObject && _pendingQuery == query) return;

            _pendingTarget = gameObject;
            _pendingQuery = query;
            _pendingSince = EditorApplication.timeSinceStartup;
            _pending = true;
        }

        private static void OnUpdate()
        {
            // Results outlive the components they came from — a script recompile or a removed
            // component leaves the cached SerializedObjects pointing at nothing.
            if (!_pending && IsStale())
            {
                _pendingTarget = _target;
                _pendingQuery = _query;
                _pendingSince = 0d;
                _pending = true;
            }

            if (!_pending) return;
            if (EditorApplication.timeSinceStartup - _pendingSince < DebounceSeconds) return;

            Rebuild();
        }

        private static bool IsStale()
        {
            foreach (ComponentMatches entry in Results)
            {
                if (!entry.Component) return true;
            }

            return false;
        }

        private static void Rebuild()
        {
            _pending = false;
            _target = _pendingTarget;
            _query = _pendingQuery;

            Results.Clear();

            if (!_target || string.IsNullOrWhiteSpace(_query))
            {
                RepaintInspectors();
                return;
            }

            foreach (Component component in HelpfulEditorGUI.GetDisplayComponents(_target, _excludedTypes))
            {
                if (!component) continue;

                SerializedObject serializedObject = new SerializedObject(component);

                SerializedProperty iterator = serializedObject.GetIterator();
                if (!iterator.NextVisible(true)) continue;

                Buffer.Clear();

                do
                {
                    if (iterator.propertyPath == ScriptProperty) continue;

                    Collect(iterator, Buffer, 0);
                }
                while (iterator.NextVisible(false));

                if (Buffer.Count == 0) continue;

                ComponentMatches entry = new ComponentMatches
                {
                    Component = component,
                    SerializedObject = serializedObject
                };

                entry.Matches.AddRange(Buffer);
                Results.Add(entry);
            }

            RepaintInspectors();
        }

        /// <summary>
        /// Adds every row needed to show this property's matches, and reports whether there were any.
        /// A property that matches is taken whole and not descended into; otherwise its ancestors are
        /// only added once something below them matched, and are inserted above those matches so the
        /// rows come out in the order they are drawn.
        /// </summary>
        private static bool Collect(SerializedProperty property, List<Match> results, int depth)
        {
            if (depth > MaxDepth) return false;

            // A path search is what makes a nested field addressable — "rb.mass" cannot be expressed
            // against display names, which are per-level and never contain the separator.
            string candidate = _query.Contains('.') ? property.propertyPath : property.displayName;

            if (HelpfulEditorFuzzySearch.TryMatch(candidate, _query, out float _))
            {
                results.Add(new Match
                {
                    Property = property.Copy(),
                    Depth = depth,
                    DrawChildren = property.hasVisibleChildren
                });

                return true;
            }

            int insertAt = results.Count;
            bool matched = false;

            foreach (SerializedProperty child in Children(property))
            {
                matched |= Collect(child, results, depth + 1);
            }

            if (!matched) return false;

            results.Insert(insertAt, new Match
            {
                Property = property.Copy(),
                Depth = depth,
                DrawChildren = false
            });

            return true;
        }

        private static IEnumerable<SerializedProperty> Children(SerializedProperty property)
        {
            if (IsArray(property))
            {
                for (int i = 0; i < property.arraySize; i++)
                {
                    SerializedProperty element = property.GetArrayElementAtIndex(i);

                    // Elements are named "Element 3", so only ones with fields of their own can be
                    // matched on anything a person would type.
                    if (element.hasVisibleChildren) yield return element;
                }

                yield break;
            }

            if (!property.hasVisibleChildren) yield break;

            SerializedProperty iterator = property.Copy();
            int parentDepth = property.depth;

            bool hasChild = iterator.NextVisible(true);

            while (hasChild && iterator.depth > parentDepth)
            {
                if (iterator.depth == parentDepth + 1) yield return iterator;

                hasChild = iterator.NextVisible(false);
            }
        }

        /// <summary>Strings report isArray, and walking one character by character finds nothing.</summary>
        private static bool IsArray(SerializedProperty property)
        {
            return property.isArray && property.propertyType != SerializedPropertyType.String;
        }

        private static void RepaintInspectors()
        {
            foreach (EditorWindow window in HelpfulEditorWindows.AllInspectors())
            {
                if (window) window.Repaint();
            }
        }
    }
}
