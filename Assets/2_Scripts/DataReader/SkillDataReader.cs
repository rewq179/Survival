using UnityEngine;
using GoogleSheetsToUnity;
using System.Collections.Generic;
using System;
using System.Globalization;
using System.Linq;

#if UNITY_EDITOR
using UnityEngine.Events;
using UnityEditor;
#endif

[DefaultExecutionOrder(100)]
[CreateAssetMenu(fileName = "SkillReader", menuName = "Scriptable Object/SkillDataReader", order = int.MaxValue)]
public class SkillDataReader : ScriptableObject
{
    public string sheetName = "Skill";
    public int startRow = 2;
    public int endRow = -1;

    [SerializeField]
    public List<SkillData> skillDatas = new List<SkillData>();

    public void SetData(List<GSTU_Cell> cells)
    {
        SkillKey skillKey = SkillKey.Max;
        SkillType skillType = SkillType.Active;
        string name = string.Empty;
        string description = string.Empty;
        float cooldown = 0;
        float baseValue = 0;
        List<SkillElement> elements = new();

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
            }
        }

        skillDatas.Add(new SkillData(skillKey, skillType, name, description, cooldown,
            baseValue, elements));

        // 마지막에 추가된 SkillData의 elements 정렬
        SortSkillElements(skillDatas[skillDatas.Count - 1]);
    }

    private List<SkillElement> DecodeSkillElement(string skillString, SkillKey skillKey)
    {
        List<SkillElement> elements = new();

        // Projectile : Speed = 15, Dmg = 12, Ricochet=2, Piercing=1, Shot=3.2, InstantAOE : Angle = 360, Radius = 4, Dmg = 10
        string[] splits = skillString.Split('/');
        foreach (string str in splits)
        {
            if (string.IsNullOrEmpty(str))
                continue;

            SkillElement element = DecodeSingleSkillElement(str.Trim(), skillKey, elements.Count);
            elements.Add(element);
        }

        return elements;
    }

    private SkillElement DecodeSingleSkillElement(string str, SkillKey skillKey, int index)
    {
        ElementType ParsedElementType(string key)
        {
            return key switch
            {
                "speed" => ElementType.Speed,
                "dmg" => ElementType.Damage,
                "height" => ElementType.Height,
                "width" => ElementType.Width,
                "angle" => ElementType.Angle,
                "radius" => ElementType.Radius,
                "duration" => ElementType.Duration,
                "tick" => ElementType.Tick,
                "ricochet" => ElementType.Ricochet,
                "piercing" => ElementType.Piercing,
                "shot" => ElementType.Shot,
                "gravity" => ElementType.Gravity,
                _ => ElementType.Max,
            };
        }


        // Projectile : Speed = 15, Dmg = 12, Ricochet=2, Piercing=1, Shot=3.2 
        string[] splits = str.Split(':');
        if (splits.Length <= 1)
            return null;

        if (splits.Length > 2)
        {
            Debug.LogError($"명령문 내 '=' 대신 ':' 사용중");
            return null;
        }

        SkillElement element = new();

        SkillComponentType type = SkillComponentType.Projectile;
        if (Enum.TryParse(splits[0].Trim(), true, out SkillComponentType parsedComType))
            type = parsedComType;

        splits = splits[1].Split(',');
        foreach (string part in splits)
        {
            string trimmedPart = part.Trim();
            string[] keyValue = trimmedPart.Split('=');

            string key = keyValue[0].Trim().ToLowerInvariant();
            if (keyValue.Length != 2)
                continue;

            string value = keyValue[1].Trim();
            switch (key)
            {
                case "order":
                    element.SetOrder(int.Parse(value));
                    break;

                case "timing":
                    element.SetTiming(Enum.Parse<ExecutionTiming>(value));
                    break;

                case "firepoint":
                    element.SetFirePoint(Enum.Parse<FirePoint>(value));
                    break;

                default:
                    if (float.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out float parsedValue))
                        element.SetFloatParameter(ParsedElementType(key), parsedValue);
                    else
                        Debug.LogError($"명령문 내 파라미터 파싱 실패 => {value}");
                    break;
            }
        }

        element.Init(skillKey, index, type);
        return element;
    }

    private void SortSkillElements(SkillData skillData)
    {
        List<SkillElement> elements = skillData.skillElements;
        if (elements == null || elements.Count <= 1)
            return;

        // 1. Order별로 그룹화
        var orderGroups = elements
            .GroupBy(e => e.order)
            .OrderBy(g => g.Key)
            .ToList();

        // 2. 정렬된 리스트 생성
        List<SkillElement> sortedElements = new();
        foreach (var group in orderGroups)
        {
            var orderElements = group.ToList();

            // 같은 Order 내에서 Timing에 따라 정렬
            var immediateElements = orderElements
                .Where(e => e.timing == ExecutionTiming.Instant)
                .ToList();

            var sequentialElements = orderElements
                .Where(e => e.timing == ExecutionTiming.Sequential)
                .ToList();

            // Immediate 먼저, Sequential 나중에
            sortedElements.AddRange(immediateElements);
            sortedElements.AddRange(sequentialElements);
        }

        // 3. 정렬된 리스트로 교체
        skillData.skillElements = sortedElements;

        // 4. index 재설정 (선택사항)
        for (int i = 0; i < sortedElements.Count; i++)
        {
            sortedElements[i].index = i;
        }
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
            dataReader.skillDatas.Clear();
            UpdateStats(UpdateMethodOne);
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