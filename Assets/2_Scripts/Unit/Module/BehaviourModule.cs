using Unity.Behavior;
using UnityEngine;

public abstract class BehaviourModule
{
    protected Unit owner;
    protected bool isForceMoving;

    public abstract void Reset();
    public abstract void Init(Unit unit);

    public virtual void UpdateBehaviour() { }
    public virtual void UpdateMoveSpeed() { }

    public virtual bool IsAttacking => false;
    public virtual bool IsForceMoving => isForceMoving;
    public virtual void SetForceMoving(bool isForceMoving) => this.isForceMoving = isForceMoving;
    public virtual void SetAIState(AIState state) { }
    public virtual void SetAttacking(bool isAttacking) { }
    public virtual void OnAnimationEnd(AnimEvent animEvent) { }
}
