using UnityEngine;

namespace DNExtensions.Utilities.GridSystem
{
    [CreateAssetMenu(fileName = "Grid Shape", menuName = "Scriptable Objects/New Grid Shape")]
    public class SOGridShape : ScriptableObject
    {
        [SerializeField] private Grid grid = new Grid(8,8);
        
        public Grid Grid => grid;
    }
}