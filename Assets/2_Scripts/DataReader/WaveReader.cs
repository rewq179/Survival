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
public class WaveData
{
    public int waveID;
    public float difficulty;
    public List<int> spawnGroupIDs;

    public WaveData(int waveID, float difficulty, List<int> spawnGroups)
    {
        this.waveID = waveID;
        this.difficulty = difficulty;
        this.spawnGroupIDs = spawnGroups;
    }
}

[CreateAssetMenu(fileName = "WaveReader", menuName = "Scriptable Object/WaveDataReader", order = int.MaxValue)]
public class WaveDataReader : BaseReader
{
    public override string sheetName => "Wave";

    [SerializeField]
    public List<WaveData> waveDatas = new List<WaveData>();

    internal void SetData(List<GSTU_Cell> cells)
    {
        int id = 0;
        float difficulty = 0;
        List<int> spawnGroups = new List<int>();

        for (int i = 0; i < cells.Count; i++)
        {
            string columnId = cells[i].columnId.ToLowerInvariant();
            
            switch (columnId)
            {
                case "id":
                    {
                        if (int.TryParse(cells[i].value, NumberStyles.Any, CultureInfo.InvariantCulture, out int parsedId))
                        {
                            id = parsedId;
                        }
                        break;
                    }

                case "difficulty":
                    {
                        if (float.TryParse(cells[i].value, NumberStyles.Any, CultureInfo.InvariantCulture, out float parsedDifficulty))
                        {
                            difficulty = parsedDifficulty;
                        }
                        break;
                    }

                case "spawngroups":
                    {
                        string str = cells[i].value;
                        if (!string.IsNullOrEmpty(str))
                        {
                            string[] groupIds = str.Split(',');
                            
                            foreach (string groupId in groupIds)
                            {
                                if (int.TryParse(groupId.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out int parsedGroupId))
                                {
                                    spawnGroups.Add(parsedGroupId);
                                }
                            }
                        }
                        break;
                    }
            }
        }

        waveDatas.Add(new WaveData(id,  difficulty, spawnGroups));
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(WaveDataReader))]
public class WaveDataReaderEditor : Editor
{
    WaveDataReader dataReader;

    private void OnEnable()
    {
        dataReader = (WaveDataReader)target;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        GUILayout.Label("\n\n스프레드 시트 읽어오기");

        if (GUILayout.Button("데이터 읽기(API 호출)"))
        {
            UpdateStats(UpdateMethodOne);
            dataReader.waveDatas.Clear();
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
