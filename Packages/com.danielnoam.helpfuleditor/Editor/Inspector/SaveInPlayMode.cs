using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNExtensions.HelpfulEditor.Inspector
{
    /// <summary>
    /// Adds a save button to component headers during play mode. Marked components have their state
    /// captured and re-applied after returning to edit mode.
    /// </summary>
    [InitializeOnLoad]
    internal static class SaveInPlayMode
    {
        private static readonly string SaveIcon = HelpfulEditorPlatform.Glyph("💾", "Save");

        private static readonly Color MarkedTint = new Color(0.3f, 0.7f, 0.3f, 0.3f);
        private static readonly HashSet<string> MarkedForSave = new HashSet<string>();
        private static readonly Dictionary<string, ComponentSnapshot> SavedData = new Dictionary<string, ComponentSnapshot>();

        /// <summary>
        /// A component's state. Object reference fields are captured separately via GlobalObjectId
        /// rather than left to EditorJsonUtility, because JSON embeds the raw instance ID of
        /// referenced objects. Those IDs are only valid for the play mode session they came from —
        /// afterwards they are recycled, so restoring one either nulls the reference or points it at
        /// an unrelated object. GlobalObjectId encodes something stable instead: scene plus local
        /// file ID for scene objects, asset GUID for assets.
        /// </summary>
        private class ComponentSnapshot
        {
            public string Json;
            public Dictionary<string, string> ObjectReferences;
        }

        static SaveInPlayMode()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            EditorApplication.quitting -= ClearAll;
            EditorApplication.quitting += ClearAll;

            ComponentHeaderButtons.RegisterProvider(GetButton);
        }

        private static ComponentHeaderButtons.ButtonData GetButton(Component component)
        {
            if (!ShouldShow(component)) return null;

            bool marked = MarkedForSave.Contains(GetKey(component));

            return new ComponentHeaderButtons.ButtonData
            {
                Icon = SaveIcon,
                Tooltip = marked ? "Disable save on exit" : "Save on play mode exit",
                Priority = -1000,
                SupportsMultiSelect = true,
                Callback = ToggleSave,
                StyleCallback = button =>
                {
                    if (marked) button.style.backgroundColor = MarkedTint;
                }
            };
        }

        private static bool ShouldShow(Component component)
        {
            InspectorSettings settings = HelpfulEditorSettings.Inspector;
            if (!settings.moduleEnabled || !settings.saveInPlayModeEnabled) return false;
            if (!EditorApplication.isPlaying) return false;

            string typeName = component.GetType().FullName;
            foreach (string blacklisted in settings.saveInPlayModeBlacklist)
            {
                if (string.Equals(typeName, blacklisted, StringComparison.Ordinal)) return false;
            }

            // Nothing spawned at runtime has an edit-mode counterpart to restore onto, so the button
            // is withheld rather than offered and then quietly doing nothing.
            return !string.IsNullOrEmpty(GetKey(component));
        }

        /// <summary>
        /// The component clicked decides the direction, and the rest of the selection follows it, so
        /// a mixed selection ends up in one state rather than each entry flipping its own way.
        /// </summary>
        private static void ToggleSave(Component component)
        {
            if (!component) return;

            bool mark = !MarkedForSave.Contains(GetKey(component));
            Type type = component.GetType();

            foreach (GameObject target in ComponentHeaderButtons.TargetObjects(component))
            {
                if (!target) continue;

                Component match = target.GetComponent(type);
                if (!match) continue;

                string key = GetKey(match);
                if (string.IsNullOrEmpty(key)) continue;

                if (mark)
                {
                    MarkedForSave.Add(key);
                    SavedData[key] = Capture(match);
                }
                else
                {
                    MarkedForSave.Remove(key);
                    SavedData.Remove(key);
                }
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.ExitingPlayMode:
                    foreach (string key in MarkedForSave)
                    {
                        Component component = Resolve(key);
                        if (component) SavedData[key] = Capture(component);
                    }

                    break;

                case PlayModeStateChange.ExitingEditMode:
                    ClearAll();
                    break;

                case PlayModeStateChange.EnteredEditMode:
                    EditorApplication.delayCall += Restore;
                    break;
            }
        }

        private static ComponentSnapshot Capture(Component component)
        {
            ComponentSnapshot snapshot = new ComponentSnapshot
            {
                Json = EditorJsonUtility.ToJson(component, false),
                ObjectReferences = new Dictionary<string, string>()
            };

            SerializedObject serializedObject = new SerializedObject(component);
            SerializedProperty property = serializedObject.GetIterator();

            while (property.NextVisible(true))
            {
                if (property.propertyType != SerializedPropertyType.ObjectReference) continue;

                Object value = property.objectReferenceValue;
                snapshot.ObjectReferences[property.propertyPath] = value
                    ? GlobalObjectId.GetGlobalObjectIdSlow(value).ToString()
                    : string.Empty;
            }

            return snapshot;
        }

        private static void Restore()
        {
            foreach (KeyValuePair<string, ComponentSnapshot> entry in SavedData)
            {
                Component component = Resolve(entry.Key);
                if (component) Apply(component, entry.Value);
            }

            ClearAll();
        }

        /// <summary>
        /// Non-reference fields come back through JSON; object references are then resolved from
        /// their GlobalObjectId and applied explicitly, overriding whatever the JSON produced.
        /// </summary>
        private static void Apply(Component component, ComponentSnapshot snapshot)
        {
            Undo.RecordObject(component, "Restore Play Mode Changes");
            EditorJsonUtility.FromJsonOverwrite(snapshot.Json, component);

            if (snapshot.ObjectReferences.Count > 0)
            {
                SerializedObject serializedObject = new SerializedObject(component);
                bool changed = false;

                foreach (KeyValuePair<string, string> entry in snapshot.ObjectReferences)
                {
                    SerializedProperty property = serializedObject.FindProperty(entry.Key);
                    if (property == null) continue;

                    Object resolved = null;
                    if (!string.IsNullOrEmpty(entry.Value) && GlobalObjectId.TryParse(entry.Value, out GlobalObjectId id))
                    {
                        resolved = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id);
                    }

                    if (property.objectReferenceValue == resolved) continue;

                    property.objectReferenceValue = resolved;
                    changed = true;
                }

                if (changed) serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(component);
        }

        /// <summary>
        /// Identifies the component itself rather than the path to it.
        ///
        /// A scene-plus-path-plus-type key looked reasonable and lost data three ways: GameObject.Find
        /// never returns inactive objects, so marking a component on a disabled object restored
        /// nothing and said nothing; it matches the first path across all loaded scenes; and two
        /// components of the same type on one object shared a key, so the second saved over the
        /// first. A GlobalObjectId has none of those problems, and this file already trusts it for
        /// object references.
        ///
        /// Returns empty for anything with no stable identity — a component on an object created at
        /// runtime, which has no edit-mode counterpart to restore onto.
        /// </summary>
        private static string GetKey(Component component)
        {
            GlobalObjectId id = GlobalObjectId.GetGlobalObjectIdSlow(component);

            return id.identifierType == 0 ? string.Empty : id.ToString();
        }

        private static Component Resolve(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (!GlobalObjectId.TryParse(key, out GlobalObjectId id)) return null;

            return GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as Component;
        }

        private static void ClearAll()
        {
            MarkedForSave.Clear();
            SavedData.Clear();
        }
    }
}
