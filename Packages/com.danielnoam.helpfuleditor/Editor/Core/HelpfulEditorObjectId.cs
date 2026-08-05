using System;
using System.Reflection;
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
        public static object ConvertTo(object rawId, Type targetType)
        {
            if (rawId == null || targetType == null) return null;
            if (targetType.IsInstanceOfType(rawId)) return rawId;

            Type sourceType = rawId.GetType();

            // A conversion operator can be declared on either side of the conversion, and Unity puts
            // EntityId's on EntityId itself — so looking only at the target type finds the way in
            // but not the way back out.
            object converted = InvokeConversion(targetType, sourceType, targetType, rawId)
                               ?? InvokeConversion(sourceType, sourceType, targetType, rawId);
            if (converted != null) return converted;

            ConstructorInfo constructor = targetType.GetConstructor(new[] { sourceType });
            return constructor?.Invoke(new[] { rawId });
        }

        private static object InvokeConversion(Type declaringType, Type sourceType, Type targetType, object value)
        {
            foreach (MethodInfo method in declaringType.GetMethods(BindingFlags.Static | BindingFlags.Public))
            {
                if (method.Name != "op_Implicit" && method.Name != "op_Explicit") continue;
                if (method.ReturnType != targetType) continue;

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != 1 || !parameters[0].ParameterType.IsAssignableFrom(sourceType)) continue;

                return method.Invoke(null, new[] { value });
            }

            return null;
        }

        /// <summary>
        /// Resolves an id back to its object. Returns null for ids that are not objects at all —
        /// hierarchy scene headers are tree rows with their own ids, and callers rely on that null
        /// to tell them apart from GameObjects.
        ///
        /// Ids read off tree rows are not always the type this Unity version's editor APIs take:
        /// the tree switched to EntityId a version before EditorUtility did, so an id from a row
        /// gets converted rather than rejected.
        /// </summary>
        public static Object Resolve(object rawId)
        {
#if UNITY_6000_4_OR_NEWER
            if (rawId is EntityId entityId) return EditorUtility.EntityIdToObject(entityId);

            return ConvertTo(rawId, typeof(EntityId)) is EntityId converted ? EditorUtility.EntityIdToObject(converted) : null;
#else
            if (rawId is int instanceId) return EditorUtility.InstanceIDToObject(instanceId);

            return ConvertTo(rawId, typeof(int)) is int converted ? EditorUtility.InstanceIDToObject(converted) : null;
#endif
        }
    }
}
