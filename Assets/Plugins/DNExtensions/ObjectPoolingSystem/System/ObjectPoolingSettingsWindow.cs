#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.ObjectPooling
{
    public class ObjectPoolingSettingsWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private ObjectPoolingSettings _settings;
        private SerializedObject _serializedSettings;
        private int _poolToDelete = -1;

        [MenuItem("Tools/DNExtensions/Object Pooling Settings", false)]
        public static void ShowWindow()
        {
            ObjectPoolingSettingsWindow window = GetWindow<ObjectPoolingSettingsWindow>();
            window.titleContent = new GUIContent("Object Pooling Settings");
            window.minSize = new Vector2(450, 400);
            window.Show();
        }

        private void OnEnable()
        {
            LoadSettings();
        }

        private void LoadSettings()
        {
            _settings = ObjectPoolingSettings.Instance;

            if (!_settings)
            {
                // Try to find it in the project
                string[] guids = AssetDatabase.FindAssets("t:ObjectPoolingSettings");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    _settings = AssetDatabase.LoadAssetAtPath<ObjectPoolingSettings>(path);
                }
            }

            if (_settings)
            {
                _serializedSettings = new SerializedObject(_settings);
            }
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            EditorGUILayout.Space();
            
            if (!_settings)
            {
                DrawNoSettingsGUI();
            }
            else
            {
                DrawSettingsGUI();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawNoSettingsGUI()
        {
            EditorGUILayout.HelpBox(
                "No ObjectPoolingSettings asset found!\n\n" +
                "Create one to configure your object pools.",
                MessageType.Warning
            );

            EditorGUILayout.Space();

            if (GUILayout.Button("Create ObjectPoolingSettings Asset", GUILayout.Height(30)))
            {
                CreateSettingsAsset();
            }

            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "The settings asset must be placed in a Resources folder to be loaded at runtime.",
                MessageType.Info
            );
        }

        private void DrawSettingsGUI()
        {
            if (_serializedSettings == null)
            {
                _serializedSettings = new SerializedObject(_settings);
            }

            _serializedSettings.Update();

            // Asset reference field
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Settings Asset:", GUILayout.Width(100));
            EditorGUILayout.ObjectField(_settings, typeof(ObjectPoolingSettings), false);
            if (GUILayout.Button("Ping", GUILayout.Width(50)))
            {
                EditorGUIUtility.PingObject(_settings);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            
            EditorGUI.indentLevel++;

            SerializedProperty instantiateFallback = _serializedSettings.FindProperty("instantiateAsFallback");
            SerializedProperty destroyFallback = _serializedSettings.FindProperty("destroyAsFallback");
            SerializedProperty showDebug = _serializedSettings.FindProperty("showDebugMessages");

            EditorGUILayout.PropertyField(instantiateFallback, new GUIContent("Instantiate As Fallback",
                "If no pool exists for an object, instantiate it instead of returning null"));
            EditorGUILayout.PropertyField(destroyFallback, new GUIContent("Destroy As Fallback",
                "If returning an object that doesn't belong to any pool, destroy it"));
            EditorGUILayout.PropertyField(showDebug, new GUIContent("Show Debug Messages",
                "Show debug messages in console for pool operations"));

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            // Pool Configurations

            SerializedProperty poolsProperty = _serializedSettings.FindProperty("pools");

            if (poolsProperty != null && poolsProperty.isArray)
            {
                EditorGUI.indentLevel++;

                // Display pool count
                EditorGUILayout.LabelField($"Total Pools: {poolsProperty.arraySize}", EditorStyles.miniLabel);
                EditorGUILayout.Space(5);

                // Draw each pool
                for (int i = 0; i < poolsProperty.arraySize; i++)
                {
                    SerializedProperty pool = poolsProperty.GetArrayElementAtIndex(i);
                    DrawPoolElement(pool, i, poolsProperty);
                    EditorGUILayout.Space(5);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            // Add new pool button
            if (GUILayout.Button("Add New Pool", GUILayout.Height(25)))
            {
                if (poolsProperty != null)
                {
                    poolsProperty.InsertArrayElementAtIndex(poolsProperty.arraySize);
                    SerializedProperty newPool = poolsProperty.GetArrayElementAtIndex(poolsProperty.arraySize - 1);

                    // Initialize with default values
                    newPool.FindPropertyRelative("poolName").stringValue = $"New Pool {poolsProperty.arraySize}";
                    newPool.FindPropertyRelative("maxPoolSize").intValue = 50;
                    newPool.FindPropertyRelative("dontDestroyOnLoad").boolValue = true;
                    newPool.FindPropertyRelative("recycleActiveObjects").boolValue = false;
                    newPool.FindPropertyRelative("preWarmPool").boolValue = false;
                    newPool.FindPropertyRelative("preWarmPoolSize").intValue = 5;
                }
            }

            _serializedSettings.ApplyModifiedProperties();

            // Handle deferred deletion after all GUI is done
            if (_poolToDelete >= 0 && poolsProperty != null && poolsProperty.arraySize > _poolToDelete)
            {
                poolsProperty.DeleteArrayElementAtIndex(_poolToDelete);
                _serializedSettings.ApplyModifiedProperties();
                _poolToDelete = -1;
            }

            EditorGUILayout.Space();

            // Help box
            EditorGUILayout.HelpBox(
                "How to use Object Pooling:\n\n" +
                "1. Configure your pools in this window\n" +
                "2. Pools are automatically created when the game starts\n" +
                "3. Use ObjectPooler.GetObjectFromPool(prefab, position, rotation) to get objects\n" +
                "4. Use ObjectPooler.ReturnObjectToPool(obj) to return objects",
                MessageType.Info
            );
        }

        private void DrawPoolElement(SerializedProperty pool, int index, SerializedProperty poolsArray)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Header with fold and delete button
            EditorGUILayout.BeginHorizontal();

            SerializedProperty poolName = pool.FindPropertyRelative("poolName");
            SerializedProperty prefab = pool.FindPropertyRelative("prefab");

            string displayName = string.IsNullOrEmpty(poolName.stringValue)
                ? $"Pool {index}"
                : poolName.stringValue;

            pool.isExpanded = EditorGUILayout.Foldout(pool.isExpanded, displayName, true);

            if (GUILayout.Button("×", GUILayout.Width(25), GUILayout.Height(18)))
            {
                if (EditorUtility.DisplayDialog("Delete Pool",
                    $"Are you sure you want to delete '{displayName}'?", "Delete", "Cancel"))
                {
                    _poolToDelete = index;
                }
            }

            EditorGUILayout.EndHorizontal();

            if (pool.isExpanded)
            {
                EditorGUI.indentLevel++;

                // Pool Settings
                EditorGUILayout.PropertyField(poolName);
                EditorGUILayout.PropertyField(pool.FindPropertyRelative("maxPoolSize"));
                EditorGUILayout.PropertyField(prefab);
                EditorGUILayout.PropertyField(pool.FindPropertyRelative("dontDestroyOnLoad"));
                EditorGUILayout.PropertyField(pool.FindPropertyRelative("recycleActiveObjects"));

                EditorGUILayout.Space(5);

                // Pre-warm settings
                EditorGUILayout.PropertyField(pool.FindPropertyRelative("preWarmPool"));

                SerializedProperty preWarmPool = pool.FindPropertyRelative("preWarmPool");
                if (preWarmPool.boolValue)
                {
                    EditorGUILayout.PropertyField(pool.FindPropertyRelative("preWarmPoolSize"));
                    EditorGUILayout.PropertyField(pool.FindPropertyRelative("scenesToPreWarm"), true);
                }

                EditorGUI.indentLevel--;

                // Show warning if prefab is missing
                if (!prefab.objectReferenceValue)
                {
                    EditorGUILayout.HelpBox("Prefab is required for this pool to function!", MessageType.Warning);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void CreateSettingsAsset()
        {
            // Create the settings asset
            ObjectPoolingSettings settings = CreateInstance<ObjectPoolingSettings>();

            // Ensure Resources folder exists
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            // Check if asset already exists
            string path = "Assets/Resources/ObjectPoolingSettings.asset";
            if (AssetDatabase.LoadAssetAtPath<ObjectPoolingSettings>(path))
            {
                if (!EditorUtility.DisplayDialog("Asset Exists",
                    "ObjectPoolingSettings already exists at " + path + "\n\nDo you want to overwrite it?",
                    "Overwrite", "Cancel"))
                {
                    return;
                }
            }


            AssetDatabase.CreateAsset(settings, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            _settings = AssetDatabase.LoadAssetAtPath<ObjectPoolingSettings>(path);
            _serializedSettings = new SerializedObject(_settings);
            EditorGUIUtility.PingObject(_settings);

            Debug.Log($"Created ObjectPoolingSettings at: {path}");
        }
    }
}
#endif