using UnityEngine;
using System.Collections.Generic;

public class DataManager : MonoBehaviour
{
    public UnitDataReader unitDataReader;
    public SkillDataReader skillDataReader;

    private static Dictionary<int, UnitData> unitDatas = new();
    private static Dictionary<int, SkillData> skillDatas = new();

    public void Init()
    {
        unitDatas.Clear();
        for (int i = 0; i < unitDataReader.unitDatas.Count; i++)
        {
            UnitData unitData = unitDataReader.unitDatas[i];
            unitDatas.Add(unitData.id, unitData);
        }

        skillDatas.Clear();
        for (int i = 0; i < skillDataReader.skillDatas.Count; i++)
        {
            SkillData skillData = skillDataReader.skillDatas[i];
            skillDatas.Add(skillData.id, skillData);
        }
    }

    public static UnitData GetUnitData(int id)
    {
        if (unitDatas.TryGetValue(id, out UnitData unitData))
            return unitData;
            
        return null;
    }

    public static SkillData GetSkillData(int id)
    {
        if (skillDatas.TryGetValue(id, out SkillData skillData))
            return skillData;

        return null;
    }
}
