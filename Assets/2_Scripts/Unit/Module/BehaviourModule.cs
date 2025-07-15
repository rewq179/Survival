using UnityEngine;

public abstract class BehaviourModule
{
    protected Unit owner;
    public abstract void Reset();
    public abstract void Init(Unit unit);
    public abstract void Update();

    public virtual void UpdateMoveSpeed() { }
    public virtual void OnAnimationEnd(AnimEvent animEvent) { }
}
