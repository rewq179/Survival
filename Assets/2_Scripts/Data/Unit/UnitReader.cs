using UnityEngine;
using GoogleSheetsToUnity;
using System.Collections.Generic;
using System;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public class UnitData
{
    public int id;
    public string name;
    public float hp;

    public UnitData(int id, string name, float hp)
    {
        this.id = id;
        this.name = name;
        this.hp = hp;
    }
}

[CreateAssetMenu(fileName = "UnitReader", menuName = "Scriptable Object/UnitDataReader", order = int.MaxValue)]
public class UnitDataReader : BaseReader
{
    [SerializeField]
    public List<UnitData> DataList = new List<UnitData>();

    internal void Update(List<GSTU_Cell> cells)
    {
        int id = 0;
        string name = string.Empty;
        float hp = 0;

        for (int i = 0; i < cells.Count; i++)
        {
            switch (cells[i].columnId)
            {
                case "id":
                    {
                        id = int.Parse(cells[i].value);
                        break;
                    }

                case "name":
                    {
                        name = cells[i].value;
                        break;
                    }

                case "hp":
                    {
                        hp = float.Parse(cells[i].value);
                        break;
                    }
            }
        }

        DataList.Add(new UnitData(id, name, hp));
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(UnitDataReader))]
public class UnitDataReaderEditor : Editor
{
    UnitDataReader data;

    private void OnEnable()
    {
        data = (UnitDataReader)target;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        GUILayout.Label("\n\n스프레드 시트 읽어오기");

        if (GUILayout.Button("데이터 읽기(API 호출)"))
        {
            UpdateStats(UpdateMethodOne);
            data.DataList.Clear();
        }
    }

    void UpdateStats(UnityAction<GstuSpreadSheet> callback, bool mergedCells = false)
    {
        SpreadsheetManager.Read(new GSTU_Search(data.sheetAddress, data.sheetName), callback, mergedCells);
    }

    void UpdateMethodOne(GstuSpreadSheet ss)
    {
        for (int i = data.startRow; i <= data.endRow; ++i)
        {
            data.Update(ss.rows[i]);
        }

        EditorUtility.SetDirty(target);
    }
}
#endif
