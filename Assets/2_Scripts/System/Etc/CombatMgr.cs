using UnityEngine;
using System.Collections.Generic;

public struct DamageInfo
{
    public Unit attacker;
    public Unit defender;
    public float damage;
    public float damageFinal;
    public Vector3 hitPoint;
    public SkillKey skillKey;

    public void Reset()
    {
        attacker = null;
        defender = null;
        damage = 0;
        damageFinal = 0;
        hitPoint = Vector3.zero;
        skillKey = SkillKey.None;
    }

    public void Init(Unit attacker, Unit defender, float damage, Vector3 hitPoint, SkillKey skillKey)
    {
        this.attacker = attacker;
        this.defender = defender;
        this.damage = damage;
        this.hitPoint = hitPoint;
        this.skillKey = skillKey;
    }
}

/// <summary>
/// 데미지 처리 시스템
/// </summary>
public static class CombatMgr
{
    private static Stack<DamageInfo> damageStack = new();

    public static void PushDamage(DamageInfo damageInfo)
    {
        damageStack.Push(damageInfo);
    }

    public static void ProcessDamage(DamageInfo damageInfo)
    {
        damageInfo.damageFinal = damageInfo.damage;

        // 1. 치명타 계산
        CalculateCriticalDamage(damageInfo);
        // 2. 데미지 감소 계산
        CalculateDamageReduction(damageInfo);
        // 3. 최종 데미지 적용
        damageInfo.defender.TakeDamage(damageInfo.damageFinal);
        // 4. 회수
        PushDamageInfo(damageInfo);
    }

    private static void CalculateCriticalDamage(DamageInfo damageInfo)
    {
        Unit attacker = damageInfo.attacker;

        float chance = attacker.GetFinalStat(StatType.CriticalChance);
        if (Random.Range(0f, 1f) < chance)
            damageInfo.damageFinal *= 1 + attacker.GetFinalStat(StatType.CriticalDamage);
    }

    private static void CalculateDamageReduction(DamageInfo damageInfo)
    {
        damageInfo.damageFinal *= 1 - damageInfo.defender.GetFinalStat(StatType.Defense);
    }
    
    public static DamageInfo PopDamageInfo()
    {
        if (!damageStack.TryPop(out DamageInfo damageInfo))
            return new DamageInfo();

        damageInfo.Reset();
        return damageInfo;
    }

    public static void PushDamageInfo(DamageInfo damageInfo)
    {
        damageStack.Push(damageInfo);
    }
}
