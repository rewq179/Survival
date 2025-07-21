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
    public float[] parameters = new float[(int)ElementType.Max];

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

public enum SubSkillType
{
    Cooldown,               // 쿨다운 감소
    Shot,                   // 투사체 개수 증가
    Piercing,               // 관통
    Ricochet,               // 도탄
    Damage,                 // 데미지 증가
    Duration,               // 지속 시간 증가
    Radius,                 // 범위 증가
    DamageTick,             // 데미지 틱 증가
    MoveSpeed,              // 이동 속도 증가

    // 패시브 스킬 타입들
    HealthInc,              // 체력 증가
    MoveSpeedInc,           // 이동 속도 증가
    DefenseInc,             // 방어력 증가
    MagnetRangeInc,         // 자석 범위 증가
    ExpGainInc,             // 경험치 획득 증가
    GoldGainInc,            // 골드 획득 증가
    CriticalChanceInc,      // 치명타 확률 증가
    CriticalDamageInc,      // 치명타 피해 증가
    AllSkillRangeInc,       // 모든 스킬 범위 증가
    AllSkillCooldownDec,    // 모든 스킬 쿨다운 감소
    AllSkillDamageInc,      // 모든 스킬 데미지 증가
    AllSkillDurationInc,    // 모든 스킬 지속시간 증가

    Max,
}

[Serializable]
public class SubSkillData
{
    public SkillKey skillKey;
    public SkillKey parentSkillKey;
    public string name;
    public string description;
    public float baseValue;
    public float perLevelValue;
    public int maxLevel;
    public SubSkillType type;

    public void Init(SkillKey skillKey, SkillKey parentSkillKey, string description, float baseValue, float perLevelValue, int maxLevel, SubSkillType type)
    {
        this.skillKey = skillKey;
        this.parentSkillKey = parentSkillKey;
        this.description = description;
        this.baseValue = baseValue;
        this.perLevelValue = perLevelValue;
        this.maxLevel = maxLevel;
        this.type = type;
    }

    public void SetName(string name)
    {
        this.name = name;
    }
}


[Serializable]
public class UnitData
{
    public int id;
    public string name;
    public float hp;
    public float moveSpeed;
    public List<SkillKey> skills;
    public float exp;
    public int gold;

    public UnitData(int id, string name, float hp, float moveSpeed, List<SkillKey> skills, float exp, int gold)
    {
        this.id = id;
        this.name = name;
        this.hp = hp;
        this.moveSpeed = moveSpeed;
        this.skills = skills;
        this.exp = exp;
        this.gold = gold;
    }
}

[Serializable]
public class SpawnGroupData
{
    public int groupID;
    public int unitID;
    public int count;
    public int repeat;
    public float repeatInterval;
    public SpawnMgr.SpawnPattern pattern;
    public float startDelay;

    public SpawnGroupData(int groupID, int unitID, int count, int repeat, float repeatInterval, SpawnMgr.SpawnPattern pattern, float startDelay)
    {
        this.groupID = groupID;
        this.unitID = unitID;
        this.count = count;
        this.repeat = repeat;
        this.repeatInterval = repeatInterval;
        this.pattern = pattern;
        this.startDelay = startDelay;
    }
}

public enum WaveType
{
    Normal,
    Boss,
}

[Serializable]
public class WaveData
{
    public int waveID;
    public WaveType waveType;
    public float difficulty;
    public List<int> spawnGroupIDs;

    public WaveData(int waveID, WaveType waveType, float difficulty, List<int> spawnGroups)
    {
        this.waveID = waveID;
        this.waveType = waveType;
        this.difficulty = difficulty;
        this.spawnGroupIDs = spawnGroups;
    }
}

[Serializable]
public class ActiveWave
{
    public int waveID;
    public float waveDuration;
    public int groupCount;
    private int groupMaxCount;

    private float waveTime;
    public bool isTimeExceeded;
    public bool isCompleted;
    public List<Unit> waveEnemies = new();

    public void Reset()
    {
        waveID = -1;
        groupCount = 0;
        waveTime = 0f;
        isTimeExceeded = false;
        isCompleted = false;

        SpawnMgr spawnMgr = GameMgr.Instance.spawnMgr;
        for (int i = waveEnemies.Count - 1; i >= 0; i--)
        {
            spawnMgr.RemoveEnemy(waveEnemies[i]);
        }

        waveEnemies.Clear();
    }

    public void Init(int waveID, float waveDuration, int groupMaxCount)
    {
        this.waveID = waveID;
        this.waveDuration = waveDuration;
        this.groupMaxCount = groupMaxCount;
    }

    public void Update(float time)
    {
        if (isCompleted)
            return;

        waveTime += time;
        if (waveTime >= waveDuration)
            isTimeExceeded = true;
    }

    public void AddGroupCount() => groupCount++;

    /// <summary>
    /// 웨이브 완료 여부 확인
    /// </summary>
    public bool IsWaveClear()
    {
        if (waveEnemies.Count > 0)
            return false;

        return groupCount >= groupMaxCount;
    }
}
