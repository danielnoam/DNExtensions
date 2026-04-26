
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.Rendering;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// While Unity's built-in box collider edit tool is active, draws camera-facing
    /// discs at each face center. X axis: red, Y axis: green, Z axis: blue.
    /// </summary>
    [InitializeOnLoad]
    internal static class BoxColliderDrawer 
    {
        private static readonly Color FaceColorX = Color.red;
        private static readonly Color FaceColorY = Color.green;
        private static readonly Color FaceColorZ = Color.blue;
        private const float DotSizeFactor = 0.09f;

        private static Type _boxPrimitiveColliderToolType;

        static BoxColliderDrawer() => SceneView.duringSceneGui += OnSceneGUI;

        private static void OnSceneGUI(SceneView view) {
            if (!IsBoxPrimitiveColliderToolActive())
                return;

            var targets = CollectSelectedBoxColliders();
            if (targets.Count == 0)
                return;

            var prevZ = Handles.zTest;
            Handles.zTest = CompareFunction.Always;

            foreach (var box in targets) {
                if (!box)
                    continue;
                DrawFaceDots(box, view.camera);
            }

            Handles.zTest = prevZ;
        }

        private static void DrawFaceDots(BoxCollider box, Camera cam) {
            Transform transform = box.transform;
            Vector3 center = box.center;
            Vector3 halfSize = box.size * 0.5f;

            (Vector3 local, Color color)[] faces = {
                (center + new Vector3( halfSize.x, 0f, 0f), FaceColorX),
                (center + new Vector3(-halfSize.x, 0f, 0f), FaceColorX),
                (center + new Vector3(0f,  halfSize.y, 0f), FaceColorY),
                (center + new Vector3(0f, -halfSize.y, 0f), FaceColorY),
                (center + new Vector3(0f, 0f,  halfSize.z), FaceColorZ),
                (center + new Vector3(0f, 0f, -halfSize.z), FaceColorZ),
            };

            foreach (var (local, color) in faces) {
                Vector3 world = transform.TransformPoint(local);
                float radius = HandleUtility.GetHandleSize(world) * DotSizeFactor * 0.75f;
                Vector3 normal = cam ? (cam.transform.position - world).normalized : Vector3.forward;
                if (normal.sqrMagnitude < 1e-10f)
                    normal = Vector3.up;
                Handles.color = color;
                Handles.DrawSolidDisc(world, normal, radius);
            }
        }

        private static List<BoxCollider> CollectSelectedBoxColliders() {
            var set = new HashSet<BoxCollider>();
            foreach (var obj in Selection.objects) {
                switch (obj) {
                    case BoxCollider box:
                        set.Add(box);
                        break;
                    case GameObject go:
                        if (go.TryGetComponent(out BoxCollider goBox))
                            set.Add(goBox);
                        break;
                }
            }
            return new List<BoxCollider>(set);
        }

        private static bool IsBoxPrimitiveColliderToolActive() {
            if (_boxPrimitiveColliderToolType == null)
                _boxPrimitiveColliderToolType = FindBoxPrimitiveColliderToolType();
            return _boxPrimitiveColliderToolType != null && ToolManager.activeToolType == _boxPrimitiveColliderToolType;
        }

        private static Type FindBoxPrimitiveColliderToolType() {
            var type = Type.GetType("UnityEditor.BoxPrimitiveColliderTool, UnityEditor");
            if (type != null)
                return type;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                type = assembly.GetType("UnityEditor.BoxPrimitiveColliderTool");
                if (type != null)
                    return type;
            }
            return null;
        }
    }
}