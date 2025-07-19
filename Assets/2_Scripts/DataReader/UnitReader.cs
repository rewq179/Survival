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
public class UnitData
{
    public int id;
    public string name;
    public float hp;
    public float moveSpeed;
    public List<SkillKey> skills;
    public float exp;
    public int gold;

    public UnitData(int id, string name, float hp, float moveSpeed, List<SkillKey> skills, float exp, int gold)
    {
        this.id = id;
        this.name = name;
        this.hp = hp;
        this.moveSpeed = moveSpeed;
        this.skills = skills;
        this.exp = exp;
        this.gold = gold;
    }
}

[CreateAssetMenu(fileName = "UnitReader", menuName = "Scriptable Object/UnitDataReader", order = int.MaxValue)]
public class UnitDataReader : ScriptableObject
{
    public string sheetName = "Unit";
    public int startRow = 2;
    public int endRow = -1;

    [SerializeField]
    public List<UnitData> unitDatas = new List<UnitData>();

    internal void SetData(List<GSTU_Cell> cells)
    {
        int id = 0;
        string name = string.Empty;
        float health = 0;
        float moveSpeed = 0;
        List<SkillKey> skills = new List<SkillKey>();
        float exp = 0;
        int gold = 0;

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

                case "name":
                    {
                        name = cells[i].value;
                        break;
                    }

                case "health":
                    {
                        if (float.TryParse(cells[i].value, NumberStyles.Any, CultureInfo.InvariantCulture, out float parsedHp))
                        {
                            health = parsedHp;
                        }
                        break;
                    }

                case "movespeed":
                    {
                        if (float.TryParse(cells[i].value, NumberStyles.Any, CultureInfo.InvariantCulture, out float parsedMoveSpeed))
                        {
                            moveSpeed = parsedMoveSpeed;
                        }
                        break;
                    }

                case "skills":
                    {
                        string[] keys = cells[i].value.Split('/');
                        foreach (string key in keys)
                        {
                            if (Enum.TryParse(key, out SkillKey parsedKey))
                                skills.Add(parsedKey);
                        }
                        break;
                    }

                case "exp":
                    {
                        if (float.TryParse(cells[i].value, NumberStyles.Any, CultureInfo.InvariantCulture, out float parsedExpReward))
                        {
                            exp = parsedExpReward;
                        }
                        break;
                    }

                case "gold":
                    {
                        if (int.TryParse(cells[i].value, NumberStyles.Any, CultureInfo.InvariantCulture, out int parsedGoldReward))
                        {
                            gold = parsedGoldReward;
                        }
                        break;
                    }
            }
        }

        unitDatas.Add(new UnitData(id, name, health, moveSpeed, skills, exp, gold));
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(UnitDataReader))]
public class UnitDataReaderEditor : Editor
{
    UnitDataReader dataReader;

    private void OnEnable()
    {
        dataReader = (UnitDataReader)target;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        GUILayout.Label("\n\n스프레드 시트 읽어오기");

        if (GUILayout.Button("데이터 읽기(API 호출)"))
        {
            UpdateStats(UpdateMethodOne);
            dataReader.unitDatas.Clear();
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
