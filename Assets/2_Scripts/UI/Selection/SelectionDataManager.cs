using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 선택 패널 데이터 관리
/// </summary>
public class SelectionDataManager
{
    private List<SelectionData> availableSkills = new();
    private List<SelectionData> selectedSkills = new();
    public event Action<SelectionData> OnSkillSelected;

    public List<SelectionData> AvailableSkills => availableSkills;

    public void UpdateAvailableSkills(Unit playerUnit)
    {
        availableSkills.Clear();

        // 플레이어가 학습 가능한 스킬들 수집
        for (SkillKey skillKey = 0; skillKey < SkillKey.StingAttack; skillKey++)
        {
            if (!playerUnit.IsSkillLearnable(skillKey))
                continue;

            SelectionData data = CreateSkillData(skillKey);
            availableSkills.Add(data);
        }
    }

    private SelectionData CreateSkillData(SkillKey skillKey)
    {
        if (DataMgr.IsSubSkill(skillKey))
        {
            SubSkillData subSkillData = DataMgr.GetSubSkillData(skillKey);

            return new SelectionData
            {
                skillKey = skillKey,
                skillType = SkillType.Sub,
                name = subSkillData.name,
                description = subSkillData.description,
                icon = GameMgr.Instance.resourceMgr.GetSkillIcon(subSkillData.parentSkillKey)
            };
        }

        else
        {
            SkillData skillData = DataMgr.GetSkillData(skillKey);

            return new SelectionData
            {
                skillKey = skillKey,
                skillType = skillData.skillType,
                name = skillData.name,
                description = skillData.desc,
                icon = GameMgr.Instance.resourceMgr.GetSkillIcon(skillKey)
            };
        }
    }

    public void SelectSkill(SelectionData skillData)
    {
        selectedSkills.Add(skillData);
        OnSkillSelected?.Invoke(skillData);
    }
}