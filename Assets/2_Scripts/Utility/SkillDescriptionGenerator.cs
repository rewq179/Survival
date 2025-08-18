using UnityEngine;

public class SkillDescriptionGenerator
{
    public static string GetSubSkillDescription(string desc, float value)
    {
        if (desc.Contains("{P0}"))
            desc = desc.Replace("{P0}", (value * 100).ToString("F1") + "%");

        else if (desc.Contains("{F0}"))
            desc = desc.Replace("{F0}", value.ToString("F1"));

        return desc;
    }

    private const string LEVEL_UP_TEXT = "\n\n<color=yellow>레벨업!</color>";
    private const string NEW_ACQUISITION_TEXT = "\n\n<color=green>새로 획득!</color>";

    /// <summary>
    /// 선택 패널 슬롯 설명 생성
    /// </summary>
    public static string GetSlectionSlotDesc(SelectionData skillData)
    {
        if (skillData == null)
            return string.Empty;

        string baseDescription = skillData.description;
        float skillValue = GetSkillValue(skillData);

        // 기본 설명 생성
        string description = GetSubSkillDescription(baseDescription, skillValue);

        // 추가 텍스트 추가
        string additionalText = skillData.skillType switch
        {
            SkillType.Sub => LEVEL_UP_TEXT,
            SkillType.Active or SkillType.Passive => NEW_ACQUISITION_TEXT,
            _ => string.Empty
        };

        return description + additionalText;
    }

    private static float GetSkillValue(SelectionData skillData)
    {
        return skillData.skillType switch
        {
            SkillType.Passive => DataMgr.GetSkillData(skillData.skillKey).baseValue,
            SkillType.Sub => DataMgr.GetSubSkillData(skillData.skillKey).baseValue,
            _ => 0f
        };
    }
}