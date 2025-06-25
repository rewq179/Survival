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
    public float maxHp;
    public float attack;
    public float moveSpd;
    public float cooldown;
    public float critChance;
    public float critMulti;

    public UnitData(int id, string name, float hp, float maxHp, float attack, float moveSpd, float cooldown, float critChance, float critMulti)
    {
        this.id = id;
        this.name = name;
        this.hp = hp;
        this.maxHp = maxHp;
        this.attack = attack;
        this.moveSpd = moveSpd;
        this.cooldown = cooldown;
        this.critChance = critChance;
        this.critMulti = critMulti;
    }
}

[CreateAssetMenu(fileName = "UnitReader", menuName = "Scriptable Object/UnitDataReader", order = int.MaxValue)]
public class UnitDataReader : BaseReader
{
    [SerializeField]
    public List<UnitData> unitDatas = new List<UnitData>();

    internal void SetData(List<GSTU_Cell> cells)
    {
        int id = 0;
        string name = string.Empty;
        float hp = 0;
        float maxHp = 0;
        float attack = 0;
        float moveSpd = 0;
        float cooldown = 0;
        float critChance = 0;
        float critMulti = 0;

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

                case "hp":
                    {
                        if (float.TryParse(cells[i].value, NumberStyles.Any, CultureInfo.InvariantCulture, out float parsedHp))
                        {
                            hp = parsedHp;
                        }
                        break;
                    }

                case "maxhp":
                    {
                        if (float.TryParse(cells[i].value, NumberStyles.Any, CultureInfo.InvariantCulture, out float parsedMaxHp))
                        {
                            maxHp = parsedMaxHp;
                        }
                        break;
                    }

                case "attack":
                    {
                        if (float.TryParse(cells[i].value, NumberStyles.Any, CultureInfo.InvariantCulture, out float parsedAttack))
                        {
                            attack = parsedAttack;
                        }
                        break;
                    }

                case "movespd":
                    {
                        if (float.TryParse(cells[i].value, NumberStyles.Any, CultureInfo.InvariantCulture, out float parsedMoveSpd))
                        {
                            moveSpd = parsedMoveSpd;
                        }
                        break;
                    }

                case "cooldown":
                    {
                        if (float.TryParse(cells[i].value, NumberStyles.Any, CultureInfo.InvariantCulture, out float parsedCooldown))
                        {
                            cooldown = parsedCooldown;
                        }
                        break;
                    }

                case "critchance":
                    {
                        if (float.TryParse(cells[i].value, NumberStyles.Any, CultureInfo.InvariantCulture, out float parsedCritChance))
                        {
                            critChance = parsedCritChance;
                        }
                        break;
                    }

                case "critmulti":
                    {
                        if (float.TryParse(cells[i].value, NumberStyles.Any, CultureInfo.InvariantCulture, out float parsedCritMulti))
                        {
                            critMulti = parsedCritMulti;
                        }
                        break;
                    }
            }
        }

        unitDatas.Add(new UnitData(id, name, hp, maxHp, attack, moveSpd, cooldown, critChance, critMulti));
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
