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

    // 액티브
    Arrow,
    Arrow_Cooldown,
    Arrow_ProjectileCount,
    Arrow_DamageInc,
    Arrow_Ricochet,

    Dagger,
    Dagger_Cooldown,
    Dagger_ProjectileCount,
    Dagger_DamageInc,
    Dagger_Piercing,

    FrontSpike,
    FrontSpike_Cooldown,
    FrontSpike_DamageInc,

    EnergyExplosion,
    EnergyExplosion_Cooldown,
    EnergyExplosion_DamageInc,
    EnergyExplosion_Radius,

    Meteor,
    Meteor_Cooldown,
    Meteor_DamageInc,
    Meteor_Radius,
    Meteor_Duration,
    Meteor_DamageTick,

    // 패시브
    Health,
    Health_Inc,
    MoveSpeed,
    MoveSpeed_Inc,
    Defense,
    Defense_Inc,
    MagnetRange,
    MagnetRange_Inc,
    ExpGain,
    ExpGain_Inc,
    GoldGain,
    GoldGain_Inc,
    CriticalChance,
    CriticalChance_Inc,
    CriticalDamage,
    CriticalDamage_Inc,
    AllSkillRange,
    AllSkillRange_Inc,
    AllSkillCooldown,
    AllSkillCooldown_Dec,
    AllSkillDamage,
    AllSkillDamage_Inc,
    AllSkillDuration,
    AllSkillDuration_Inc,

    // 몬스터
    StingAttack,

    Max,
}

public enum SkillType
{
    Active,
    Passive,
    Sub,
    Max,
}

[Serializable]
public class SkillData
{
    public SkillKey skillKey;
    public SkillType skillType;
    public string name;
    public string description;
    public float cooldown;
    public float baseValue;
    public List<SkillElement> skillElements;
    public SkillLauncherType launcherType;

    public SkillData(SkillKey skillKey, SkillType skillType, string name, string description, float cooldown, float baseValue,
        List<SkillElement> elements, SkillLauncherType launcherType)
    {
        this.skillKey = skillKey;
        this.skillType = skillType;
        this.name = name;
        this.description = description;
        this.cooldown = cooldown;
        this.baseValue = baseValue;
        skillElements = elements;
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
        SkillType skillType = SkillType.Active;
        string name = string.Empty;
        string description = string.Empty;
        float cooldown = 0;
        float baseValue = 0;
        List<SkillElement> elements = new();
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

                case "skilltype":
                    if (Enum.TryParse(cells[i].value, true, out SkillType parsedSkillType))
                        skillType = parsedSkillType;
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

                case "basevalue":
                    if (float.TryParse(cells[i].value, NumberStyles.Any, CultureInfo.InvariantCulture, out float parsedBaseValue))
                        baseValue = parsedBaseValue;
                    break;

                case "elements":
                    if (!string.IsNullOrEmpty(cells[i].value))
                    {
                        elements = DecodeSkillElement(cells[i].value, skillKey);
                    }
                    break;

                case "launchertype":
                    if (Enum.TryParse(cells[i].value, true, out SkillLauncherType parsedLauncherType))
                        launcherType = parsedLauncherType;
                    break;
            }
        }

        skillDatas.Add(new SkillData(skillKey, skillType, name, description, cooldown, baseValue, elements, launcherType));
    }

    private List<SkillElement> DecodeSkillElement(string skillString, SkillKey skillKey)
    {
        List<SkillElement> elements = new();

        // DMG : 12 / Int : 0.5
        string[] splits = skillString.Split(',');
        foreach (string str in splits)
        {
            SkillElement element = DecodeSingleSkillElement(str.Trim(), skillKey, elements.Count);
            elements.Add(element);
        }

        return elements;
    }

    private SkillElement DecodeSingleSkillElement(string str, SkillKey skillKey, int index)
    {
        float damage = 0f;
        float duration = 0f;
        float interval = 0f;
        float length = 0f;
        float width = 0f;
        float angle = 0f;
        float radius = 0f;
        float speed = 0f;

        // Dmg : 12 / Int : 0.5
        string[] splits = str.Split('/');
        foreach (string part in splits)
        {
            string trimmedPart = part.Trim();
            string[] keyValue = trimmedPart.Split(':');

            if (keyValue.Length != 2)
                continue;

            string key = keyValue[0].Trim().ToLowerInvariant();
            string value = keyValue[1].Trim();

            if (float.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out float parsedValue))
            {
                switch (key)
                {
                    case "dmg":
                        damage = parsedValue;
                        break;

                    case "duration":
                        duration = parsedValue;
                        break;

                    case "interval":
                        interval = parsedValue;
                        break;

                    case "length":
                        length = parsedValue;
                        break;

                    case "width":
                        width = parsedValue;
                        break;

                    case "angle":
                        angle = parsedValue;
                        break;

                    case "radius":
                        radius = parsedValue;
                        break;

                    case "movespeed":
                        speed = parsedValue;
                        break;
                }
            }
        }

        SkillElement element = new();
        element.Init(skillKey, index, speed, length, width, angle, radius, damage, duration, interval);
        return element;
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
