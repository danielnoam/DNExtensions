using System;
using DNExtensions.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DNExtensions.Utilities.Button;

namespace DNExtensions.Utilities.Springs
{
    
    public class VisualizedSpring : MonoBehaviour
    {
        [SerializeField] private Vector3Spring spring = new Vector3Spring();


        private void Update()
        {
            spring.Update(Time.deltaTime);
        }


        [Button]
        private void ToggleSpring()
        {
            if (spring.IsLocked)
            {
                spring.Unlock();
            }
            else
            {
                spring.Lock();
            }

        }

        [Button]
        private void SetSpringValue()
        {
            spring.SetValue(new Vector3(0, 15, 0));
        }


        [Button]
        private void AddSpringValue(Vector3 amount)
        {
            spring.SetValue(spring.Value + amount);
        }


        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawCube(transform.position + spring.target, Vector3.one);

            Gizmos.DrawLine(transform.position + spring.target, transform.position + spring.Value);

            Gizmos.color = spring.IsLocked ? Color.red : Color.yellow;
            Gizmos.DrawWireCube(transform.position + spring.Value, Vector3.one / 1.5f);



            if (spring.useLimits)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireCube(transform.position, spring.max);
                Gizmos.DrawWireCube(transform.position, spring.min);
            }

        }
    }
}