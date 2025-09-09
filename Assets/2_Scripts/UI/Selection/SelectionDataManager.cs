using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// 선택 패널 데이터 관리
/// </summary>
public class SelectionDataManager
{
    #region Constants
    private const int MAX_AVAILABLE_SKILLS = 3;
    private const float DEFAULT_WEIGHT = 1.0f;

    // SubSkill 가중치 상수
    private const float COOLDOWN_WEIGHT = 1.1f;
    private const float DAMAGE_WEIGHT = 1.0f;
    private const float SHOT_WEIGHT = 0.9f;
    private const float RADIUS_WEIGHT = 0.8f;
    private const float DURATION_WEIGHT = 0.7f;
    private const float HEALTH_INC_WEIGHT = 1.0f;
    private const float MOVE_SPEED_INC_WEIGHT = 0.9f;
    private const float DEFENSE_INC_WEIGHT = 0.8f;
    private const float CRITICAL_CHANCE_INC_WEIGHT = 0.7f;
    private const float CRITICAL_DAMAGE_INC_WEIGHT = 0.6f;

    // MainSkill 가중치 상수
    private const float ACTIVE_SKILL_WEIGHT = 1.0f;
    private const float PASSIVE_SKILL_WEIGHT = 0.8f;
    #endregion

    private List<SelectionData> availableSkills = new();
    private List<SelectionData> selectedSkills = new();
    public event Action<SelectionData> OnSkillSelected;

    public List<SelectionData> AvailableSkills => availableSkills;
    public List<SelectionData> SelectedSkills => selectedSkills;

    public void UpdateAvailableSkills(Unit playerUnit)
    {
        if (playerUnit == null)
        {
            Debug.LogError("PlayerUnit이 null입니다.");
            return;
        }

        availableSkills.Clear();
        List<SkillKey> learnableSkills = GetLearnableSkills(playerUnit);
        AddAvailableSkills(learnableSkills);
    }

    /// <summary> 학습 가능한 스킬들 수집 </summary>
    private List<SkillKey> GetLearnableSkills(Unit playerUnit)
    {
        List<SkillKey> learnableSkills = new();

        // SkillKey enum의 마지막 값까지 반복
        for (SkillKey skillKey = 0; skillKey < SkillKey.StingAttack; skillKey++)
        {
            if (playerUnit.IsSkillLearnable(skillKey))
                learnableSkills.Add(skillKey);
        }

        return learnableSkills;
    }

    /// <summary> 학습 가능한 스킬들 중 가중치 기반 랜덤 선택 </summary>
    private void AddAvailableSkills(List<SkillKey> learnableSkills)
    {
        if (learnableSkills == null || learnableSkills.Count == 0)
            return;

        int maxSkills = Mathf.Min(MAX_AVAILABLE_SKILLS, learnableSkills.Count);
        List<SkillKey> availableSkillKeys = SelectRandomSkills(learnableSkills, maxSkills);

        foreach (SkillKey skillKey in availableSkillKeys)
        {
            SelectionData data = CreateSkillData(skillKey);
            if (data != null)
            {
                availableSkills.Add(data);
            }
        }
    }

    /// <summary> 가중치 기반으로 랜덤 스킬 선택 </summary>
    private List<SkillKey> SelectRandomSkills(List<SkillKey> learnableSkills, int count)
    {
        List<SkillKey> selectedSkills = new();
        for (int i = 0; i < count; i++)
        {
            if (learnableSkills.Count == 0)
                break;

            SkillKey selectedSkill = RandomPickerByWeight.PickOne(learnableSkills, GetSkillWeight);
            selectedSkills.Add(selectedSkill);
            learnableSkills.Remove(selectedSkill);
        }

        return selectedSkills;
    }

    private float GetSkillWeight(SkillKey skillKey)
    {
        if (DataMgr.IsSubSkill(skillKey))
        {
            SubSkillData subSkillData = DataMgr.GetSubSkillData(skillKey);
            return GetSubSkillWeight(subSkillData);
        }

        else
        {
            SkillData skillData = DataMgr.GetSkillData(skillKey);
            return GetMainSkillWeight(skillData);
        }
    }

    /// <summary> 서브 스킬 가중치 계산 </summary>
    private float GetSubSkillWeight(SubSkillData subSkillData)
    {
        if (subSkillData == null)
        {
            Debug.LogWarning("SubSkillData가 null입니다.");
            return DEFAULT_WEIGHT;
        }

        return subSkillData.type switch
        {
            SubSkillType.Cooldown => COOLDOWN_WEIGHT,
            SubSkillType.Damage => DAMAGE_WEIGHT,
            SubSkillType.Shot => SHOT_WEIGHT,
            SubSkillType.Radius => RADIUS_WEIGHT,
            SubSkillType.Duration => DURATION_WEIGHT,
            SubSkillType.HealthInc => HEALTH_INC_WEIGHT,
            SubSkillType.MoveSpeedInc => MOVE_SPEED_INC_WEIGHT,
            SubSkillType.DefenseInc => DEFENSE_INC_WEIGHT,
            SubSkillType.CriticalChanceInc => CRITICAL_CHANCE_INC_WEIGHT,
            SubSkillType.CriticalDamageInc => CRITICAL_DAMAGE_INC_WEIGHT,
            _ => DEFAULT_WEIGHT
        };
    }

    /// <summary> 메인 스킬 가중치 계산 </summary>
    private float GetMainSkillWeight(SkillData skillData)
    {
        if (skillData == null)
        {
            Debug.LogWarning("SkillData가 null입니다.");
            return DEFAULT_WEIGHT;
        }

        return skillData.skillType switch
        {
            SkillType.Active => ACTIVE_SKILL_WEIGHT,
            SkillType.Passive => PASSIVE_SKILL_WEIGHT,
            _ => DEFAULT_WEIGHT
        };
    }

    private SelectionData CreateSkillData(SkillKey skillKey)
    {
        if (DataMgr.IsSubSkill(skillKey))
        {
            return CreateSubSkillData(skillKey);
        }
        else
        {
            return CreateMainSkillData(skillKey);
        }
    }

    private SelectionData CreateSubSkillData(SkillKey skillKey)
    {
        SubSkillData subSkillData = DataMgr.GetSubSkillData(skillKey);
        if (subSkillData == null)
        {
            Debug.LogError($"SubSkillData를 찾을 수 없습니다: {skillKey}");
            return null;
        }

        return new SelectionData
        {
            skillKey = skillKey,
            skillType = SkillType.Sub,
            name = subSkillData.name,
            description = subSkillData.description,
            icon = GameMgr.Instance.resourceMgr.GetSkillIcon(subSkillData.parentSkillKey)
        };
    }

    private SelectionData CreateMainSkillData(SkillKey skillKey)
    {
        SkillData skillData = DataMgr.GetSkillData(skillKey);
        if (skillData == null)
        {
            Debug.LogError($"SkillData를 찾을 수 없습니다: {skillKey}");
            return null;
        }

        return new SelectionData
        {
            skillKey = skillKey,
            skillType = skillData.skillType,
            name = skillData.name,
            description = skillData.desc,
            icon = GameMgr.Instance.resourceMgr.GetSkillIcon(skillKey)
        };
    }

    public void SelectSkill(SelectionData skillData)
    {
        if (skillData == null)
        {
            Debug.LogWarning("선택된 스킬 데이터가 null입니다.");
            return;
        }

        selectedSkills.Add(skillData);
        OnSkillSelected?.Invoke(skillData);
    }

    /// <summary> 선택된 스킬 초기화 </summary>
    public void ClearSelectedSkills()
    {
        selectedSkills.Clear();
    }

    /// <summary> 사용 가능한 스킬 초기화 </summary>
    public void ClearAvailableSkills()
    {
        availableSkills.Clear();
    }
}