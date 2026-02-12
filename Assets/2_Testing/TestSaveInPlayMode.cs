using DNExtensions.Utilities;
using UnityEngine;



public class TestSaveInPlayMode : MonoBehaviour {
    
    public float speed = 5f;
    [ReadOnly]
    public int health = 100;
    public Vector3 position;
    public Color color = Color.white;


    [ComponentHeaderButton("Test")]
    private void TestButton()
    {
        health += 10;
    }
    
    [ComponentHeaderButton("Test2")]
    private void Test2Button()
    {
        health -= 10;
    }
}