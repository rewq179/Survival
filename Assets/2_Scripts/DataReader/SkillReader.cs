using UnityEngine;
using GoogleSheetsToUnity;
using System.Collections.Generic;
using System;
using UnityEngine.Events;
using System.Globalization;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum SkillKey
{
    None = -1,
    Arrow,
    Dagger,
    Laser,
    Nova,
    FrontSpike,
    EnergyExplosion,
    LightningStrike,
    Meteor,
    Max,
}

[Serializable]
public class SkillData
{
    public SkillKey skillKey;
    public string name;
    public string description;
    public float cooldown;
    public float reqLevel;
    public List<IndicatorElement> elements;
    public SkillLauncherType launcherType;

    public SkillData(SkillKey skillKey, string name, string description, float cooldown, float reqLevel, 
        List<IndicatorElement> elements, SkillLauncherType launcherType)
    {
        this.skillKey = skillKey;
        this.name = name;
        this.description = description;
        this.cooldown = cooldown;
        this.reqLevel = reqLevel;
        this.elements = elements;
        this.launcherType = launcherType;
    }
}

[CreateAssetMenu(fileName = "SkillReader", menuName = "Scriptable Object/SkillDataReader", order = int.MaxValue)]
public class SkillDataReader : BaseReader
{
    public override string sheetName => "Skill";

    [SerializeField]
    public List<SkillData> skillDatas = new List<SkillData>();

    internal void SetData(List<GSTU_Cell> cells)
    {
        SkillKey skillKey = SkillKey.Max;
        string name = string.Empty;
        string description = string.Empty;
        float cooldown = 0;
        float reqLevel = 0;
        List<IndicatorElement> elements = new List<IndicatorElement>();
        SkillLauncherType launcherType = SkillLauncherType.Projectile;

        for (int i = 0; i < cells.Count; i++)
        {
            string columnId = cells[i].columnId.ToLowerInvariant();

            switch (columnId)
            {
                case "skillkey":
                    if (Enum.TryParse(cells[i].value, true, out SkillKey parsedSkillKey))
                        skillKey = parsedSkillKey;
                    break;

                case "name":
                    name = cells[i].value;
                    break;

                case "desc":
                    description = cells[i].value;
                    break;

                case "cooldown":
                    if (float.TryParse(cells[i].value, NumberStyles.Any, CultureInfo.InvariantCulture, out float parsedCooldown))
                        cooldown = parsedCooldown;
                    break;

                case "reqlevel":
                    if (float.TryParse(cells[i].value, NumberStyles.Any, CultureInfo.InvariantCulture, out float parsedReqLevel))
                        reqLevel = parsedReqLevel;
                    break;

                case "indicator":
                    if (!string.IsNullOrEmpty(cells[i].value))
                    {
                        elements = DecodeIndicatorElement(skillKey, cells[i].value);
                    }
                    break;

                case "launchertype":
                    if (Enum.TryParse(cells[i].value, true, out SkillLauncherType parsedLauncherType))
                        launcherType = parsedLauncherType;
                    break;
            }
        }

        skillDatas.Add(new SkillData(skillKey, name, description, cooldown, reqLevel, elements, launcherType));
    }

    private List<IndicatorElement> DecodeIndicatorElement(SkillKey skillKey, string indicatorString)
    {
        List<IndicatorElement> elements = new List<IndicatorElement>();
        
        /// Line : 1.5 / 6, Circle : 2.5 / 360
        string[] splits = indicatorString.Split(',');
        foreach (string str in splits)
        {
            IndicatorElement element = DecodeSingleElement(str.Trim(), skillKey, elements.Count);
            elements.Add(element);
        }

        return elements;
    }

    private IndicatorElement DecodeSingleElement(string str, SkillKey skillKey, int index)
    {
        string[] splits = str.Split(':');

        if (splits.Length != 2)
            return new IndicatorElement(skillKey, index, SkillIndicatorType.Line);

        Enum.TryParse(splits[0].Trim(), true, out SkillIndicatorType type);
        string[] values = splits[1].Trim().Split('/');

        if (values.Length != 2)
            return new IndicatorElement(skillKey, index, type);

        string firstValue = values[0].Trim();
        string secondValue = values[1].Trim();

        float.TryParse(firstValue, NumberStyles.Any, CultureInfo.InvariantCulture, out float first);
        float.TryParse(secondValue, NumberStyles.Any, CultureInfo.InvariantCulture, out float second);

        return type switch
        {
            SkillIndicatorType.Line => new IndicatorElement(skillKey, index, type, second, first, 0, 0),
            SkillIndicatorType.Sector => new IndicatorElement(skillKey, index, type, 0, 0, second, first),
            SkillIndicatorType.Circle => new IndicatorElement(skillKey, index, type, 0, 0, second, first),
            _ => new IndicatorElement(skillKey, index, type)
        };
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(SkillDataReader))]
public class SkillDataReaderEditor : Editor
{
    SkillDataReader dataReader;

    private void OnEnable()
    {
        dataReader = (SkillDataReader)target;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        GUILayout.Label("\n\n스프레드 시트 읽어오기");

        if (GUILayout.Button("데이터 읽기(API 호출)"))
        {
            UpdateStats(UpdateMethodOne);
            dataReader.skillDatas.Clear();
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
