using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// 개별 스킬 관리 모듈
/// </summary>
public class SkillModule : MonoBehaviour
{
    private Unit owner;

    // 개별 유닛 스킬 관리
    private Dictionary<SkillKey, int> skillLevels = new();
    private List<SkillKey> activeSkills = new();
    private List<SkillKey> passiveSkills = new();
    // Key: 부모 스킬, Value: 서브 스킬 리스트
    private Dictionary<SkillKey, List<SkillKey>> subSkills = new();

    // 개별 쿨타임 관리
    private Dictionary<SkillKey, float> cooldowns = new();
    private Dictionary<SkillKey, float> cooldownTimes = new();
    private List<SkillKey> activatedSkills = new();

    // 이벤트
    public event Action<SkillKey, float> OnSkillCooldownChanged;
    public event Action<SkillKey> OnSkillCooldownEnded;
    public event Action<SkillKey> OnSkillAdded;
    public event Action<SkillKey> OnSkillRemoved;
    public event Action<SkillKey, int> OnSkillLevelChanged;

    public void Init(Unit unit)
    {
        owner = unit;

        RemoveAllSkillEffects();
        cooldowns.Clear();
        cooldownTimes.Clear();
        activatedSkills.Clear();
    }

    public void UpdateCooldowns()
    {
        for (int i = activatedSkills.Count - 1; i >= 0; i--)
        {
            SkillKey skillKey = activatedSkills[i];

            if (cooldowns[skillKey] > 0f)
                cooldowns[skillKey] -= Time.deltaTime;

            float cool = cooldowns[skillKey];
            if (cool <= 0f)
            {
                activatedSkills.RemoveAt(i);
                OnSkillCooldownEnded?.Invoke(skillKey);
            }

            else
            {
                OnSkillCooldownChanged?.Invoke(skillKey, cool);
            }
        }
    }

    public void LearnSkill(SkillKey skillKey)
    {
        if (skillLevels.ContainsKey(skillKey))
            LevelUpSkill(skillKey);
        else
            AddSkill(skillKey);
    }

    private void AddSkill(SkillKey skillKey)
    {
        if (skillLevels.ContainsKey(skillKey))
            skillLevels[skillKey] += 1;
        else
            skillLevels[skillKey] = 1;

        SkillType type = DataMgr.GetSkillType(skillKey);
        switch (type)
        {
            case SkillType.Active:
                activeSkills.Add(skillKey);
                cooldownTimes[skillKey] = DataMgr.GetSkillData(skillKey).cooldown;
                cooldowns[skillKey] = 0f;
                OnSkillAdded?.Invoke(skillKey);
                break;

            case SkillType.Passive:
                passiveSkills.Add(skillKey);
                ApplyPassiveSkillEffect(skillKey);
                OnSkillAdded?.Invoke(skillKey);
                break;

            case SkillType.Sub:
                SkillKey parentKey = DataMgr.GetSubSkillData(skillKey).parentSkillKey;
                if (subSkills.ContainsKey(parentKey))
                    subSkills[parentKey].Add(skillKey);
                else
                    subSkills[parentKey] = new List<SkillKey> { skillKey };

                ApplySubSkillEffect(skillKey);
                break;
        }
    }

    private void LevelUpSkill(SkillKey skillKey)
    {
        if (!skillLevels.ContainsKey(skillKey))
            return;

        if (DataMgr.IsSubSkill(skillKey))
        {
            RemoveSubSkillEffect(skillKey);
            skillLevels[skillKey]++;
            ApplySubSkillEffect(skillKey);
            OnSkillLevelChanged?.Invoke(skillKey, skillLevels[skillKey]);
        }
    }

    public bool CanUseSkill(SkillKey skillKey)
    {
        return cooldowns.ContainsKey(skillKey) && cooldowns[skillKey] <= 0f;
    }

    public void StartCooldown(SkillKey skillKey)
    {
        if (!cooldownTimes.ContainsKey(skillKey))
            return;

        cooldowns[skillKey] = cooldownTimes[skillKey];
        if (!activatedSkills.Contains(skillKey))
            activatedSkills.Add(skillKey);
    }

    public int GetSkillLevel(SkillKey skillKey)
    {
        return skillLevels.ContainsKey(skillKey) ? skillLevels[skillKey] : 0;
    }

    public bool HasSkill(SkillKey skillKey)
    {
        return activeSkills.Contains(skillKey) || passiveSkills.Contains(skillKey) || subSkills.ContainsKey(skillKey);
    }

    #region 스킬 효과 적용

    private void RemoveAllSkillEffects()
    {
        foreach (SkillKey skill in passiveSkills)
        {
            RemovePassiveSkillEffect(skill);
        }

        foreach (var skill in subSkills)
        {
            foreach (SkillKey subSkill in skill.Value)
            {
                RemoveSubSkillEffect(subSkill);
            }
        }
    }

    private void ApplyPassiveSkillEffect(SkillKey skillKey)
    {
        SkillData data = DataMgr.GetSkillData(skillKey);
        if (data == null)
            return;

        float value = data.baseValue;
        switch (skillKey)
        {
            case SkillKey.Health:
                owner.AddStatModifier(StatType.Health, value);
                break;


            case SkillKey.MoveSpeed:
                owner.AddStatModifier(StatType.MoveSpeed, value);
                break;

            case SkillKey.Defense:
                owner.AddStatModifier(StatType.Defense, value);
                break;

            case SkillKey.MagnetRange:
                owner.AddStatModifier(StatType.MagnetRange, value);
                break;

            case SkillKey.ExpGain:
                owner.AddStatModifier(StatType.ExpGain, value);
                break;

            case SkillKey.GoldGain:
                owner.AddStatModifier(StatType.GoldGain, value);
                break;

            case SkillKey.CriticalChance:
                owner.AddStatModifier(StatType.CriticalChance, value);
                break;

            case SkillKey.CriticalDamage:
                owner.AddStatModifier(StatType.CriticalDamage, value);
                break;

            case SkillKey.AllSkillRange:
                owner.AddStatModifier(StatType.AllSkillRange, value);
                break;

            case SkillKey.AllSkillCooldown:
                owner.AddStatModifier(StatType.AllSkillCooldown, value);
                break;

            case SkillKey.AllSkillDamage:
                owner.AddStatModifier(StatType.AllSkillDamage, value);
                break;

            case SkillKey.AllSkillDuration:
                owner.AddStatModifier(StatType.AllSkillDuration, value);
                break;
        }
    }

    private void RemovePassiveSkillEffect(SkillKey skillKey)
    {
        SkillData data = DataMgr.GetSkillData(skillKey);
        if (data == null)
            return;

        float value = data.baseValue;
        switch (skillKey)
        {
            case SkillKey.Health:
                owner.AddStatModifier(StatType.Health, -value);
                break;
            case SkillKey.MoveSpeed:
                owner.AddStatModifier(StatType.MoveSpeed, -value);
                break;
            case SkillKey.Defense:
                owner.AddStatModifier(StatType.Defense, -value);
                break;
            case SkillKey.MagnetRange:
                owner.AddStatModifier(StatType.MagnetRange, -value);
                break;
            case SkillKey.ExpGain:
                owner.AddStatModifier(StatType.ExpGain, -value);
                break;
            case SkillKey.GoldGain:
                owner.AddStatModifier(StatType.GoldGain, -value);
                break;
            case SkillKey.CriticalChance:
                owner.AddStatModifier(StatType.CriticalChance, -value);
                break;
            case SkillKey.CriticalDamage:
                owner.AddStatModifier(StatType.CriticalDamage, -value);
                break;
            case SkillKey.AllSkillRange:
                owner.AddStatModifier(StatType.AllSkillRange, -value);
                break;
            case SkillKey.AllSkillCooldown:
                owner.AddStatModifier(StatType.AllSkillCooldown, -value);
                break;
            case SkillKey.AllSkillDamage:
                owner.AddStatModifier(StatType.AllSkillDamage, -value);
                break;
            case SkillKey.AllSkillDuration:
                owner.AddStatModifier(StatType.AllSkillDuration, -value);
                break;
        }
    }

    private void ApplySubSkillEffect(SkillKey skillKey)
    {
        SubSkillData data = DataMgr.GetSubSkillData(skillKey);
        if (data == null)
            return;

        int level = GetSkillLevel(data.skillKey);
        float value = data.baseValue + (data.perLevelValue * (level - 1));

        switch (skillKey)
        {
            case SkillKey.Health_Inc:
                owner.AddStatModifier(StatType.Health, value);
                break;
            case SkillKey.MoveSpeed_Inc:
                owner.AddStatModifier(StatType.MoveSpeed, value);
                break;
            case SkillKey.Defense_Inc:
                owner.AddStatModifier(StatType.Defense, value);
                break;
            case SkillKey.MagnetRange_Inc:
                owner.AddStatModifier(StatType.MagnetRange, value);
                break;
            case SkillKey.ExpGain_Inc:
                owner.AddStatModifier(StatType.ExpGain, value);
                break;
            case SkillKey.GoldGain_Inc:
                owner.AddStatModifier(StatType.GoldGain, value);
                break;
            case SkillKey.CriticalChance_Inc:
                owner.AddStatModifier(StatType.CriticalChance, value);
                break;
            case SkillKey.CriticalDamage_Inc:
                owner.AddStatModifier(StatType.CriticalDamage, value);
                break;
            case SkillKey.AllSkillRange_Inc:
                owner.AddStatModifier(StatType.AllSkillRange, value);
                break;
            case SkillKey.AllSkillCooldown_Dec:
                owner.AddStatModifier(StatType.AllSkillCooldown, value);
                break;
            case SkillKey.AllSkillDamage_Inc:
                owner.AddStatModifier(StatType.AllSkillDamage, value);
                break;
            case SkillKey.AllSkillDuration_Inc:
                owner.AddStatModifier(StatType.AllSkillDuration, value);
                break;
        }
    }

    private void RemoveSubSkillEffect(SkillKey skillKey)
    {
        SubSkillData data = DataMgr.GetSubSkillData(skillKey);
        if (data == null)
            return;

        int level = GetSkillLevel(data.skillKey);
        float value = data.baseValue + (data.perLevelValue * (level - 1));

        switch (skillKey)
        {
            case SkillKey.Health_Inc:
                owner.AddStatModifier(StatType.Health, -value);
                break;
            case SkillKey.MoveSpeed_Inc:
                owner.AddStatModifier(StatType.MoveSpeed, -value);
                break;
            case SkillKey.Defense_Inc:
                owner.AddStatModifier(StatType.Defense, -value);
                break;
            case SkillKey.MagnetRange_Inc:
                owner.AddStatModifier(StatType.MagnetRange, -value);
                break;
            case SkillKey.ExpGain_Inc:
                owner.AddStatModifier(StatType.ExpGain, -value);
                break;
            case SkillKey.GoldGain_Inc:
                owner.AddStatModifier(StatType.GoldGain, -value);
                break;
            case SkillKey.CriticalChance_Inc:
                owner.AddStatModifier(StatType.CriticalChance, -value);
                break;
            case SkillKey.CriticalDamage_Inc:
                owner.AddStatModifier(StatType.CriticalDamage, -value);
                break;
            case SkillKey.AllSkillRange_Inc:
                owner.AddStatModifier(StatType.AllSkillRange, -value);
                break;
            case SkillKey.AllSkillCooldown_Dec:
                owner.AddStatModifier(StatType.AllSkillCooldown, -value);
                break;
            case SkillKey.AllSkillDamage_Inc:
                owner.AddStatModifier(StatType.AllSkillDamage, -value);
                break;
            case SkillKey.AllSkillDuration_Inc:
                owner.AddStatModifier(StatType.AllSkillDuration, -value);
                break;
        }
    }

    #endregion
}
