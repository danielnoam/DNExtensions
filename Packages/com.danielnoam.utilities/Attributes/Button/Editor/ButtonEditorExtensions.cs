using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace DNExtensions.Utilities.Button
{
    internal static class ButtonEditorExtensions
    {
        /// <summary>
        /// Returns true if the field is serialized by Unity — public non-excluded fields,
        /// or private fields marked with <see cref="SerializeField"/> or <see cref="SerializeReference"/>.
        /// </summary>
        internal static bool IsSerializedByUnity(this FieldInfo field)
        {
            if (field.GetCustomAttribute<SerializeReference>() != null) return true;
            if (field.IsPublic) return field.GetCustomAttribute<NonSerializedAttribute>() == null;
            return field.GetCustomAttribute<SerializeField>() != null;
        }

        /// <summary>
        /// Returns true if the type is a plain serializable class — not a <see cref="UnityEngine.Object"/>,
        /// not a primitive, and decorated with <see cref="SerializableAttribute"/>.
        /// </summary>
        internal static bool IsSerializableClass(this Type type)
        {
            if (!type.IsClass) return false;
            if (type == typeof(string)) return false;
            if (typeof(UnityEngine.Object).IsAssignableFrom(type)) return false;
            return type.GetCustomAttribute<SerializableAttribute>() != null;
        }

        /// <summary>
        /// Returns true if the instance's type is a serializable class with at least one
        /// <see cref="ButtonAttribute"/>-decorated method.
        /// </summary>
        internal static bool HasButtonMethods(this object instance)
        {
            var type = instance.GetType();
            if (!type.IsSerializableClass()) return false;

            return type
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Any(m => m.GetCustomAttribute<ButtonAttribute>() != null);
        }

        /// <summary>
        /// Returns true if the type is supported as a button method parameter or inspector field.
        /// </summary>
        internal static bool IsButtonSupportedType(this Type type)
        {
            if (type == typeof(int) || type == typeof(float) || type == typeof(double) ||
                type == typeof(long) || type == typeof(string) || type == typeof(bool))
                return true;
            if (type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Vector4) ||
                type == typeof(Vector2Int) || type == typeof(Vector3Int))
                return true;
            if (type == typeof(Color) || type == typeof(Color32)) return true;
            if (type == typeof(Rect) || type == typeof(RectInt)) return true;
            if (type == typeof(Bounds) || type == typeof(BoundsInt)) return true;
            if (type == typeof(AnimationCurve) || type == typeof(Gradient)) return true;
            if (type == typeof(LayerMask)) return true;
            if (type.IsEnum) return true;
            if (typeof(UnityEngine.Object).IsAssignableFrom(type)) return true;
            if (type.IsArray && type.GetElementType() == typeof(string)) return true;
            return false;
        }

        /// <summary>
        /// Returns the default value for a given type, using the declared parameter default if available.
        /// </summary>
        internal static object GetDefaultValue(this ParameterInfo parameter)
        {
            return parameter.HasDefaultValue ? parameter.DefaultValue : parameter.ParameterType.GetDefaultValue();
        }

        /// <summary>
        /// Returns the default value for a given type.
        /// </summary>
        internal static object GetDefaultValue(this Type type)
        {
            if (type == typeof(string)) return "";
            if (type == typeof(int)) return 0;
            if (type == typeof(float)) return 0f;
            if (type == typeof(double)) return 0.0;
            if (type == typeof(long)) return 0L;
            if (type == typeof(bool)) return false;
            if (type == typeof(Vector2)) return Vector2.zero;
            if (type == typeof(Vector3)) return Vector3.zero;
            if (type == typeof(Vector4)) return Vector4.zero;
            if (type == typeof(Vector2Int)) return Vector2Int.zero;
            if (type == typeof(Vector3Int)) return Vector3Int.zero;
            if (type == typeof(Color)) return Color.white;
            if (type == typeof(Color32)) return (Color32)Color.white;
            if (type == typeof(Rect)) return new Rect(0, 0, 100, 100);
            if (type == typeof(RectInt)) return new RectInt(0, 0, 100, 100);
            if (type == typeof(Bounds)) return new Bounds();
            if (type == typeof(BoundsInt)) return new BoundsInt();
            if (type == typeof(AnimationCurve)) return AnimationCurve.Linear(0, 0, 1, 1);
            if (type == typeof(Gradient)) return new Gradient();
            if (type == typeof(LayerMask)) return (LayerMask)0;
            if (type.IsEnum) return Enum.GetValues(type).GetValue(0);
            if (typeof(UnityEngine.Object).IsAssignableFrom(type)) return null;
            if (type.IsArray) return Array.CreateInstance(type.GetElementType() ?? throw new InvalidOperationException(), 0);
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }
    }
}