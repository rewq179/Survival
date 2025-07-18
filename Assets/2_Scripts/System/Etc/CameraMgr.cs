using UnityEngine;

public class CameraMgr : MonoBehaviour
{
    [SerializeField] private Transform target;
    private float smoothSpeed = 0.5f;
    private readonly Vector3 offset = new Vector3(0, 8f, -5f);

    public Vector3 Offset => offset;

    private void LateUpdate()
    {
        MoveTarget();
    }

    public void SetTarget(Transform target) => this.target = target;
    private void MoveTarget()
    {
        if (target == null)
            return;

        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;
    }
}
