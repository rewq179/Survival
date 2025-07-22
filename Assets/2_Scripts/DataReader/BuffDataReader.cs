using UnityEngine;
using GoogleSheetsToUnity;
using System.Collections.Generic;
using System;
using System.Globalization;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DefaultExecutionOrder(100)]
[CreateAssetMenu(fileName = "BuffReader", menuName = "Scriptable Object/BuffDataReader", order = int.MaxValue)]
public class BuffDataReader : ScriptableObject
{
    public string sheetName = "Buff";
    public int startRow = 2;
    public int endRow = -1;

    [SerializeField]
    public List<BuffData> buffDatas = new();

    internal void SetData(List<GSTU_Cell> cells)
    {
        BuffKey buffKey = BuffKey.None;
        string name = string.Empty;
        string desc = string.Empty;
        BuffElement element = null;

        foreach (var cell in cells)
        {
            switch (cell.columnId.ToLowerInvariant())
            {
                case "buffkey":
                    if (Enum.TryParse(cell.value, out BuffKey parsedBuffKey))
                        buffKey = parsedBuffKey;
                    break;

                case "name":
                    name = cell.value;
                    break;

                case "description":
                    desc = cell.value;
                    break;

                case "element":
                    element = DecodeBuffElement(cell.value, buffKey);
                    break;
            }
        }

        BuffData buffData = new BuffData();
        buffData.Init(buffKey, name, desc, element);
        buffDatas.Add(buffData);
    }

    private BuffElement DecodeBuffElement(string buffString, BuffKey buffKey)
    {
        BuffElement element = new BuffElement();
        float stack = 0;
        float maxStack = 0;
        float duration = 0;
        float tick = 0;
        float dmg = 0;

        // Stack = 1, Duration = 3, Tick = 1, Dmg = 3.5
        string[] splits = buffString.Split(',');
        foreach (string str in splits)
        {
            if (string.IsNullOrEmpty(str))
                continue;

            string trimmedPart = str.Trim();
            string[] keyValue = trimmedPart.Split('=');

            string key = keyValue[0].Trim().ToLowerInvariant();
            if (keyValue.Length != 2)
                continue;

            float value = 0f;
            float.TryParse(keyValue[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out value);

            switch (key)
            {
                case "stack":
                    stack = value;
                    break;

                case "maxstack":
                    maxStack = value;
                    break;

                case "duration":
                    duration = value;
                    break;

                case "tick":
                    tick = value;
                    break;

                case "dmg":
                    dmg = value;
                    break;
            }
        }

        element.Init(buffKey, (int)stack, (int)maxStack, duration, tick, dmg);
        return element;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(BuffDataReader))]
public class BuffDataReaderEditor : Editor
{
    BuffDataReader dataReader;

    private void OnEnable()
    {
        dataReader = (BuffDataReader)target;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        GUILayout.Label("\n\n스프레드 시트 읽어오기");

        if (GUILayout.Button("데이터 읽기(API 호출)"))
        {
            UpdateStats(UpdateMethodOne);
            dataReader.buffDatas.Clear();
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
