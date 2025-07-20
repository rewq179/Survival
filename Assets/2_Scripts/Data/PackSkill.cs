using System.Collections.Generic;
using System;
using UnityEngine;

public enum SkillKey
{
    None = -1,

    // 액티브
    Arrow,
    Arrow_Cooldown,
    Arrow_Shot,
    Arrow_DamageInc,
    Arrow_Ricochet,

    Dagger,
    Dagger_Cooldown,
    Dagger_Shot,
    Dagger_DamageInc,
    Dagger_Piercing,

    FrontSpike,
    FrontSpike_Cooldown,
    FrontSpike_DamageInc,

    EnergyExplosion,
    EnergyExplosion_Cooldown,
    EnergyExplosion_DamageInc,
    EnergyExplosion_Radius,

    Meteor,
    Meteor_Cooldown,
    Meteor_DamageInc,
    Meteor_Radius,
    Meteor_Duration,
    Meteor_DamageTick,

    Blackhole,
    Blackhole_Cooldown,
    Blackhole_DamageInc,
    Blackhole_Radius,
    Blackhole_Duration,
    Blackhole_DamageTick,

    IseAttack,
    IseAttack_Cooldown,
    IseAttack_DamageInc,
    IseAttack_Radius,

    // 패시브
    Health,
    Health_Inc,
    MoveSpeed,
    MoveSpeed_Inc,
    Defense,
    Defense_Inc,
    MagnetRange,
    MagnetRange_Inc,
    ExpGain,
    ExpGain_Inc,
    GoldGain,
    GoldGain_Inc,
    CriticalChance,
    CriticalChance_Inc,
    CriticalDamage,
    CriticalDamage_Inc,
    AllSkillRange,
    AllSkillRange_Inc,
    AllSkillCooldown,
    AllSkillCooldown_Dec,
    AllSkillDamage,
    AllSkillDamage_Inc,
    AllSkillDuration,
    AllSkillDuration_Inc,

    // 몬스터
    StingAttack,
    FireProjectile,
    MeleeAttack,
    BiteAttack,
    SpitPoisonAttack,
    PunchAttack,
    HitGroundAttack,
    DragonBiteAttack,
    BreathAttack,

    Max,
}

public enum SkillType
{
    Active,
    Passive,
    Sub,
    Max,
}

public enum ElementType
{
    Order,
    Timing,
    FirePoint,
    Damage,
    Speed,
    Duration,
    Tick,
    Height,
    Width,
    Angle,
    Radius,
    Ricochet,
    Piercing,
    Shot,
    Gravity,
    Max,
}

public enum ExecutionTiming
{
    Instant,        // 즉시 실행
    Sequential,     // 순차 실행 (이전 엘리먼트 완료 후)
    Max,
}

public enum FirePoint
{
    Self,
    Target,
    Max,
}

[System.Serializable]
public class SkillElement
{
    public SkillKey skillKey;
    public int index;
    public SkillComponentType componentType;
    public SkillIndicatorType indicatorType;
    public int order;
    public ExecutionTiming timing;
    public FirePoint firePoint;
    public bool isTarget;
    public float maxDistance;
    private float[] parameters = new float[(int)ElementType.Max];

    public bool IsMainIndicator => index == 0;
    public float Speed => GetParameter(ElementType.Speed);
    public float Damage => GetParameter(ElementType.Damage);
    public float Radius => GetParameter(ElementType.Radius);
    public float Angle => GetParameter(ElementType.Angle);
    public float Duration => GetParameter(ElementType.Duration);
    public float Tick => GetParameter(ElementType.Tick);
    public float Height => GetParameter(ElementType.Height);
    public float Width => GetParameter(ElementType.Width);
    public float Ricochet => GetParameter(ElementType.Ricochet);
    public float Piercing => GetParameter(ElementType.Piercing);
    public float Shot => GetParameter(ElementType.Shot);
    public float Gravity => GetParameter(ElementType.Gravity);

    public void Init(SkillKey skillKey, int index, SkillComponentType componentType)
    {
        this.skillKey = skillKey;
        this.index = index;
        this.componentType = componentType;

        SetIndicatorType();
        SetMaxDistance();
    }

    private void SetIndicatorType()
    {
        if (componentType == SkillComponentType.Projectile)
            indicatorType = SkillIndicatorType.Line;
        else if (componentType == SkillComponentType.Beam)
            indicatorType = SkillIndicatorType.Rectangle;
        else if (Angle == 0)
            indicatorType = SkillIndicatorType.InstantAttack;
        else if (Angle < 360)
            indicatorType = SkillIndicatorType.Sector;
        else
            indicatorType = SkillIndicatorType.Circle;
    }

    private void SetMaxDistance()
    {
        maxDistance = Radius > 0 ? Radius * 2f : Width * 1.6f;
    }

    public float GetParameter(ElementType key)
    {
        int index = (int)key;
        if (index >= 0 && index < parameters.Length)
            return parameters[index];

        return 0f;
    }

    public void SetFirePoint(FirePoint value) => firePoint = value;
    public void SetOrder(int value) => order = value;
    public void SetTiming(ExecutionTiming value) => timing = value;
    
    public void SetFloatParameter(ElementType key, float value)
    {
        int index = (int)key;
        if (index >= 0 && index < parameters.Length)
            parameters[index] = value;
    }
}

[Serializable]
public class SkillData
{
    public SkillKey skillKey;
    public SkillType skillType;
    public string name;
    public string desc;
    public float cooldown;
    public float baseValue;
    public List<SkillElement> skillElements;

    public SkillData(SkillKey skillKey, SkillType skillType, string name, string description, float cooldown, float baseValue,
        List<SkillElement> elements)
    {
        this.skillKey = skillKey;
        this.skillType = skillType;
        this.name = name;
        this.desc = description;
        this.cooldown = cooldown;
        this.baseValue = baseValue;
        skillElements = elements;
    }
}