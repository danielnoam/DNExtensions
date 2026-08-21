using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNExtensions.HelpfulEditor.Inspector
{
    /// <summary>
    /// The scene behind a preview: a PreviewRenderUtility, a copy of whatever is being previewed, and
    /// whatever else that copy needs to stand up in. Shared by the UI and particle previews because
    /// the lifecycle is the whole difficulty — a PreviewRenderUtility that outlives its owner keeps
    /// its scene, and the utility stops working altogether once enough have accumulated.
    ///
    /// Camera work is deliberately not here. The two previews want opposite things — an orthographic
    /// camera square-on to a canvas, a perspective one orbiting a burst of particles — and a framing
    /// method general enough for both would be worth less than the two short ones it replaced.
    /// </summary>
    internal sealed class PreviewStage
    {
        /// <summary>
        /// How many objects of a multiple selection get a stage of their own. There is one
        /// PreviewRenderUtility behind each — the shape Unity uses, a PreviewData per target index —
        /// because the grid drawn for a multiple selection asks every target for a cell on the same
        /// repaint, and one shared stage would spend every frame being rebuilt for whichever asked
        /// last. The cap is because a PreviewRenderUtility is not free and the class stops working
        /// altogether once enough have been made; past it a cell says so rather than showing nothing.
        /// </summary>
        public const int MaxTargets = 16;

        private static Type _propertyEditorType;
        private static FieldInfo _previewsField;
        private static FieldInfo _selectedPreviewField;
        private static bool _selectionResolved;
        private static bool _selectionAvailable;

        private readonly List<GameObject> _owned = new List<GameObject>();

        private PreviewRenderUtility _preview;
        private GameObject _instance;
        private GameObject _built;

        public Camera Camera => _preview?.camera;

        /// <summary>The copy in the preview scene. Never the asset — nothing here may touch that.</summary>
        public GameObject Instance => _instance;

        public bool Ready => _preview != null && _instance;

        public Color AmbientColor
        {
            set { if (_preview != null) _preview.ambientColor = value; }
        }

        /// <summary>
        /// True when this call replaced the copy, which is the caller's cue to reset anything it was
        /// playing or measuring. False both when the copy still matches and when there is nothing to
        /// build — <see cref="Ready"/> is what separates those.
        /// </summary>
        public bool Rebuild(GameObject source)
        {
            if (_preview != null && _instance && _built == source) return false;

            Destroy();

            if (!source) return false;

            _preview = new PreviewRenderUtility();
            _built = source;

            // A plain copy rather than PrefabUtility.InstantiatePrefab: a connected prefab instance
            // brings override bookkeeping to a scene that is about to be thrown away, and every
            // property a preview sets on it would be recorded as an override of the asset. It lands in
            // the open scene for the length of these three statements; Instantiate does not mark a
            // scene dirty, and nothing repaints in between.
            _instance = Object.Instantiate(source);
            _instance.hideFlags = HideFlags.HideAndDontSave;
            _preview.AddSingleGO(_instance);

            _instance.SetActive(true);

            return true;
        }

        /// <summary>
        /// Something of the preview's own in the same scene — the canvas the UI preview puts above a
        /// prefab that has none. Destroyed with everything else, which is the point of asking here
        /// rather than building it at the call site.
        /// </summary>
        public GameObject CreateObject(string name, params Type[] components)
        {
            if (_preview == null) return null;

            GameObject go = EditorUtility.CreateGameObjectWithHideFlags(name, HideFlags.HideAndDontSave, components);

            _preview.AddSingleGO(go);
            _owned.Add(go);

            return go;
        }

        public void Begin(Rect rect, GUIStyle background)
        {
            _preview.BeginPreview(rect, background);
        }

        public void Render(Rect rect)
        {
            _preview.camera.Render();
            _preview.EndAndDrawPreview(rect);
        }

        public void Destroy()
        {
            for (int i = _owned.Count - 1; i >= 0; i--)
            {
                if (_owned[i]) Object.DestroyImmediate(_owned[i]);
            }

            _owned.Clear();

            if (_instance) Object.DestroyImmediate(_instance);

            _instance = null;
            _built = null;

            _preview?.Cleanup();
            _preview = null;
        }

        /// <summary>
        /// Repaints the inspector from outside a GUI callback, which is the one thing an animated
        /// preview cannot otherwise do: ObjectPreview is handed no window and owns no editor whose
        /// Repaint it could call. The windows are looked up rather than fetched so that asking for
        /// them cannot create one.
        /// </summary>
        public static void RepaintInspectors()
        {
            foreach (EditorWindow window in Inspectors())
            {
                window.Repaint();
            }
        }

        /// <summary>
        /// Makes this the preview on show, where the inspector has not been told otherwise.
        ///
        /// Unity lists the editor's own preview first and <c>GetEditorThatControlsPreview</c> returns
        /// the first entry unless <c>m_SelectedPreview</c> says otherwise — so a particle prefab opens
        /// on Unity's static render of a system that never ran, an empty box, with the live one an
        /// entry down a dropdown nobody has a reason to open.
        ///
        /// The field is cleared whenever the previewables are rebuilt, which is to say whenever the
        /// selection changes, so finding it null means precisely "nothing has been picked since this
        /// was selected". Writing to it only then is what keeps this from overriding a choice: pick
        /// Unity's from the dropdown and it stays picked for as long as that selection lasts.
        /// </summary>
        public static void ClaimSelection(ObjectPreview preview)
        {
            ResolveSelection();
            if (!_selectionAvailable) return;

            try
            {
                foreach (EditorWindow window in Inspectors())
                {
                    // Ours belongs to exactly one inspector's list, which is what scopes this to the
                    // window actually showing it rather than to every window open.
                    if (!(_previewsField.GetValue(window) is IList previews)) continue;
                    if (!previews.Contains(preview)) continue;

                    if (_selectedPreviewField.GetValue(window) != null) continue;

                    _selectedPreviewField.SetValue(window, preview);
                }
            }
            catch (Exception e)
            {
                // One failure means the internals moved, so the mechanism is abandoned rather than
                // retried on every selection. The preview still works, it just is not the one on top.
                _selectionAvailable = false;
                Debug.LogWarning($"[HelpfulEditor] Previews cannot be pre-selected on this Unity version. ({e.Message})");
            }
        }

        /// <summary>
        /// The open inspectors, from the shared window scan rather than a FindObjectsOfTypeAll of
        /// this class's own. That call walks every loaded object in the editor, so it costs what the
        /// project is big rather than what it returns — and RepaintInspectors runs thirty times a
        /// second for as long as an animated preview is playing, which made this the one place in
        /// the suite still paying that per frame.
        ///
        /// The scan it reads is up to a quarter second old, so an inspector opened by hand can miss
        /// a frame or two of an animation before it starts being repainted. Every other module in
        /// the suite already accepts that trade, and it is invisible against a preview that plays
        /// for seconds.
        /// </summary>
        private static IEnumerable<EditorWindow> Inspectors() => HelpfulEditorWindows.AllInspectors();

        private static void ResolveSelection()
        {
            if (_selectionResolved) return;
            _selectionResolved = true;

            try
            {
                _propertyEditorType = typeof(EditorWindow).Assembly.GetType("UnityEditor.PropertyEditor");
                if (_propertyEditorType == null) return;

                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

                _previewsField = _propertyEditorType.GetField("m_Previews", flags);
                _selectedPreviewField = _propertyEditorType.GetField("m_SelectedPreview", flags);

                _selectionAvailable = _previewsField != null && _selectedPreviewField != null;
            }
            catch (Exception)
            {
                _selectionAvailable = false;
            }
        }
    }
}
