using DNExtensions.Utilities;
using UnityEngine;

public class TestSaveInPlayMode : MonoBehaviour {
    
    public float speed = 5f;
    public int health = 100;
    public Vector3 position;
    public Color color = Color.white;


    [ComponentHeaderButton("Test")]
    private void TestButton()
    {
        Debug.Log("Button was pressed");
        health += 10;
    }
    
    [ComponentHeaderButton("Test2")]
    private void Test2Button()
    {
        Debug.Log("Button 2 was pressed");
    }
}