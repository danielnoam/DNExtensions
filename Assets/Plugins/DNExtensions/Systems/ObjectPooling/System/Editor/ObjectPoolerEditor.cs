
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.Systems.ObjectPooling
{
    
    /// <summary>
    /// Custom editor for ObjectPooler to display pool information at runtime.
    /// </summary>
    [CustomEditor(typeof(ObjectPooler))]
    public class ObjectPoolerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var pooler = (ObjectPooler)target;

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Pool debug info is available at runtime.", MessageType.Info);
                return;
            }

            if (pooler.Pools == null || pooler.Pools.Count == 0)
            {
                EditorGUILayout.HelpBox("No pools initialized.", MessageType.Warning);
                return;
            }

            foreach (var pool in pooler.Pools)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.LabelField(pool.poolName, EditorStyles.boldLabel);

                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("Pool Size", pool.PoolSize.ToString());
                EditorGUILayout.LabelField("Active", pool.ActiveCount.ToString());
                EditorGUILayout.LabelField("Inactive", pool.InactiveCount.ToString());
                EditorGUI.indentLevel--;

                EditorGUILayout.EndVertical();
            }

            Repaint();
        }
    }
    
}
