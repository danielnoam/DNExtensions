using UnityEngine;

public class PingPongMovement : MonoBehaviour
{
    [SerializeField] private Vector3 endPosition;
    [SerializeField] private float speed = 2f;
    [SerializeField] private bool useLocalSpace = true;

    private bool _movingForward = true;
    private Vector3 _startPosition;
    private float _t;


    private void Start()
    {
        _startPosition = useLocalSpace ? transform.localPosition : transform.position;
    }

    private void Update()
    {
        _t += Time.deltaTime * speed * (_movingForward ? 1f : -1f);

        if (_t >= 1f)
        {
            _t = 1f;
            _movingForward = false;
        }
        else if (_t <= 0f)
        {
            _t = 0f;
            _movingForward = true;
        }

        Vector3 newPosition = Vector3.Lerp(_startPosition, endPosition, _t);

        if (useLocalSpace)
            transform.localPosition = newPosition;
        else
            transform.position = newPosition;
    }

    private void OnDrawGizmos()
    {
        Vector3 start = Application.isPlaying ? _startPosition : (useLocalSpace ? transform.localPosition : transform.position);
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(start, 0.2f);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(endPosition, 0.2f);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(start, endPosition);
    }
}