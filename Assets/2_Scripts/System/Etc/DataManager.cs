using UnityEngine;
using System.Collections.Generic;

public class DataManager : MonoBehaviour
{
    public UnitDataReader unitDataReader;
    public SpawnGroupDataReader spawnGroupDataReader;
    public SkillDataReader skillDataReader;
    public SubSkillDataReader subSkillDataReader;
    public WaveDataReader waveDataReader;

    private static Dictionary<int, UnitData> unitDatas = new();
    private static Dictionary<SkillKey, SkillData> skillDatas = new();
    private static Dictionary<SubSkillKey, SubSkillData> subSkillDatas = new();
    private static Dictionary<int, SpawnGroupData> spawnGroupDatas = new();
    private static Dictionary<int, WaveData> waveDatas = new();

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
            skillDatas.Add(skillData.skillKey, skillData);
        }

        subSkillDatas.Clear();
        for (int i = 0; i < subSkillDataReader.subSkillDatas.Count; i++)
        {
            SubSkillData subSkillData = subSkillDataReader.subSkillDatas[i];
            subSkillDatas.Add(subSkillData.subSkillKey, subSkillData);
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
            waveDatas.Add(waveData.waveID, waveData);
        }
    }

    public static UnitData GetUnitData(int id)
    {
        if (unitDatas.TryGetValue(id, out UnitData unitData))
            return unitData;

        return null;
    }

    public static SkillData GetSkillData(SkillKey skillKey)
    {
        if (skillDatas.TryGetValue(skillKey, out SkillData skillData))
            return skillData;

        return null;

    }

    public static SubSkillData GetSubSkillData(SubSkillKey subSkillKey)
    {
        if (subSkillDatas.TryGetValue(subSkillKey, out SubSkillData subSkillData))
            return subSkillData;

        return null;
    }

    public static SpawnGroupData GetSpawnGroupData(int groupID)
    {
        if (spawnGroupDatas.TryGetValue(groupID, out SpawnGroupData spawnGroupData))
            return spawnGroupData;

        return null;
    }

    public static WaveData GetWaveData(int waveID)
    {
        if (waveDatas.TryGetValue(waveID, out WaveData waveData))
            return waveData;

        return null;
    }
}
