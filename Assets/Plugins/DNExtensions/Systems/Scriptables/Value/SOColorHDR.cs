using UnityEngine;

namespace DNExtensions.Systems.Scriptables
{
    /// <summary>
    /// ScriptableObject wrapper for an HDR <see cref="Color"/> value.
    /// </summary>
    [CreateAssetMenu(fileName = "New HDR Color", menuName = "Scriptables/HDR Color")]
    public class SOColorHDR : SOValue<Color> { }
}