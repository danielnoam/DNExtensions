using UnityEngine;
using UnityEngine.Events;

namespace DNExtensions.Systems.ScriptableObjects
{
    [CreateAssetMenu(fileName = "New Float", menuName = "Scriptable Objects/Float")]
    public class SOFloat : ScriptableObject
    {
        [SerializeField] private float value;


        public event UnityAction<float> OnValueChanged;
        
        public float Value
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