using UnityEngine;

public enum AnimationType
{
    Attack = 0,
    TakeDamage = 1,
    Die = 2,
    Movement = 3,
}

public abstract class BehaviourModule
{
    protected Unit owner;
    protected Animator animator;

    protected bool isForceMoving;
    protected AnimationType animationType;

    public abstract void Reset();
    public virtual void Init(Unit unit)
    {
        owner = unit;
        animator = owner.GetComponent<Animator>();
    }

    public virtual void UpdateBehaviour() { }
    public virtual void UpdateMoveSpeed() { }

    public virtual bool IsForceMoving => isForceMoving;
    public virtual void SetAIState(AIState state) { }
    public virtual void SetForceMoving(bool isForceMoving) => this.isForceMoving = isForceMoving;

    // 애니메이션
    public virtual bool IsAttacking => false;
    public virtual void SetAttacking(bool isAttacking) { }

    public bool PlayAnimation(AnimationType type) => PlayAnimation(type.ToString(), type);
    public bool PlayAnimation(string name, AnimationType type)
    {
        if (animationType < type)
            return false;

        animationType = type;
        animator.Play(name);
        return true;
    }

    public virtual void OnAnimationEnd(AnimEvent animEvent)
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
