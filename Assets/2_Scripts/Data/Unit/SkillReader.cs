using UnityEngine;
using GoogleSheetsToUnity;
using System.Collections.Generic;
using System;
using UnityEngine.Events;
using System.Globalization;

#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public class SkillData
{
    public int id;
    public string name;
    public float cooldown;
    public float reqLevel;
    public SkillIndicatorType indicatorType;
    public float length;
    public float angle;
    public float width;

    public SkillData(int id, string name, float cooldown, float reqLevel,
        SkillIndicatorType indicatorType, float length, float angle, float width)
    {
        this.id = id;
        this.name = name;
        this.cooldown = cooldown;
        this.reqLevel = reqLevel;
        this.indicatorType = indicatorType;
        this.length = length;
        this.angle = angle;
        this.width = width;
    }
}

[CreateAssetMenu(fileName = "SkillReader", menuName = "Scriptable Object/SkillDataReader", order = int.MaxValue)]
public class SkillDataReader : BaseReader
{
    [SerializeField]
    public List<SkillData> skillDatas = new List<SkillData>();

    internal void SetData(List<GSTU_Cell> cells)
    {
        int id = 0;
        string name = string.Empty;
        float cooldown = 0;
        float reqLevel = 0;
        SkillIndicatorType indicatorType = SkillIndicatorType.Circle;
        float length = 0;
        float angle = 0;
        float width = 0;

        for (int i = 0; i < cells.Count; i++)
        {
            string columnId = cells[i].columnId.ToLowerInvariant();

            switch (columnId)
            {
                case "id":
                    if (int.TryParse(cells[i].value, NumberStyles.Any, CultureInfo.InvariantCulture, out int parsedId))
                        id = parsedId;
                    break;

                case "name":
                    name = cells[i].value;
                    break;

                case "cooldown":
                    if (float.TryParse(cells[i].value, NumberStyles.Any, CultureInfo.InvariantCulture, out float parsedCooldown))
                        cooldown = parsedCooldown;
                    break;

                case "reqlevel":
                    if (float.TryParse(cells[i].value, NumberStyles.Any, CultureInfo.InvariantCulture, out float parsedReqLevel))
                        reqLevel = parsedReqLevel;
                    break;

                case "indicatorType":
                    indicatorType = (SkillIndicatorType)Enum.Parse(typeof(SkillIndicatorType), cells[i].value);
                    break;

                case "angle":
                    if (float.TryParse(cells[i].value, NumberStyles.Any, CultureInfo.InvariantCulture, out float parsedAngle))
                        angle = parsedAngle;
                    break;

                case "length":
                    if (float.TryParse(cells[i].value, NumberStyles.Any, CultureInfo.InvariantCulture, out float parsedLength))
                        length = parsedLength;
                    break;

                case "width":
                    if (float.TryParse(cells[i].value, NumberStyles.Any, CultureInfo.InvariantCulture, out float parsedWidth))
                        width = parsedWidth;
                    break;
            }
        }

        skillDatas.Add(new SkillData(id, name, cooldown, reqLevel, indicatorType, length, angle, width));
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
