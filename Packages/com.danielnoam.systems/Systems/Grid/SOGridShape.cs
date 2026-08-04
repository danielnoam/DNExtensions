using UnityEngine;

namespace DNExtensions.Systems.Grid
{
    /// <summary>
    /// ScriptableObject that stores a grid shape configuration.
    /// </summary>
    [CreateAssetMenu(fileName = "Grid Shape", menuName = "Scriptable Objects/Grid Shape")]
    public class SOGridShape : ScriptableObject
    {
        [SerializeField] private Grid grid = new Grid();
        
        public Grid Grid => grid;
    }
}