using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

#if UNITY_EDITOR
using UnityEditor;

[CustomPropertyDrawer(typeof(SkillElement))]
public class SkillElementDrawer : PropertyDrawer
{
    private static readonly ElementType[] elementTypes = (ElementType[])Enum.GetValues(typeof(ElementType));
    private static readonly int maxElementTypes = elementTypes.Length - 1; // Max 제외
    private int createdCount;
    private const int COLUMN_COUNT = 3;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        void DrawElement(string name)
        {
            EditorGUI.PropertyField(position, property.FindPropertyRelative(name));
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        EditorGUI.BeginProperty(position, label, property);

        // 기본 필드들
        position.height = EditorGUIUtility.singleLineHeight;
        DrawElement("skillKey");
        DrawElement("order");
        DrawElement("timing");
        DrawElement("componentType");
        DrawElement("indicatorType");

        SkillElement element = GetSkillElements(property, ExtractElementIndex(property.displayName));
        if (element != null)
        {
            // 버프 섹션
            SerializedProperty buffKeysProperty = property.FindPropertyRelative("buffKeys");
            if (buffKeysProperty != null && buffKeysProperty.arraySize > 0)
            {
                float buffKeysHeight = EditorGUI.GetPropertyHeight(buffKeysProperty);
                Rect buffKeysRect = new Rect(position.x, position.y, position.width, buffKeysHeight);
                EditorGUI.PropertyField(buffKeysRect, buffKeysProperty, true);
                position.y += buffKeysHeight + EditorGUIUtility.standardVerticalSpacing;
            }

            // 파라미터 섹션
            EditorGUI.LabelField(position, "Parameters", EditorStyles.boldLabel);
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            DrawAllParameters(position, element);
        }

        EditorGUI.EndProperty();
    }

    private SkillElement GetSkillElements(SerializedProperty property, int index)
    {
        SkillElement skillElement = property.boxedValue as SkillElement;
        if (skillElement == null)
            return null;

        var path = property.propertyPath.Split('.');
        if (path.Length < 2 || path[0] != "skillDatas")
            return null;

        var target = property.serializedObject.targetObject;
        var field = target.GetType().GetField("skillDatas");
        if (field == null)
            return null;

        List<SkillData> datas = field.GetValue(target) as List<SkillData>;
        if (datas == null)
            return null;

        foreach (SkillData data in datas)
        {
            if (data.skillKey == skillElement.skillKey)
                return data.skillElements[index];
        }

        return null;
    }

    private int ExtractElementIndex(string displayName)
    {
        Match match = Regex.Match(displayName, @"Element\s+(\d+)");
        if (match.Success && int.TryParse(match.Groups[1].Value, out int index))
            return index;
        return -1;
    }

    private void DrawAllParameters(Rect position, SkillElement element)
    {
        // 3열로 표시
        float columnWidth = (position.width - 5) / COLUMN_COUNT;
        float spacing = 5f;
        float currentX = position.x;
        float currentY = position.y;
        createdCount = 0;

        for (int i = 0; i < maxElementTypes; i++)
        {
            ElementType elementType = elementTypes[i];
            float value = element.GetParameter(elementType);
            if (value == 0)
                continue;

            // 가로로 3개씩 배치하도록 계산 수정
            int column = createdCount % COLUMN_COUNT;
            int row = createdCount / COLUMN_COUNT;
            createdCount++;

            float x = currentX + column * (columnWidth + spacing);
            float y = currentY + row * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);

            Rect paramRect = new Rect(x, y, columnWidth, EditorGUIUtility.singleLineHeight);

            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.FloatField(paramRect, elementType.ToString(), value);
            EditorGUI.EndDisabledGroup();
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        // 기본 필드들 높이
        float height = (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 5;

        // 버프 높이 추가
        SerializedProperty buffKeysProperty = property.FindPropertyRelative("buffKeys");
        if (buffKeysProperty != null && buffKeysProperty.arraySize > 0)
        {
            height += EditorGUI.GetPropertyHeight(buffKeysProperty) + EditorGUIUtility.standardVerticalSpacing;
        }

        // 파라미터 높이 추가
        height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // "Parameters" 라벨

        // 파라미터들 높이 계산
        SkillElement element = GetSkillElements(property, ExtractElementIndex(property.displayName));
        if (element == null)
            return height;

        int parameterCount = 0;
        for (int i = 0; i < maxElementTypes; i++)
        {
            ElementType elementType = elementTypes[i];
            if (element.GetParameter(elementType) != 0)
            {
                parameterCount++;
            }
        }

        int parameterRows = Mathf.CeilToInt(parameterCount / (float)COLUMN_COUNT);
        height += parameterRows * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);

        return height;
    }
}

#endif