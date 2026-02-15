using UnityEngine;
using UnityEngine.Events;

namespace DNExtensions.Systems.ScriptableObjects
{
    [CreateAssetMenu(fileName = "New Int", menuName = "Scriptable Objects/Int")]
    public class SOInt : ScriptableObject
    {
        [SerializeField] private int value;


        public event UnityAction<int> OnValueChanged;
        
        public int Value
        {
            get => value;
            set
            {
                if (Mathf.Approximately(this.value, value)) return;
                this.value = value;
                OnValueChanged?.Invoke(this.value);
            }
        }
        
        
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                OnValueChanged?.Invoke(value);
            }
        }
    }
}