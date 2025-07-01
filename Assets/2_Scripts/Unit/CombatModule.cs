using UnityEngine;
using System;

public class CombatModule
{
    private float maxHp;
    private float curHp;
    private bool isDead;

    public event Action<float> OnHpChanged;

    public float CurHp => curHp;
    public bool IsDead => isDead;

    public void Reset()
    {
        isDead = false;
    }

    public void SetHp(StatModule statModule)
    {
        maxHp = statModule.MaxHp;
        curHp = maxHp;
    }

    public void TakeDamage(float damage)
    {
        if (damage == 0)
            return;

        curHp = Mathf.Clamp(curHp - damage, 0, maxHp);
        OnHpChanged?.Invoke(curHp);

        if (curHp <= 0)
            OnDead();
    }

    public void OnDead()
    {
        isDead = true;
        // 죽음 로직
    }

    public void AttackTarget(Unit attacker, Unit target, SkillKey skillKey)
    {
        GameManager.Instance.damageTextMgr.ShowDamageText(target.transform.position, 10, Color.red);
    }
}
