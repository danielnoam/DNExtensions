using DNExtensions.Utilities;
using UnityEngine;

public class TestSaveInPlayMode : MonoBehaviour {
    
    public float speed = 5f;
    public int health = 100;
    public Vector3 position;
    public Color color = Color.white;
    
    void Update() {
        // Modify values during play mode
        speed += Time.deltaTime;
        health--;
        position = transform.position;
    }


    [ComponentHeaderButton("Icon")]
    private void TestButton()
    {
        Debug.Log("Button was pressed");
    }
}