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

    public float MaxHP => health * (1 + statModifiers[StatType.Health]);
    public float MoveSpeed => moveSpeed * (1 + statModifiers[StatType.MoveSpeed]);

    public void Reset()
    {
        health = 0f;
        moveSpeed = 0f;
        statModifiers.Clear();
    }

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
        return type switch
        {
            StatType.Health => health,
            StatType.MoveSpeed => moveSpeed,
            _ => statModifiers[type],
        };
    }

    public void AddStatModifier(StatType type, float value)
    {
        statModifiers[type] += value;
    }
}
