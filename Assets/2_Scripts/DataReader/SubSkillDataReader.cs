using UnityEngine;
using GoogleSheetsToUnity;
using System.Collections.Generic;
using System;
using UnityEngine.Events;
using System.Globalization;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DefaultExecutionOrder(100)]
[CreateAssetMenu(fileName = "SubSkillReader", menuName = "Scriptable Object/SubSkillDataReader", order = int.MaxValue)]
public class SubSkillDataReader : ScriptableObject
{
    public string sheetName = "SubSkill";
    public int startRow = 2;
    public int endRow = -1;

    [SerializeField]
    public List<SubSkillData> subSkillDatas = new List<SubSkillData>();

    internal void SetData(List<GSTU_Cell> cells)
    {
        SkillKey skillKey = SkillKey.Max;
        SkillKey parentSkillKey = SkillKey.Max;
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
        data.Init(skillKey, parentSkillKey, description, baseValue, perLevelValue, maxLevel, type);
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
        SpreadsheetManager.Read(new GSTU_Search(GameValue.SHEET_ADDRESS, dataReader.sheetName), callback, mergedCells);
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