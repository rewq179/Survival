using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [SerializeField] private Transform target;
    private float smoothSpeed = 0.5f;
    private Vector3 offset = Vector3.up * 10f;

    private void LateUpdate()
    {
        MoveTarget();
    }

    public void SetTarget(Transform target)
    {
        this.target = target;
    }

    private void MoveTarget()
    {
        if (target == null)
            return;

        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;
    }
}
