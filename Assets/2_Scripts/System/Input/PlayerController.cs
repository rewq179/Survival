using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    private PlayerInputAction PlayerInputAction;
    private Vector2 moveInput;
    private bool isAttacking;
    private Rigidbody rigidBody;
    private float moveSpeed = 5f;
    private float rotationSpeed = 10f;

    // 스킬 관련
    private SkillManager skillManager;
    private Camera mainCamera;

    private void Awake()
    {
        PlayerInputAction = new PlayerInputAction();
        rigidBody = GetComponent<Rigidbody>();
        mainCamera = Camera.main;
    }

    private void OnEnable()
    {
        // 액션 활성화
        PlayerInputAction.Player.Enable();

        // 이벤트 구독
        PlayerInputAction.Player.Move.performed += OnMove;
        PlayerInputAction.Player.Move.canceled += OnMove;
        PlayerInputAction.Player.Attack.performed += OnAttack;
        PlayerInputAction.Player.Attack.performed += OnSkillActivate;
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제
        PlayerInputAction.Player.Move.performed -= OnMove;
        PlayerInputAction.Player.Move.canceled -= OnMove;
        PlayerInputAction.Player.Attack.performed -= OnAttack;
        PlayerInputAction.Player.Attack.performed -= OnSkillActivate;

        // 액션 비활성화
        PlayerInputAction.Player.Disable();
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void OnAttack(InputAction.CallbackContext context)
    {
        isAttacking = context.performed;
    }

    private void OnSkillActivate(InputAction.CallbackContext context)
    {
        if (skillManager == null)
            skillManager = GameManager.Instance.skillManager;

        skillManager.ActivateSkill();
    }

    private void FixedUpdate()
    {
        Vector3 movement = new Vector3(moveInput.x, 0, moveInput.y);
        rigidBody.linearVelocity = movement * moveSpeed;

        if (movement.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movement, Vector3.up);
            Quaternion newRotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
            transform.rotation = newRotation;
        }

        if (isAttacking)
        {
            // 공격 로직
        }
    }

    public Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 10f; // 카메라에서의 거리
        return mainCamera.ScreenToWorldPoint(mousePos);
    }
}
