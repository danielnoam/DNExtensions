using UnityEngine;

namespace DNExtensions.Systems.Scriptables
{
    [CreateAssetMenu(fileName = "New Color", menuName = "Scriptables/Color")]
    public class SOColor : SOValue<Color>
    {
#pragma warning disable 0414
        [SerializeField] private bool showAlpha = true;
        [SerializeField] private bool isHDR;
#pragma warning restore 0414
    }
}