using UnityEngine;

public abstract class BehaviourModule
{
    protected Unit owner;
    public abstract void Reset();
    public abstract void Init(Unit unit);
    public abstract void Update();

    protected void AttackTarget(Unit target, SkillKey skillKey)
    {
        owner.PlayAnimation(skillKey.ToString());
        target.PlayAnimation("Take Damage");
        owner.AttackTarget(owner, target, skillKey);
    }

    public virtual void OnAttackAnimationEnd()
    {
        
    }
}
