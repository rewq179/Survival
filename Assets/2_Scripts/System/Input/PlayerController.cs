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

    private void Awake()
    {
        PlayerInputAction = new PlayerInputAction();
        rigidBody = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        // 액션 활성화
        PlayerInputAction.Player.Enable();

        // 이벤트 구독
        PlayerInputAction.Player.Move.performed += OnMove;
        PlayerInputAction.Player.Move.canceled += OnMove;
        PlayerInputAction.Player.Attack.performed += OnAttack;
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제
        PlayerInputAction.Player.Move.performed -= OnMove;
        PlayerInputAction.Player.Move.canceled -= OnMove;
        PlayerInputAction.Player.Attack.performed -= OnAttack;

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

    private void FixedUpdate()
    {
        // 3D 이동 처리
        Vector3 movement = new Vector3(moveInput.x, 0, moveInput.y);
        rigidBody.linearVelocity = movement * moveSpeed;

        // 공격 처리
        if (isAttacking)
        {
            // 공격 로직
        }
    }
}
