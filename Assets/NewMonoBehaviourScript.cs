using DNExtensions.Utilities.AutoGet;
using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{


    [SerializeField, AutoGetSelf] private Rigidbody rb;
    [SerializeField, AutoGetScene(Tags = new []{"Player"})] private Transform player;
    
    
}
