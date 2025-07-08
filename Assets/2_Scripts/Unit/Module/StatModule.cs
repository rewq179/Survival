using UnityEngine;
using System.Collections.Generic;

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
    private float health;
    private float moveSpeed;
    private Dictionary<StatType, float> statModifiers = new();

    public void Init(UnitData unitData)
    {
        for (StatType type = 0; type < StatType.Max; type++)
        {
            statModifiers[type] = 0f;
        }

        health = unitData.hp;
        moveSpeed = unitData.moveSpeed;
    }

    public float GetFinalStat(StatType type)
    {
        float baseStat = type switch
        {
            StatType.Health => health,
            StatType.MoveSpeed => moveSpeed,
            _ => 0f,
        };

        return baseStat * (1 + statModifiers[type]);
    }

    public void AddStatModifier(StatType type, float value)
    {
        statModifiers[type] += value;
    }
}
