using UnityEngine;
using GoogleSheetsToUnity;
using System.Collections.Generic;
using System;
using System.Globalization;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
[CreateAssetMenu(fileName = "WaveReader", menuName = "Scriptable Object/WaveDataReader", order = int.MaxValue)]
public class WaveDataReader : ScriptableObject
{
    public string sheetName = "Wave";
    public int startRow = 2;
    public int endRow = -1;

    [SerializeField]
    public List<WaveData> waveDatas = new();

    internal void SetData(List<GSTU_Cell> cells)
    {
        int id = 0;
        WaveType waveType = WaveType.Normal;
        float difficulty = 0;
        List<int> spawnGroups = new();

        foreach (var cell in cells)
        {
            switch (cell.columnId.ToLowerInvariant())
            {
                case "id":
                    if (int.TryParse(cell.value, NumberStyles.Any, CultureInfo.InvariantCulture, out int parsedId))
                        id = parsedId;
                    break;

                case "type":
                    if (Enum.TryParse(cell.value, out WaveType parsedWaveType))
                        waveType = parsedWaveType;
                    break;

                case "difficulty":
                    if (float.TryParse(cell.value, NumberStyles.Any, CultureInfo.InvariantCulture, out float parsedDifficulty))
                        difficulty = parsedDifficulty;
                    break;

                case "spawngroups":
                    string[] groupIds = cell.value.Split(',');
                    foreach (string groupId in groupIds)
                    {
                        if (int.TryParse(groupId.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out int parsedGroupId))
                            spawnGroups.Add(parsedGroupId);
                    }
                    break;
            }
        }

        waveDatas.Add(new WaveData(id, waveType, difficulty, spawnGroups));
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
