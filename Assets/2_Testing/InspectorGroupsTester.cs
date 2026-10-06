using DNExtensions.Utilities;
using UnityEngine;

public class InspectorGroupsTester : MonoBehaviour
{
    private enum Mode { First = 0, Second = 5, Third = 10 }

    [SerializeField] private bool showBlock;
    [SerializeField] private bool allowEditing;
    [SerializeField] private Mode mode;

    [Foldout("References")]
    [SerializeField] private Transform target;
    [SerializeField] private Rigidbody body;

    [Foldout("Movement")]
    [SerializeField] private float speed = 5f;
    [ShowIf("showBlock")]
    [SerializeField] private float blockA;
    [SerializeField] private float[] blockList;
    [EndIf]
    [SerializeField] private float afterBlock;

    [Foldout("Movement/Jump")]
    [EnableIf("allowEditing")]
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private int jumpCount = 1;
    [EndIf, ShowIf("mode", Mode.Second)]
    [SerializeField] private string onlyInSecond;
    [EndIf]
    [SerializeField] private float coyoteTime = 0.1f;

    [EndFoldout]
    [HideIf("showBlock")]
    [SerializeField] private string singleHidden;
    [SerializeField] private string alwaysVisible;
}
