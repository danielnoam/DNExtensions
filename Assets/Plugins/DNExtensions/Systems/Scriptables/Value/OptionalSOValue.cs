using System;
using UnityEngine;

namespace DNExtensions.Systems.Scriptables
{
    /// <summary>
    /// Holds either a reference to an <see cref="SOValue{T}"/> or a local fallback value.
    /// When an SO is assigned its value is used; otherwise the fallback is used.
    /// </summary>
    [Serializable]
    public class OptionalSOValue<T>
    {
        public SOValue<T> soValue;
        public T fallbackValue;

        public T Value => soValue ? soValue.Value : fallbackValue;
    }

    [Serializable] public class OptionalSOBool : OptionalSOValue<bool> { }
    [Serializable] public class OptionalSOInt : OptionalSOValue<int> { }
    [Serializable] public class OptionalSOFloat : OptionalSOValue<float> { }
    [Serializable] public class OptionalSOString : OptionalSOValue<string> { }
    [Serializable] public class OptionalSOColor : OptionalSOValue<Color> { }
    [Serializable] public class OptionalSOVector2 : OptionalSOValue<Vector2> { }
    [Serializable] public class OptionalSOVector2Int : OptionalSOValue<Vector2Int> { }
    [Serializable] public class OptionalSOVector3 : OptionalSOValue<Vector3> { }
    [Serializable] public class OptionalSOVector3Int : OptionalSOValue<Vector3Int> { }
    [Serializable] public class OptionalSOVector4 : OptionalSOValue<Vector4> { }
    [Serializable] public class OptionalSOQuaternion : OptionalSOValue<Quaternion> { }
    [Serializable] public class OptionalSOLayerMask : OptionalSOValue<LayerMask> { }
    [Serializable] public class OptionalSOAnimationCurve : OptionalSOValue<AnimationCurve> { }
    [Serializable] public class OptionalSOSprite : OptionalSOValue<Sprite> { }
}