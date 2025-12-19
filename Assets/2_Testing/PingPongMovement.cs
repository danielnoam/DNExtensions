using UnityEngine;

public class PingPongMovement : MonoBehaviour
{
    [SerializeField] private Vector3 endPosition;
    [SerializeField] private float speed = 2f;
    [SerializeField] private bool useLocalSpace = true;

    private Vector3 startPosition;
    private float t = 0f;
    private bool movingForward = true;

    private void Start()
    {
        startPosition = useLocalSpace ? transform.localPosition : transform.position;
    }

    private void Update()
    {
        t += Time.deltaTime * speed * (movingForward ? 1f : -1f);

        if (t >= 1f)
        {
            t = 1f;
            movingForward = false;
        }
        else if (t <= 0f)
        {
            t = 0f;
            movingForward = true;
        }

        Vector3 newPosition = Vector3.Lerp(startPosition, endPosition, t);

        if (useLocalSpace)
            transform.localPosition = newPosition;
        else
            transform.position = newPosition;
    }

    private void OnDrawGizmos()
    {
        Vector3 start = Application.isPlaying ? startPosition : (useLocalSpace ? transform.localPosition : transform.position);
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(start, 0.2f);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(endPosition, 0.2f);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(start, endPosition);
    }
}