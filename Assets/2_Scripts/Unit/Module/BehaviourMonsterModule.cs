using System.Collections.Generic;
using UnityEngine;

public enum AIState
{
    Chasing,
    MeleeAttack,
    RangedAttack,
    Stun
}

public class BehaviourMonsterModule : BehaviourModule
{
    private Unit target;
    private SkillModule skillModule;
    private AIState currentState = AIState.Chasing;
    private float moveSpeed;
    private bool isAttacking;
    private Vector3 lastPlayerPosition;

    // 상수
    private const float ROTATION_SPEED = 5f;
    private const float CHASE_ROTION_SPEED_FACTOR = 1f;
    private const float ATTACK_ROTION_SPEED_FACTOR = 2f;
    private const float MELEE_ATTACK_RANGE = 1.5f;
    private const float MELEE_ATTACK_RANGE_SQR = MELEE_ATTACK_RANGE * MELEE_ATTACK_RANGE;
    private const float RANGED_ATTACK_RANGE = 8f;
    private const float RANGED_ATTACK_RANGE_SQR = RANGED_ATTACK_RANGE * RANGED_ATTACK_RANGE;

    public override void Reset()
    {
        currentState = AIState.Chasing;
        isAttacking = false;
    }

    public override void Init(Unit unit)
    {
        owner = unit;
        target = GameMgr.Instance.PlayerUnit;
        moveSpeed = unit.MoveSpeed;
        skillModule = unit.SkillModule;
    }

    public override void Update()
    {
        if (owner.IsDead || target.IsDead)
            return;

        UpdatePlayerPosition();
        UpdateState();
        UpdateMovement();
    }

    private void UpdatePlayerPosition() => lastPlayerPosition = target.transform.position;
    public override void UpdateMoveSpeed() => moveSpeed = owner.MoveSpeed;

    private void UpdateState()
    {
        float distanceSqr = (owner.transform.position - lastPlayerPosition).sqrMagnitude;
        if (distanceSqr <= MELEE_ATTACK_RANGE_SQR && skillModule.CanUseSkillType(true))
            ChangeState(AIState.MeleeAttack);
        else if (distanceSqr <= RANGED_ATTACK_RANGE_SQR && skillModule.CanUseSkillType(false))
            ChangeState(AIState.RangedAttack);
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

            case AIState.MeleeAttack:
            case AIState.RangedAttack:
                AttackPlayer();
                break;
        }
    }

    private void ChasePlayer()
    {
        Vector3 direction = GetDirection();
        Transform transform = owner.transform;
        transform.position += direction * moveSpeed * Time.deltaTime;
        transform.rotation = GetRotation(transform.rotation, direction, CHASE_ROTION_SPEED_FACTOR);
    }

    private void AttackPlayer()
    {
        if (isAttacking)
            return;

        isAttacking = true;
        Vector3 direction = GetDirection();

        Transform transform = owner.transform;
        transform.rotation = GetRotation(transform.rotation, direction, ATTACK_ROTION_SPEED_FACTOR);
        skillModule.UseRandomSkill(target, currentState);
    }

    public override void OnAnimationEnd(AnimEvent animEvent)
    {
        switch (animEvent)
        {
            case AnimEvent.Attack:
                isAttacking = false;
                UpdatePlayerPosition();
                ChangeState(AIState.Chasing);
                break;

            case AnimEvent.Die:
                GameMgr.Instance.spawnMgr.RemoveEnemy(owner);
                break;
        }
    }

    #region 유틸리티

    private Vector3 GetDirection()
    {
        Vector3 direction = (lastPlayerPosition - owner.transform.position).normalized;
        direction.y = 0f;
        return direction;
    }

    private Quaternion GetRotation(Quaternion rotation, Vector3 direction, float speedFactor)
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
