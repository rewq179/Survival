using UnityEngine;
using System.Collections.Generic;

public class DataMgr : MonoBehaviour
{
    public UnitDataReader unitDataReader;
    public SpawnGroupDataReader spawnGroupDataReader;
    public SkillDataReader skillDataReader;
    public SubSkillDataReader subSkillDataReader;
    public WaveDataReader waveDataReader;

    private static Dictionary<int, UnitData> unitDatas = new();
    private static Dictionary<SkillKey, SkillData> skillDatas = new();
    private static HashSet<SkillKey> activeSkillKeys = new();
    private static HashSet<SkillKey> passiveSkillKeys = new();
    private static Dictionary<SkillKey, SubSkillData> subSkillDatas = new();
    private static Dictionary<SkillKey, List<SkillKey>> subSkillDatasByMain = new();
    private static Dictionary<int, SpawnGroupData> spawnGroupDatas = new();
    private static List<WaveData> waveDatas = new();

    public void Init()
    {
        unitDatas.Clear();
        for (int i = 0; i < unitDataReader.unitDatas.Count; i++)
        {
            UnitData unitData = unitDataReader.unitDatas[i];
            unitDatas.Add(unitData.id, unitData);
        }

        skillDatas.Clear();
        activeSkillKeys.Clear();
        passiveSkillKeys.Clear();
        for (int i = 0; i < skillDataReader.skillDatas.Count; i++)
        {
            SkillData data = skillDataReader.skillDatas[i];
            skillDatas.Add(data.skillKey, data);

            if (data.skillType == SkillType.Active)
                activeSkillKeys.Add(data.skillKey);
            else if (data.skillType == SkillType.Passive)
                passiveSkillKeys.Add(data.skillKey);
        }

        subSkillDatas.Clear();
        subSkillDatasByMain.Clear();
        for (int i = 0; i < subSkillDataReader.subSkillDatas.Count; i++)
        {
            SubSkillData data = subSkillDataReader.subSkillDatas[i];
            data.SetName(GetSkillData(data.parentSkillKey).name);
            subSkillDatas.Add(data.skillKey, data);

            if (subSkillDatasByMain.TryGetValue(data.parentSkillKey, out List<SkillKey> keys))
                keys.Add(data.skillKey);
            else
                subSkillDatasByMain.Add(data.parentSkillKey, new List<SkillKey>() { data.skillKey });
        }

        spawnGroupDatas.Clear();
        for (int i = 0; i < spawnGroupDataReader.spawnGroupDatas.Count; i++)
        {
            SpawnGroupData spawnGroupData = spawnGroupDataReader.spawnGroupDatas[i];
            spawnGroupDatas.Add(spawnGroupData.groupID, spawnGroupData);
        }

        waveDatas.Clear();
        for (int i = 0; i < waveDataReader.waveDatas.Count; i++)
        {
            WaveData waveData = waveDataReader.waveDatas[i];
            waveDatas.Add(waveData);
        }
    }

    public static UnitData GetUnitData(int id)
    {
        if (unitDatas.TryGetValue(id, out UnitData unitData))
            return unitData;

        return null;
    }

    public static SkillType GetSkillType(SkillKey skillKey)
    {
        if (IsActiveSkill(skillKey))
            return SkillType.Active;

        return IsPassiveSkill(skillKey) ? SkillType.Passive : SkillType.Sub;
    }

    public static bool IsActiveSkill(SkillKey skillKey)
    {
        return activeSkillKeys.Contains(skillKey);
    }

    public static bool IsPassiveSkill(SkillKey skillKey)
    {
        return passiveSkillKeys.Contains(skillKey);
    }

    public static bool IsSubSkill(SkillKey skillKey)
    {
        return subSkillDatas.ContainsKey(skillKey);
    }

    public static SkillData GetSkillData(SkillKey skillKey)
    {
        if (skillDatas.TryGetValue(skillKey, out SkillData skillData))
            return skillData;

        return null;
    }

    public static List<SkillKey> GetSubSkillKeysByMain(SkillKey skillKey)
    {
        if (subSkillDatasByMain.TryGetValue(skillKey, out List<SkillKey> subSkillKeys))
            return subSkillKeys;

        return null;
    }

    public static SubSkillData GetSubSkillData(SkillKey skillKey)
    {
        if (subSkillDatas.TryGetValue(skillKey, out SubSkillData subSkillData))
            return subSkillData;

        return null;
    }

    public static SpawnGroupData GetSpawnGroupData(int groupID)
    {
        if (spawnGroupDatas.TryGetValue(groupID, out SpawnGroupData spawnGroupData))
            return spawnGroupData;

        return null;
    }

    public static List<WaveData> GetWaveDatas() => waveDatas;
    public static WaveData GetWaveData(int waveID)
    {
        for (int i = 0; i < waveDatas.Count; i++)
        {
            if (waveDatas[i].waveID == waveID)
                return waveDatas[i];
        }

        return null;
    }
}
