using UnityEngine;
using GoogleSheetsToUnity;
using System.Collections.Generic;
using System;
using System.Globalization;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public class SpawnGroupData
{
    public int groupID;
    public int unitID;
    public int count;
    public int repeat;
    public float repeatInterval;
    public SpawnManager.SpawnPattern pattern;
    public float startDelay;

    public SpawnGroupData(int groupID, int unitID, int count, int repeat, float repeatInterval, SpawnManager.SpawnPattern pattern, float startDelay)
    {
        this.groupID = groupID;
        this.unitID = unitID;
        this.count = count;
        this.repeat = repeat;
        this.repeatInterval = repeatInterval;
        this.pattern = pattern;
        this.startDelay = startDelay;
    }
}

[CreateAssetMenu(fileName = "SpawnGroupReader", menuName = "Scriptable Object/SpawnGroupDataReader", order = int.MaxValue)]
public class SpawnGroupDataReader : BaseReader
{
    public override string sheetName => "SpawnGroup";

    [SerializeField]
    public List<SpawnGroupData> spawnGroupDatas = new List<SpawnGroupData>();

    internal void SetData(List<GSTU_Cell> cells)
    {
        int groupID = 0;
        int unitID = 0;
        int count = 0;
        int repeat = 0;
        float repeatInterval = 0;
        SpawnManager.SpawnPattern pattern = SpawnManager.SpawnPattern.Circle;
        float startDelay = 0;

        for (int i = 0; i < cells.Count; i++)
        {
            string columnId = cells[i].columnId.ToLowerInvariant();

            switch (columnId)
            {
                case "id":
                    {
                        if (int.TryParse(cells[i].value, NumberStyles.Any, CultureInfo.InvariantCulture, out int parsedId))
                        {
                            groupID = parsedId;
                        }
                        break;
                    }

                case "unitid":
                    {
                        if (int.TryParse(cells[i].value, NumberStyles.Any, CultureInfo.InvariantCulture, out int parsedUnitID))
                        {
                            unitID = parsedUnitID;
                        }
                        break;
                    }

                case "count":
                    {
                        if (int.TryParse(cells[i].value, NumberStyles.Any, CultureInfo.InvariantCulture, out int parsedCount))
                        {
                            count = parsedCount;
                        }
                        break;
                    }

                case "repeat":
                    {
                        if (int.TryParse(cells[i].value, NumberStyles.Any, CultureInfo.InvariantCulture, out int parsedRepeat))
                        {
                            repeat = parsedRepeat;
                        }
                        break;
                    }

                case "repeatinterval":
                    {
                        if (float.TryParse(cells[i].value, NumberStyles.Any, CultureInfo.InvariantCulture, out float parsedRepeatInterval))
                        {
                            repeatInterval = parsedRepeatInterval;
                        }
                        break;
                    }

                case "pattern":
                    {
                        if (int.TryParse(cells[i].value, NumberStyles.Any, CultureInfo.InvariantCulture, out int parsedPattern))
                        {
                            pattern = (SpawnManager.SpawnPattern)parsedPattern;
                        }
                        break;
                    }

                case "startdelay":
                    {
                        if (float.TryParse(cells[i].value, NumberStyles.Any, CultureInfo.InvariantCulture, out float parsedStartDelay))
                        {
                            startDelay = parsedStartDelay;
                        }
                        break;
                    }


            }
        }

        spawnGroupDatas.Add(new SpawnGroupData(groupID, unitID, count, repeat, repeatInterval, pattern, startDelay));
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(SpawnGroupDataReader))]
public class SpawnGroupDataReaderEditor : Editor
{
    SpawnGroupDataReader dataReader;

    private void OnEnable()
    {
        dataReader = (SpawnGroupDataReader)target;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        GUILayout.Label("\n\n스프레드 시트 읽어오기");

        if (GUILayout.Button("데이터 읽기(API 호출)"))
        {
            UpdateStats(UpdateMethodOne);
            dataReader.spawnGroupDatas.Clear();
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
