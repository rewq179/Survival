using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.Behavior;

[BlackboardEnum]
public enum AIState
{
    Chasing,
    Attack,
}

public class BehaviourMonsterModule : BehaviourModule
{
    private BehaviorGraphAgent agent;
    private SkillModule skillModule;

    // 블랙보드
    private Unit target;
    private AIState currentState;
    private float distanceSqr;
    private bool isAttacking;
    private bool hasRangedAttackSkill;
    private bool canRangedAttack;
    private bool canMeleeAttack;

    // 상수
    private const float ATTACK_MELEE_RANGE = 1.5f;
    private const float ATTACK_MELEE_RANGE_SQR = ATTACK_MELEE_RANGE * ATTACK_MELEE_RANGE;
    private const float ATTACK_RANGED_RANGE = 6f;
    private const float ATTACK_RANGED_RANGE_SQR = ATTACK_RANGED_RANGE * ATTACK_RANGED_RANGE;

    public override void Reset()
    {
        SetAIState(AIState.Chasing);
    }

    public override void Init(Unit unit)
    {
        owner = unit;
        target = GameMgr.Instance.PlayerUnit;
        skillModule = owner.SkillModule;

        agent = owner.GetComponent<BehaviorGraphAgent>();
        agent.SetVariableValue("Target", target);
        SetAttacking(false);
        SetHasRangedAttackSkill(owner.HasRangedAttackSkill());
        SetRangedAttackRange(false);
        SetMeleeAttackRange(false);
    }

    public override void UpdateBehaviour()
    {
        distanceSqr = (owner.transform.position - target.transform.position).sqrMagnitude;

        if (hasRangedAttackSkill)
        {
            bool canUseRangedAttack = skillModule.CanUseSkillType(false);
            SetRangedAttackRange(canUseRangedAttack && distanceSqr < ATTACK_RANGED_RANGE_SQR);
        }

        bool canUseMeleeAttack = skillModule.CanUseSkillType(true);
        SetMeleeAttackRange(canUseMeleeAttack && distanceSqr < ATTACK_MELEE_RANGE_SQR);
    }

    public override void SetAttacking(bool isAttacking)
    {
        this.isAttacking = isAttacking;
        agent.SetVariableValue("IsAttacking", isAttacking);
    }

    private void SetHasRangedAttackSkill(bool hasRangedAttackSkill)
    {
        this.hasRangedAttackSkill = hasRangedAttackSkill;
        agent.SetVariableValue("HasRangedAttackSkill", hasRangedAttackSkill);
    }

    public override void SetAIState(AIState state)
    {
        currentState = state;
        agent.SetVariableValue("CurrentState", currentState);
    }

    private void SetRangedAttackRange(bool isInRange)
    {
        canRangedAttack = isInRange;
        agent.SetVariableValue("CanRangedAttack", isInRange);
    }

    private void SetMeleeAttackRange(bool isInRange)
    {
        canMeleeAttack = isInRange;
        agent.SetVariableValue("CanMeleeAttack", isInRange);
    }

    public override void OnAnimationEnd(AnimEvent animEvent)
    {
        switch (animEvent)
        {
            case AnimEvent.Attack:
                SetAttacking(false);
                break;

            case AnimEvent.Die:
                GameMgr.Instance.spawnMgr.RemoveEnemy(owner);
                break;
        }
    }
}
