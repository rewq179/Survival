using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;

public enum StatType
{
    Health,
    MoveSpeed,
    Defense,
    MagnetRange,
    ExpGain,
    GoldGain,
    CriticalChance,
    CriticalDamage,
    AllSkillRange,
    AllSkillCooldown,
    AllSkillDamage,
    AllSkillDuration,
    Max,
}

public class StatModule
{
    private Dictionary<StatType, float> baseStats = new();
    private Dictionary<StatType, float> statModifiers = new();

    public void Init(UnitData unitData)
    {
        for (StatType type = 0; type < StatType.Max; type++)
        {
            baseStats[type] = 0f;
            statModifiers[type] = 0f;
        }

        baseStats[StatType.Health] = unitData.hp;
        baseStats[StatType.MoveSpeed] = unitData.moveSpeed;
        baseStats[StatType.CriticalChance] = unitData.critcalChance;
        baseStats[StatType.CriticalDamage] = unitData.criticalDamage;
    }

    public float GetFinalStat(StatType statType)
    {
        return baseStats[statType] * (1 + statModifiers[statType]);
    }

    public void AddBaseStatValue(StatType statType, float value)
    {
        baseStats[statType] += value;
    }

    public void AddStatModifier(StatType statType, float value)
    {
        statModifiers[statType] += value;
    }
}
