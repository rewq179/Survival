using UnityEngine;
using System;

public class CombatModule
{
    private DamageTextMgr damageTextMgr;
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
        damageTextMgr = GameManager.Instance.damageTextMgr;
        UpdateHp();
    }

    public void UpdateHp()
    {
        maxHp = owner.MaxHp;
        maxInvHp = 1f / maxHp;
        curHp = maxHp;
    }

    public void TakeDamage(float damage)
    {
        if (damage == 0)
            return;

        damageTextMgr.ShowDamageText(owner.transform.position, (int)damage, Color.red);
        float preHp = curHp;
        curHp = Mathf.Clamp(curHp - damage, 0, maxHp);
        OnHpChanged?.Invoke(preHp * maxInvHp, curHp * maxInvHp);

        if (curHp <= 0)
            OnDead();
    }

    public void OnDead()
    {
        isDead = true;
        GameManager.Instance.spawnManager.RemoveEnemy(owner);
    }
}
