using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    private PlayerInputAction PlayerInputAction;
    private Animator animator;
    private Vector2 moveInput;
    private bool isAttacking;
    private Rigidbody rigidBody;
    private float moveSpeed;
    private float rotationSpeed = 10f;

    // 스킬 관련
    private Unit owner;
    private SkillMgr skillManager;
    private Camera mainCamera;

    private void Awake()
    {
        PlayerInputAction = new PlayerInputAction();
        rigidBody = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        mainCamera = Camera.main;
    }

    public void Init(Unit owner)
    {
        this.owner = owner;
        skillManager = GameMgr.Instance.skillMgr;
        UpdateMoveSpeed();
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
        skillManager.ActivateSkill(owner);
    }

    private void FixedUpdate()
    {
        if (owner.IsDead)
            return;

        Vector3 movement = new Vector3(moveInput.x, 0, moveInput.y);
        UpdateMovement(movement);
        UpdateRotation(movement);
    }

    public void UpdateMoveSpeed() => moveSpeed = owner.MoveSpeed;
    public Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 10f; // 카메라에서의 거리
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(mousePos);
        worldPos.y = 0f;
        return worldPos;
    }

    private void UpdateMovement(Vector3 movement)
    {
        float CalculateBlendValue(float moveAmount)
        {
            return moveAmount switch
            {
                < 0.05f => 0f,
                < 0.3f => Mathf.Lerp(0f, 0.3f, (moveAmount - 0.05f) * 4f),
                < 0.7f => 0.3f,
                < 0.9f => Mathf.Lerp(0.3f, 1.0f, (moveAmount - 0.7f) * 5f),
                _ => 1.0f
            };
        }

        rigidBody.linearVelocity = movement * moveSpeed;
        float blendValue = CalculateBlendValue(movement.magnitude);
        animator.SetFloat("Movement", blendValue, 0.1f, Time.fixedDeltaTime);
    }

    private void UpdateRotation(Vector3 movement)
    {
        if (movement.sqrMagnitude <= 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(movement, Vector3.up);
        Quaternion newRotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        transform.rotation = newRotation;
    }
}
