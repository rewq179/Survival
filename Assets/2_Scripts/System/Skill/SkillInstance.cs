using System.Collections.Generic;
using UnityEngine;

public class InstanceValue
{
    public int order;
    public ExecutionTiming timing;
    public SkillComponentType type;

    // 데미지
    private float damageFix;
    private float damageAdditive;
    private float damageMultiplier;
    public float damageFinal;

    // 범위
    private float radiusFix;
    private float radiusAdditive;
    private float radiusMultiplier;
    public float radiusFinal;

    // 지속시간
    private float durationFix;
    private float durationAdditive;
    private float durationMultiplier;
    public float durationFinal;

    // 피해 주기
    private float damageTickFix;
    private float damageTickAdditive;
    private float damageTickMultiplier;
    public float damageTickFinal;

    // 투사체
    private float ShotFix;
    private float ShotAdditive;
    public float ShotFinal;

    // 도탄
    private float ricochetFix;
    private float ricochetAdditive;
    public float ricochetFinal;

    // 관통
    private float piercingFix;
    private float piercingAdditive;
    public float piercingFinal;

    // 속도
    private float moveSpeedFix;
    private float moveSpeedAdditive;
    private float moveSpeedMultiplier;
    public float moveSpeedFinal;

    // 중력
    private float gravityFix;
    public float gravityFinal;

    public float angle;

    public void SetFix(SkillElement skillElement)
    {
        if (skillElement == null)
            return;

        damageFix = skillElement.Damage;
        durationFix = skillElement.Duration;
        damageTickFix = skillElement.Tick;
        radiusFix = skillElement.Radius;
        moveSpeedFix = skillElement.Speed;
        ricochetFix = skillElement.Ricochet;
        piercingFix = skillElement.Piercing;
        ShotFix = Mathf.Max(1, skillElement.Shot);
        gravityFix = skillElement.Gravity;
        angle = skillElement.Angle;
        type = skillElement.componentType;
        order = skillElement.order;
        timing = skillElement.timing;
    }

    public void Reset()
    {
        damageAdditive = 0;
        damageMultiplier = 0;
    }

    public void AddAdditiveValue(SubSkillType type, float value)
    {
        switch (type)
        {
            case SubSkillType.Damage:
                damageAdditive += value;
                break;

            case SubSkillType.Radius:
                radiusAdditive += value;
                break;

            case SubSkillType.Duration:
                durationAdditive += value;
                break;

            case SubSkillType.DamageTick:
                damageTickAdditive += value;
                break;

            case SubSkillType.Shot:
                ShotAdditive += value;
                break;

            case SubSkillType.Ricochet:
                ricochetAdditive += value;
                break;

            case SubSkillType.Piercing:
                piercingAdditive += value;
                break;

            case SubSkillType.MoveSpeed:
                moveSpeedAdditive += value;
                break;
        }
    }

    public void AddMultiplierValue(SubSkillType type, float value)
    {
        switch (type)
        {
            case SubSkillType.Damage:
                damageMultiplier += value;
                break;

            case SubSkillType.Radius:
                radiusMultiplier += value;
                break;

            case SubSkillType.Duration:
                durationMultiplier += value;
                break;

            case SubSkillType.DamageTick:
                damageTickMultiplier += value;
                break;

            case SubSkillType.MoveSpeed:
                moveSpeedMultiplier += value;
                break;
        }
    }

    public void CalculateFinalValue(Unit caster)
    {
        damageFinal = (damageFix + damageAdditive) * (1 + damageMultiplier + caster.GetFinalStat(StatType.AllSkillDamage));
        radiusFinal = (radiusFix + radiusAdditive) * (1 + radiusMultiplier + caster.GetFinalStat(StatType.AllSkillRange));
        durationFinal = (durationFix + durationAdditive) * (1 + durationMultiplier + caster.GetFinalStat(StatType.AllSkillDuration));
        damageTickFinal = (damageTickFix + damageTickAdditive) * (1 + damageTickMultiplier);
        ShotFinal = ShotFix + ShotAdditive;
        ricochetFinal = ricochetFix + ricochetAdditive;
        piercingFinal = piercingFix + piercingAdditive;
        moveSpeedFinal = (moveSpeedFix + moveSpeedAdditive) * (1 + moveSpeedMultiplier);
        gravityFinal = gravityFix;
    }
}

public class SkillInstance
{
    public SkillKey skillKey;

    private float cooldownFix;
    private float cooldownAdditive;
    private float cooldownMultiplier;
    public float cooldownFinal;
    private List<InstanceValue> values = new();

    public List<InstanceValue> Values => values;

    private void ResetValue()
    {
        cooldownAdditive = 0;
        cooldownMultiplier = 0;

        for (int i = 0; i < values.Count; i++)
        {
            values[i].Reset();
        }
    }

    public void Init(SkillKey skillKey)
    {
        this.skillKey = skillKey;
        SetFixValue();
    }

    private void SetFixValue()
    {
        SkillData data = DataMgr.GetSkillData(skillKey);
        cooldownFix = data.cooldown;

        for (int i = 0; i < data.skillElements.Count; i++)
        {
            InstanceValue value = new InstanceValue();
            value.SetFix(data.skillElements.Get(i));
            values.Add(value);
        }
    }

    public void Refresh(Unit caster)
    {
        ResetValue();

        // 서브 스킬 효과 적용
        ApplySubSkillEffect(caster);
        // 최종 값 계산
        CalculateFinalValue(caster);

#if UNITY_EDITOR
        // 디버그 출력
        PrintDebug();
#endif
    }

    private void ApplySubSkillEffect(Unit caster)
    {
        List<SkillKey> skills = caster.GetSubSkills(skillKey);
        if (skills == null)
            return;

        foreach (SkillKey key in skills)
        {
            SubSkillData data = DataMgr.GetSubSkillData(key);
            if (data == null)
                continue;

            int level = caster.GetSkillLevel(key);
            float value = data.baseValue + data.perLevelValue * (level - 1);

            switch (data.type) // TODO: 추후 SubSkill에 Index가 추가되면 0을 수정할 것
            {
                case SubSkillType.Cooldown:
                    cooldownAdditive -= value;
                    break;

                case SubSkillType.Damage:
                case SubSkillType.Radius:
                case SubSkillType.Duration:
                case SubSkillType.DamageTick:
                    values[0].AddMultiplierValue(data.type, value);
                    break;

                case SubSkillType.Shot:
                case SubSkillType.Ricochet:
                case SubSkillType.Piercing:
                    values[0].AddAdditiveValue(data.type, value);
                    break;
            }
        }
    }

    private void CalculateFinalValue(Unit caster)
    {
        cooldownFinal = (cooldownFix + cooldownAdditive) * (1 + cooldownMultiplier + caster.GetFinalStat(StatType.AllSkillCooldown));
        for (int i = 0; i < values.Count; i++)
        {
            values[i].CalculateFinalValue(caster);
        }
    }

    public bool IsMultipleProjectile()
    {
        foreach (InstanceValue value in values)
        {
            if (value.type == SkillComponentType.Projectile && value.ShotFinal > 1)
                return true;
        }

        return false;
    }

    private void PrintDebug()
    {
        string debug = $"[{skillKey}]\n쿨다운: {cooldownFinal:F2}초";
        for (int i = 0; i < values.Count; i++)
        {
            InstanceValue value = values[i];
            debug += $"\n[스킬 {i}] 데미지: {value.damageFinal:F1}, 범위: {value.radiusFinal:F1}, 지속시간: {value.durationFinal:F1}초, 피해주기: {value.damageTickFinal:F1}초, 투사체: {value.ShotFinal:F0}개, 도탄: {value.ricochetFinal:F0}회, 관통: {value.piercingFinal:F0}회";
        }
    }
}
