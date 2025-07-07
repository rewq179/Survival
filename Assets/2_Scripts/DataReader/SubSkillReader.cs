using UnityEngine;
using GoogleSheetsToUnity;
using System.Collections.Generic;
using System;
using UnityEngine.Events;
using System.Globalization;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum SubSkillType
{Cooldown,               // 쿨다운 감소
    ProjectileCount,        // 투사체 개수 증가
    Piercing,               // 관통
    Ricochet,               // 도탄
    DamageInc,              // 데미지 증가
    Duration,               // 지속 시간 증가
    Radius,                 // 범위 증가
    DamageTick,             // 데미지 틱 증가
    
    // 패시브 스킬 타입들
    HealthInc,              // 체력 증가
    MoveSpeedInc,           // 이동 속도 증가
    DefenseInc,             // 방어력 증가
    MagnetRangeInc,         // 자석 범위 증가
    ExpGainInc,             // 경험치 획득 증가
    GoldGainInc,            // 골드 획득 증가
    CriticalChanceInc,      // 치명타 확률 증가
    CriticalDamageInc,      // 치명타 피해 증가
    AllSkillRangeInc,       // 모든 스킬 범위 증가
    AllSkillCooldownDec,    // 모든 스킬 쿨다운 감소
    AllSkillDamageInc,      // 모든 스킬 데미지 증가
    AllSkillDurationInc,    // 모든 스킬 지속시간 증가
}

[Serializable]
public class SubSkillData
{
    public SkillKey skillKey;
    public SkillKey parentSkillKey;
    public string name;
    public string description;
    public float baseValue;
    public float perLevelValue;
    public int maxLevel;
    public SubSkillType type;

    public void Init(SkillKey skillKey, SkillKey parentSkillKey, string name, string description, float baseValue, float perLevelValue, int maxLevel, SubSkillType type)
    {
        this.skillKey = skillKey;
        this.parentSkillKey = parentSkillKey;
        this.name = name;
        this.description = description;
        this.baseValue = baseValue;
        this.perLevelValue = perLevelValue;
        this.maxLevel = maxLevel;
        this.type = type;
    }
}


[CreateAssetMenu(fileName = "SubSkillReader", menuName = "Scriptable Object/SubSkillDataReader", order = int.MaxValue)]
public class SubSkillDataReader : BaseReader
{
    public override string sheetName => "SubSkill";

    [SerializeField]
    public List<SubSkillData> subSkillDatas = new List<SubSkillData>();

    internal void SetData(List<GSTU_Cell> cells)
    {
        SkillKey skillKey = SkillKey.Max;
        SkillKey parentSkillKey = SkillKey.Max;
        string name = string.Empty;
        string description = string.Empty;
        float baseValue = 0f;
        float perLevelValue = 0f;
        int maxLevel = 1;
        SubSkillType type = SubSkillType.Cooldown;

        for (int i = 0; i < cells.Count; i++)
        {
            string columnId = cells[i].columnId.ToLowerInvariant();

            switch (columnId)
            {
                case "skillkey":
                    if (Enum.TryParse(cells[i].value, true, out SkillKey parsedSkillKey))
                        skillKey = parsedSkillKey;
                    break;

                case "parentskillkey":
                    if (Enum.TryParse(cells[i].value, true, out SkillKey parsedParentSkillKey))
                        parentSkillKey = parsedParentSkillKey;
                    break;

                case "name":
                    name = cells[i].value;
                    break;

                case "description":
                    description = cells[i].value;
                    break;

                case "basevalue":
                    if (float.TryParse(cells[i].value, NumberStyles.Any, CultureInfo.InvariantCulture, out float parsedBaseValue))
                        baseValue = parsedBaseValue;
                    break;

                case "perlevelvalue":
                    if (float.TryParse(cells[i].value, NumberStyles.Any, CultureInfo.InvariantCulture, out float parsedPerLevelValue))
                        perLevelValue = parsedPerLevelValue;
                    break;

                case "maxlevel":
                    if (int.TryParse(cells[i].value, out int parsedMaxLevel))
                        maxLevel = parsedMaxLevel;
                    break;

                case "type":
                    if (Enum.TryParse(cells[i].value, true, out SubSkillType parsedType))
                        type = parsedType;
                    break;
            }
        }

        SubSkillData data = new SubSkillData();
        data.Init(skillKey, parentSkillKey, name, description, baseValue, perLevelValue, maxLevel, type);
        subSkillDatas.Add(data);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(SubSkillDataReader))]
public class SubSkillDataReaderEditor : Editor
{
    SubSkillDataReader dataReader;

    private void OnEnable()
    {
        dataReader = (SubSkillDataReader)target;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        GUILayout.Label("\n\n서브 스킬 스프레드 시트 읽어오기");

        if (GUILayout.Button("서브 스킬 데이터 읽기(API 호출)"))
        {
            UpdateStats(UpdateMethodOne);
            dataReader.subSkillDatas.Clear();
        }
    }

    void UpdateStats(UnityAction<GstuSpreadSheet> callback, bool mergedCells = false)
    {
        SpreadsheetManager.Read(new GSTU_Search(dataReader.sheetAddress, dataReader.sheetName), callback, mergedCells);
    }

    void UpdateMethodOne(GstuSpreadSheet ss)
    {
        for (int i = dataReader.startRow; i <= dataReader.endRow; ++i)
        {
            dataReader.SetData(ss.rows[i]);
        }

        EditorUtility.SetDirty(target);
    }
}
#endif 