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
    public BuffKey buffKey;

    public void Reset()
    {
        attacker = null;
        defender = null;
        damage = 0;
        damageFinal = 0;
        hitPoint = Vector3.zero;
        skillKey = SkillKey.None;
        buffKey = BuffKey.None;
    }

    public void Init(Unit attacker, Unit defender, float damage, Vector3 hitPoint, SkillKey skillKey)
    {
        Init(attacker, defender, damage, hitPoint);
        this.skillKey = skillKey;
    }

    public void Init(Unit attacker, Unit defender, float damage, Vector3 hitPoint, BuffKey buffKey)
    {
        Init(attacker, defender, damage, hitPoint);
        this.buffKey = buffKey;
    }

    private void Init(Unit attacker, Unit defender, float damage, Vector3 hitPoint)
    {
        this.attacker = attacker;
        this.defender = defender;
        this.damage = damage;
        this.hitPoint = hitPoint;
    }
}

/// <summary>
/// 전투 처리 시스템
/// </summary>
public static class CombatMgr
{
    private static Stack<DamageInfo> damageStack = new();
    private static Stack<BuffInstance> buffInstancePool = new();

    public static void ApplyDamageBySkill(Unit attacker, Unit defender, float damage, Vector3 hitPoint, SkillKey skillKey)
    {
        DamageInfo damageInfo = PopDamageInfo();
        damageInfo.Init(attacker, defender, damage, hitPoint, skillKey);
        ProcessDamage(damageInfo);
    }

    public static void ApplyDamageByBuff(Unit attacker, Unit defender, float damage, Vector3 hitPoint, BuffKey buffKey)
    {
        DamageInfo damageInfo = PopDamageInfo();
        damageInfo.Init(attacker, defender, damage, hitPoint, buffKey);
        ProcessDamage(damageInfo);
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

    public static BuffInstance PopBuffInstance()
    {
        if (buffInstancePool.TryPop(out BuffInstance instance))
            return instance;

        return new BuffInstance();
    }

    public static void PushBuffInstance(BuffInstance instance)
    {
        instance.Reset();
        buffInstancePool.Push(instance);
    }
}
