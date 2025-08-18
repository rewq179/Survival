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
            owner.PlayAnimation(AnimationType.TakeDamage);
        }
    }

    private void SetHp(float hp)
    {
        float prevHp = curHp;
        curHp = Mathf.Clamp(hp, 0, maxHp);

        float prev = prevHp * maxInvHp;
        float cur = curHp * maxInvHp;

        if (owner.IsPlayer)
            GameEvents.Instance.PlayerHpChanged(prev, cur);
        else
            OnHpChanged?.Invoke(prev, cur);
    }

    public void OnDead()
    {
        isDead = true;

        if (owner.IsPlayer)
        {
            UIMgr.Instance.gameOverUI.ShowGameOverUI();
        }

        else
        {
            rewardMgr.CreateItem(owner);
            owner.ShowHealthBar(false);
        }

        owner.PlayAnimation(AnimationType.Die);
    }
}
