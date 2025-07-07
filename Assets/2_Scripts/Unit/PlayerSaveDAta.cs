using System.Collections.Generic;
using UnityEngine;
using System;

public class PlayerSaveData
{
    private Unit unit;
    public int level;
    public float exp;
    public int gold;
    private Dictionary<SkillKey, List<SubSkillKey>> skills = new();
    private Dictionary<SubSkillKey, int> subSkills = new();

    public Dictionary<SkillKey, List<SubSkillKey>> Skills => skills;
    public Dictionary<SubSkillKey, int> SubSkills => subSkills;

    // 델리게이트/이벤트
    public event Action<int> OnLevelChanged;
    public event Action<float> OnExpChanged;
    public event Action<int> OnGoldChanged;
    public event Action<Dictionary<SkillKey, List<SubSkillKey>>> OnSkillChanged;

    public bool HasSkill(SkillKey skillKey) => skills.ContainsKey(skillKey);

    public void Init(Unit unit)
    {
        this.unit = unit;
    }

    public float GetRequiredExp(int level)
    {
        // 기본 공식: 100 * (현재 레벨 + 1) * 1.5
        return 100 * (level + 1) * 1.5f;
    }

    public int AddExp(float amount)
    {
        int levelUpCount = 0;
        exp += amount;

        while (true)
        {
            float requiredExp = GetRequiredExp(level);
            if (exp < requiredExp)
                break;

            exp -= requiredExp;
            level++;
            levelUpCount++;
        }

        OnExpChanged?.Invoke(exp);
        if (levelUpCount > 0)
        {
            unit.UpdateHp();
            OnLevelChanged?.Invoke(level);

            // 레벨업 시 스킬 선택 UI 표시
            if (UIMgr.Instance != null && UIMgr.Instance.selectionPanel != null)
            {
                UIMgr.Instance.selectionPanel.ShowSkillSelection();
            }
        }

        return levelUpCount;
    }

    public void AddSkill(SkillKey skillKey)
    {
        if (!skills.ContainsKey(skillKey))
        {
            skills.Add(skillKey, new());
            OnSkillChanged?.Invoke(skills);
        }
    }

    public void RemoveSkill(SkillKey skillKey)
    {
        if (skills.Remove(skillKey))
        {
            OnSkillChanged?.Invoke(skills);
        }
    }

    public void LevelUpSkill(SkillKey skillKey, SubSkillKey subSkillKey)
    {
        if (skills.TryGetValue(skillKey, out List<SubSkillKey> mains))
            mains.Add(subSkillKey);
        else
            skills.Add(skillKey, new() { subSkillKey });

        if (subSkills.TryGetValue(subSkillKey, out int level))
            level++;
        else
            subSkills.Add(subSkillKey, 1);
    }

    public void AddGold(int amount)
    {
        gold += amount;
        OnGoldChanged?.Invoke(gold);
    }
}