using DNExtensions;
using DNExtensions.Button;
using DNExtensions.SerializedInterface;
using UnityEngine;

public class Test : MonoBehaviour
{

    [Separator("Serialized Interface")]
    [SerializeField] private InterfaceReference<ITest> testInterface;
    [SerializeField, RequireInterface(typeof(ITest))] private MonoBehaviour interactableObject;

}
internal interface ITest
{
}
