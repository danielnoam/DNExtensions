using UnityEngine;

public class AutoRotate : MonoBehaviour
{
    
    public Vector3 amount = Vector3.up;

    private void Update()
    {
        transform.Rotate(amount * Time.deltaTime);
    }
}