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
        
        // 스킬 매니저를 통해 공격 실행
        GameMgr.Instance.skillMgr.ExecuteMonsterAttack(skillKey, owner, target);
    }

    public virtual void OnAttackAnimationEnd()
    {
        
    }
}
