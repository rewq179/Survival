using UnityEngine;
using System;

public class CombatModule
{
    private DamageTextMgr damageTextMgr;
    private Unit owner;
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

    public void Init(Unit owner)
    {
        this.owner = owner;
        damageTextMgr = GameManager.Instance.damageTextMgr;
        maxHp = owner.MaxHp;
        curHp = maxHp;
    }

    public void UpdateHp()
    {
        maxHp = owner.MaxHp;
        curHp = maxHp;
    }

    public void TakeDamage(float damage)
    {
        if (damage == 0)
            return;

        damageTextMgr.ShowDamageText(owner.transform.position, (int)damage, Color.red);
        curHp = Mathf.Clamp(curHp - damage, 0, maxHp);
        OnHpChanged?.Invoke(curHp);

        if (curHp <= 0)
            OnDead();
    }

    public void OnDead()
    {
        isDead = true;
        GameManager.Instance.spawnManager.RemoveEnemy(owner);
    }
}
