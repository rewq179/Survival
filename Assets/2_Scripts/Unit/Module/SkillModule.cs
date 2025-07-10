using UnityEngine;
using System.Collections.Generic;
using System;
using Google.GData.Spreadsheets;

/// <summary>
/// 개별 스킬 관리 모듈
/// </summary>
public class SkillModule : MonoBehaviour
{
    private Unit owner;
    private Dictionary<SkillKey, SkillInstance> skillInstances = new();
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

    private static HashSet<SkillKey> applyAllSkill = new()
    {
        SkillKey.AllSkillRange,
        SkillKey.AllSkillRange_Inc,
        SkillKey.AllSkillCooldown,
        SkillKey.AllSkillCooldown_Dec,
        SkillKey.AllSkillDamage,
        SkillKey.AllSkillDamage_Inc,
        SkillKey.AllSkillDuration,
        SkillKey.AllSkillDuration_Inc,
    };

    public void Init(Unit unit, UnitData data)
    {
        owner = unit;

        cooldowns.Clear();
        cooldownTimes.Clear();
        activatedSkills.Clear();
        skillInstances.Clear();

        LearnSkill(data.skillKey);
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
        if (skillKey == SkillKey.None)
            return;

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
                CreateSkillInstance(skillKey);
                activeSkills.Add(skillKey);
                cooldownTimes[skillKey] = GetSkillInstance(skillKey).cooldownFinal;
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

                ApplySubSkillEffect(skillKey, false);
                GetSkillInstance(parentKey)?.Refresh(owner);
                break;
        }
    }

    private void LevelUpSkill(SkillKey skillKey)
    {
        if (!skillLevels.ContainsKey(skillKey))
            return;

        skillLevels[skillKey]++;
        OnSkillLevelChanged?.Invoke(skillKey, skillLevels[skillKey]);

        if (DataMgr.IsSubSkill(skillKey))
        {
            ApplySubSkillEffect(skillKey, true);
        }

        GetSkillInstance(skillKey)?.Refresh(owner);
    }

    private void CreateSkillInstance(SkillKey skillKey)
    {
        SkillInstance instance = new SkillInstance();
        instance.Init(skillKey);
        instance.Refresh(owner);
        skillInstances[skillKey] = instance;
    }

    public SkillInstance GetSkillInstance(SkillKey skillKey)
    {
        return skillInstances.ContainsKey(skillKey) ? skillInstances[skillKey] : null;
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

    public List<SkillKey> GetSubSkills(SkillKey parentSkillKey)
    {
        return subSkills.ContainsKey(parentSkillKey) ? subSkills[parentSkillKey] : null;
    }

    public bool HasSkill(SkillKey skillKey)
    {
        return activeSkills.Contains(skillKey) || passiveSkills.Contains(skillKey) || subSkills.ContainsKey(skillKey);
    }

    public bool IsSkillLearnable(SkillKey key)
    {
        SkillType type = DataMgr.GetSkillType(key);
        switch (type)
        {
            case SkillType.Active: // 액티브 : 보유하지 않았거나 액티브 계열이 N개 미만일 경우
                if (HasSkill(key) || activeSkills.Count >= GameValue.MAX_ACTIVE_SKILL_LEVEL)
                    return false;
                return true;

            case SkillType.Passive: // 패시브 : 보유하지 않았거나 패시브 계열이 N개 미만일 경우
                if (HasSkill(key) || passiveSkills.Count >= GameValue.MAX_PASSIVE_SKILL_LEVEL)
                    return false;
                return true;

            case SkillType.Sub: // 서브 : 부모 스킬을 보유하고 있고 서브 계열이 N개 미만일 경우
                // 또한, 동일 계열의 서브는 최대 N개 가능
                SubSkillData data = DataMgr.GetSubSkillData(key);
                if (!HasSkill(data.parentSkillKey))
                    return false;

                int cnt = GetSubSkillTotalLevel(data.parentSkillKey);
                if (cnt >= GameValue.MAX_SUB_SKILL_LEVEL)
                    return false;

                if (skillLevels.TryGetValue(key, out int level))
                    return level < GameValue.MAX_SUB_SKILL_LEVEL && cnt < GameValue.MAX_SUB_SKILL_LEVEL;
                return true;

            default:
                return false;
        }
    }

    private int GetSubSkillTotalLevel(SkillKey parentKey)
    {
        int cnt = 0;
        if (subSkills.TryGetValue(parentKey, out List<SkillKey> skills))
        {
            foreach (SkillKey key in skills)
            {
                if (skillLevels.ContainsKey(key))
                    cnt += skillLevels[key];
            }
        }

        return cnt;
    }

    #region 스킬 효과 적용

    private void ApplyPassiveSkillEffect(SkillKey skillKey)
    {
        SkillData data = DataMgr.GetSkillData(skillKey);
        if (data == null)
            return;

        float value = data.baseValue;
        switch (skillKey)
        {
            case SkillKey.Health:
                float heal = owner.MaxHp * value;
                owner.AddStatModifier(StatType.Health, value);
                owner.UpdateHp();
                owner.TakeHeal(heal);
                break;

            case SkillKey.MoveSpeed:
                owner.AddStatModifier(StatType.MoveSpeed, value);
                owner.UpdateMoveSpeed();
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

        CheckAllSkillEffect(skillKey);
    }

    private void ApplySubSkillEffect(SkillKey skillKey, bool isLevelUp)
    {
        SubSkillData data = DataMgr.GetSubSkillData(skillKey);
        if (data == null)
            return;

        float value = isLevelUp ? data.perLevelValue : data.baseValue;
        switch (skillKey)
        {
            case SkillKey.Health_Inc:
                float heal = owner.MaxHp * value;
                owner.AddStatModifier(StatType.Health, value);
                owner.UpdateHp();
                owner.TakeHeal(heal);
                break;
            case SkillKey.MoveSpeed_Inc:
                owner.AddStatModifier(StatType.MoveSpeed, value);
                owner.UpdateMoveSpeed();
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

        CheckAllSkillEffect(skillKey);
    }

    /// <summary>
    /// 모든 스킬 효과 적용
    /// </summary>
    private void CheckAllSkillEffect(SkillKey skillKey)
    {
        if (!applyAllSkill.Contains(skillKey))
            return;

        foreach (SkillKey key in activeSkills)
        {
            GetSkillInstance(key).Refresh(owner);
        }
    }

    #endregion
}
