using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MovementJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public enum HandlePosition
    {
        RightTop,
        RightBottom,
        LeftTop,
        LeftBottom,
        Max,
    }

    [Header("UI Components")]
    [SerializeField] private RectTransform handle;
    [SerializeField] private RectTransform[] focusButton;
    [SerializeField] private RectTransform joystickPanel;

    private Vector2 startMousePosition;
    private Vector2 currentMousePosition;
    private bool isJoystickActive;

    // 이벤트
    public System.Action<Vector2> OnJoystickMove;
    public System.Action OnJoystickRelease;

    // 상수
    private const float MAX_RADIUS = 60f;
    private const float MAX_RADIUS_SQR = MAX_RADIUS * MAX_RADIUS;
    private const float DEAD_ZONE = 5f;
    private const float DEAD_ZONE_SQR = DEAD_ZONE * DEAD_ZONE;
    private const float RIGHT_ANGLE_MIN = 315f;
    private const float RIGHT_ANGLE_MAX = 45f;
    private const float UP_ANGLE_MIN = 45f;
    private const float UP_ANGLE_MAX = 135f;
    private const float LEFT_ANGLE_MIN = 135f;
    private const float LEFT_ANGLE_MAX = 225f;
    private const float DOWN_ANGLE_MIN = 225f;
    private const float DOWN_ANGLE_MAX = 315f;

    private void Awake()
    {
        SetJoystickActive(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.pointerCurrentRaycast.gameObject == gameObject)
        {
            StartJoystick(eventData.position);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isJoystickActive)
            return;

        UpdateJoystick(eventData.position);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isJoystickActive)
            return;

        EndJoystick();
    }

    private void StartJoystick(Vector2 position)
    {
        startMousePosition = position;
        currentMousePosition = position;

        SetJoystickActive(true);
        joystickPanel.position = startMousePosition;
        handle.anchoredPosition = Vector2.zero;
        SetAllFocusButtons(false);
    }

    private void UpdateJoystick(Vector2 position)
    {
        currentMousePosition = position;

        Vector2 direction = currentMousePosition - startMousePosition;
        float sqrDistance = direction.sqrMagnitude;

        UpdatePlayerMovement(direction, sqrDistance);
        UpdateHandlePosition(direction, sqrDistance);
        UpdateFocusButtons(direction, sqrDistance);
    }

    private void EndJoystick()
    {
        SetJoystickActive(false);
        handle.anchoredPosition = Vector2.zero;
        SetAllFocusButtons(false);
        OnJoystickRelease?.Invoke();
    }

    private void SetJoystickActive(bool active)
    {
        isJoystickActive = active;
        joystickPanel.gameObject.SetActive(active);
    }

    private void SetAllFocusButtons(bool active)
    {
        foreach (RectTransform focus in focusButton)
        {
            focus.gameObject.SetActive(active);
        }
    }

    private void UpdatePlayerMovement(Vector2 direction, float sqrDistance)
    {
        if (sqrDistance < DEAD_ZONE_SQR)
        {
            OnJoystickMove.Invoke(Vector2.zero);
        }
        else
        {
            OnJoystickMove.Invoke(direction.normalized);
        }
    }

    private void UpdateHandlePosition(Vector2 direction, float sqrDistance)
    {
        if (sqrDistance < DEAD_ZONE_SQR)
        {
            handle.anchoredPosition = Vector2.zero;
            return;
        }

        if (sqrDistance > MAX_RADIUS_SQR)
        {
            direction = direction.normalized * MAX_RADIUS;
        }

        handle.anchoredPosition = direction;
    }

    private void UpdateFocusButtons(Vector2 direction, float sqrDistance)
    {
        SetAllFocusButtons(false);
        if (sqrDistance < DEAD_ZONE_SQR)
            return;

        HandlePosition type = GetFocusPositionFromAngle(direction);
        focusButton[(int)type].gameObject.SetActive(true);
    }

    private HandlePosition GetFocusPositionFromAngle(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (angle < 0)
            angle += 360f;

        // 오른쪽(우상단, 우하단)
        if (angle >= RIGHT_ANGLE_MIN || angle < RIGHT_ANGLE_MAX)
        {
            if (angle >= RIGHT_ANGLE_MIN || angle < 0f)
                return HandlePosition.RightBottom;
            else
                return HandlePosition.RightTop;
        }

        // 위쪽(상단, 중앙)
        else if (angle >= UP_ANGLE_MIN && angle < UP_ANGLE_MAX)
        {
            if (angle >= UP_ANGLE_MIN && angle < 90f)
                return HandlePosition.RightTop;
            else
                return HandlePosition.LeftTop;
        }

        // 왼쪽(좌상단, 좌하단)
        else if (angle >= LEFT_ANGLE_MIN && angle < LEFT_ANGLE_MAX)
        {
            if (angle >= LEFT_ANGLE_MIN && angle < 180f)
                return HandlePosition.LeftTop;
            else
                return HandlePosition.LeftBottom;
        }

        // 아래쪽(하단, 중앙)
        else
        {
            if (angle >= DOWN_ANGLE_MIN && angle < 270f)
                return HandlePosition.LeftBottom;
            else
                return HandlePosition.RightBottom;
        }
    }
}
