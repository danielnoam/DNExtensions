using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNExtensions.HelpfulEditor
{
    /// <summary>
    /// Unity's object identity type changed with 6.4: the 32-bit instance id the 2021 APIs use was
    /// replaced by EntityId, and GetInstanceID became a compile error rather than a warning. Ids are
    /// handed around as opaque boxed values so nothing outside this class has to know which it is —
    /// they only ever travel from here to a reflected tree API that expects the matching type.
    /// </summary>
    internal static class HelpfulEditorObjectId
    {
        public static object Raw(Object unityObject)
        {
            if (!unityObject) return null;

#if UNITY_6000_4_OR_NEWER
            return unityObject.GetEntityId();
#else
            return unityObject.GetInstanceID();
#endif
        }

        /// <summary>Bridges whichever id type this Unity version produces onto a reflected signature.</summary>
        public static object ConvertTo(object rawId, System.Type targetType)
        {
            if (rawId == null || targetType == null) return null;
            if (targetType.IsInstanceOfType(rawId)) return rawId;

            System.Reflection.MethodInfo implicitCast = targetType.GetMethod("op_Implicit",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public, null, new[] { rawId.GetType() }, null);
            if (implicitCast != null) return implicitCast.Invoke(null, new[] { rawId });

            System.Reflection.ConstructorInfo constructor = targetType.GetConstructor(new[] { rawId.GetType() });
            return constructor?.Invoke(new[] { rawId });
        }

        /// <summary>
        /// Resolves an id back to its object. Returns null for ids that are not objects at all —
        /// hierarchy scene headers are tree rows with their own ids, and callers rely on that null
        /// to tell them apart from GameObjects.
        /// </summary>
        public static Object Resolve(object rawId)
        {
#if UNITY_6000_4_OR_NEWER
            return rawId is EntityId entityId ? EditorUtility.EntityIdToObject(entityId) : null;
#else
            return rawId is int instanceId ? EditorUtility.InstanceIDToObject(instanceId) : null;
#endif
        }
    }
}
