using System.Text;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DNExtensions.Utilities.MCP
{
    /// <summary>
    /// Reads and writes the designer notes left in scenes, through <see cref="Note"/> components
    /// and NoteField fields on any component.
    /// </summary>
    internal static class NoteMcpCommands
    {
        private const string PlaceholderText = "Add your note here...";

        [CliCommand("dn_list_notes", "List the notes left in the loaded scenes: Note components and NoteField fields on any component")]
        private static string ListNotes(
            [CliArg("include_fields", "Also scan every component for NoteField fields")] bool includeFields = true)
        {
            var builder = new StringBuilder();

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (Note note in root.GetComponentsInChildren<Note>(true))
                    {
                        if (string.IsNullOrWhiteSpace(note.Text) || note.Text == PlaceholderText) continue;

                        builder.AppendLine($"[{scene.name}] {McpUtility.GetHierarchyPath(note.transform)}: {note.Text}");
                    }

                    if (!includeFields) continue;

                    foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                    {
                        if (!behaviour || behaviour is Note) continue;

                        AppendNoteFields(builder, scene.name, behaviour);
                    }
                }
            }

            return builder.Length > 0 ? builder.ToString() : "No notes in the loaded scenes.";
        }

        [CliCommand("dn_set_note", "Write a Note on a scene GameObject, adding the Note component when it has none")]
        private static string SetNote(
            [CliArg("target", "Scene hierarchy path of the GameObject", Required = true)] string target,
            [CliArg("text", "The note text", Required = true)] string text,
            [CliArg("append", "Add the text on a new line after the existing note instead of replacing it")] bool append = false)
        {
            GameObject gameObject = McpUtility.FindGameObject(target);
            Note note = gameObject.GetComponent<Note>();

            if (!note)
            {
                note = Undo.AddComponent<Note>(gameObject);
            }
            else
            {
                Undo.RecordObject(note, "Set Note");
            }

            bool hasText = !string.IsNullOrWhiteSpace(note.Text) && note.Text != PlaceholderText;
            note.Text = append && hasText ? $"{note.Text}\n{text}" : text;

            EditorUtility.SetDirty(note);

            if (!Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }

            return $"Note on {McpUtility.GetHierarchyPath(gameObject.transform)}: {note.Text}";
        }

        private static void AppendNoteFields(StringBuilder builder, string sceneName, MonoBehaviour behaviour)
        {
            var serializedObject = new SerializedObject(behaviour);
            SerializedProperty property = serializedObject.GetIterator();

            while (property.NextVisible(true))
            {
                if (property.type != "NoteField") continue;

                string text = property.FindPropertyRelative("note")?.stringValue;
                if (string.IsNullOrWhiteSpace(text)) continue;

                builder.AppendLine($"[{sceneName}] {McpUtility.GetHierarchyPath(behaviour.transform)} {behaviour.GetType().Name}.{property.propertyPath}: {text}");
            }
        }
    }
}
