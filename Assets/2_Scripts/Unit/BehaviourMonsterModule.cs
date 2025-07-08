using UnityEngine;

public enum AIState
{
    Chasing,
    Attacking,
    Stun
}

public class BehaviourMonsterModule : BehaviourModule
{
    private Transform playerTransform;
    private AIState currentState = AIState.Chasing;

    // 이동 관련
    private float moveSpeed;
    private const float ROTATION_SPEED = 5f;
    private const float ATTACK_RANGE = 1.5f * 1.5f;

    // 애니메이션 관련
    private bool isAttacking;


    // 성능 관련
    private Vector3 lastPlayerPosition;
    private const float POSITION_UPDATE_INTERVAL = 0.1f;
    private float lastPlayerUpdateTime;

    public override void Reset()
    {
        currentState = AIState.Chasing;
    }

    public override void Init(Unit unit)
    {
        owner = unit;
        playerTransform = GameManager.Instance.PlayerUnit.transform;
        moveSpeed = unit.MoveSpeed;
    }

    public override void Update()
    {
        if (owner.IsDead)
            return;

        if (IsCheckPlayerPosition())
            UpdatePlayerPosition();
        UpdateState();
        UpdateMovement();
    }

    private bool IsCheckPlayerPosition()
    {
        return Time.time - lastPlayerUpdateTime > POSITION_UPDATE_INTERVAL;
    }

    private void UpdatePlayerPosition()
    {
        lastPlayerPosition = playerTransform.position;
        lastPlayerUpdateTime = Time.time;
    }

    private void UpdateState()
    {
        float distanceSqr = (owner.transform.position - lastPlayerPosition).sqrMagnitude;
        if (distanceSqr <= ATTACK_RANGE)
            ChangeState(AIState.Attacking);
        else
            ChangeState(AIState.Chasing);
    }

    private void UpdateMovement()
    {
        switch (currentState)
        {
            case AIState.Chasing:
                ChasePlayer();
                break;

            case AIState.Attacking:
                AttackPlayer();
                break;
        }
    }

    private void ChasePlayer()
    {
        Vector3 direction = GetDirection();
        Transform transform = owner.transform;
        transform.position += direction * moveSpeed * Time.deltaTime;

        if (direction != Vector3.zero)
        {
            transform.rotation = GetRotation(transform.rotation, direction);
        }
    }

    private void AttackPlayer()
    {
        if (isAttacking)
            return;

        isAttacking = true;
        Vector3 direction = GetDirection();

        if (direction != Vector3.zero)
        {
            Transform transform = owner.transform;
            transform.rotation = GetRotation(transform.rotation, direction, 2f);
        }

        AttackTarget(GameManager.Instance.PlayerUnit, SkillKey.StingAttack);
    }

    public override void OnAttackAnimationEnd()
    {
        isAttacking = false;
        UpdatePlayerPosition();
        ChangeState(AIState.Chasing);
    }

    #region 유틸리티

    private Vector3 GetDirection()
    {
        Vector3 direction = (lastPlayerPosition - owner.transform.position).normalized;
        direction.y = 0f;
        return direction;
    }

    private Quaternion GetRotation(Quaternion rotation, Vector3 direction, float speedFactor = 1f)
    {
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        return Quaternion.Slerp(rotation, targetRotation, ROTATION_SPEED * speedFactor * Time.deltaTime);
    }

    private void ChangeState(AIState newState)
    {
        if (isAttacking)
            return;

        currentState = newState;
    }

    #endregion
}
