using UnityEngine;
using System;

public class CombatModule
{
    private DamageTextMgr damageTextMgr;
    private RewardMgr rewardMgr;

    private Unit owner;
    private float maxHp;
    private float maxInvHp;
    private float curHp;
    private bool isDead;

    public event Action<float, float> OnHpChanged;

    public float CurHp => curHp;
    public bool IsDead => isDead;

    public void Reset()
    {
        isDead = false;
    }

    public void Init(Unit owner)
    {
        this.owner = owner;
        damageTextMgr = GameMgr.Instance.damageTextMgr;
        rewardMgr = GameMgr.Instance.rewardMgr;
        UpdateHp();
        curHp = maxHp;
    }

    public void UpdateHp()
    {
        maxHp = owner.MaxHp;
        maxInvHp = 1f / maxHp;
    }

    public void TakeHeal(float healAmount)
    {
        if (healAmount == 0)
            return;

        damageTextMgr.ShowDamageText(owner.transform.position, (int)healAmount, Color.green);
        SetHp(curHp + healAmount);
    }

    public void TakeDamage(float damage)
    {
        if (damage == 0)
            return;

        damageTextMgr.ShowDamageText(owner.transform.position, (int)damage, Color.red);
        SetHp(curHp - damage);

        if (curHp <= 0)
        {
            OnDead();
        }

        else
        {
            owner.PlayAnimation("Take Damage");
        }
    }

    private void SetHp(float hp)
    {
        float prevHp = curHp;
        curHp = Mathf.Clamp(hp, 0, maxHp);
        OnHpChanged?.Invoke(prevHp * maxInvHp, curHp * maxInvHp);
    }

    public void OnDead()
    {
        isDead = true;

        if (!owner.IsPlayer)
            rewardMgr.CreateItem(owner);

        owner.PlayAnimation("Die");
    }
}
