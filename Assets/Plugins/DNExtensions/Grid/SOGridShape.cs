using UnityEngine;

namespace DNExtensions.GridSystem
{
    [CreateAssetMenu(fileName = "Grid Shape", menuName = "Scriptable Objects/New Grid Shape")]
    public class SOGridShape : ScriptableObject
    {
        [SerializeField] private Grid grid = new Grid(8,8);
        
        public Grid Grid => grid;
    }
}