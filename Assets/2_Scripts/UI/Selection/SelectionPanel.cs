using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System;
using System.Linq;

public class SelectionPanel : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private List<SelectionSlot> selectionSlots = new();
    [SerializeField] private GameObject panel;
    [SerializeField] private CanvasGroup canvasGroup;


    // 데이터
    private Unit playerUnit;
    private List<SelectionData> skills = new();
    private List<SelectionData> selectedSkills = new();
    private Stack<SelectionData> skillPool = new();
    private Vector2 slotStartPosition = new Vector2(0, -Screen.height * 0.3f);
    private Vector2[] startPos = new Vector2[SELECTION_COUNT];
    private Vector2[] targetPos = new Vector2[SELECTION_COUNT];

    // 상수
    private const int SELECTION_COUNT = 3;
    private const float ACTIVE_WEIGHT = 1.2f;
    private const float PASSIVE_WEIGHT = 0.7f;
    private const float SUB_WEIGHT = 0.5f;

    private const float FADE_IN_DURATION = 0.3f;
    private const float FADE_IN_INV_DURATION = 1 / FADE_IN_DURATION;
    private const float FADE_OUT_DURATION = 0.2f;
    private const float FADE_OUT_INV_DURATION = 1 / FADE_OUT_DURATION;
    private const float MOVE_DURATION = 0.2f;
    private const float MOVE_INV_DURATION = 1 / MOVE_DURATION;
    private const float SLOT_DIFF = 600f;

    public void Init(Unit unit)
    {
        playerUnit = unit;
        SetActive(false);
    }

    public void SetActive(bool isActive)
    {
        canvasGroup.alpha = isActive ? 1f : 0f;
        panel.SetActive(isActive);
    }

    public void ShowSkillSelection()
    {
        UpdateSkillPool();
        GetRandomSkills(SELECTION_COUNT);
        StartCoroutine(ShowSelectionCoroutine());
    }

    private void UpdateSkillPool()
    {
        foreach (SelectionData data in skills)
        {
            PushSelectionData(data);
        }
        skills.Clear();

        for (SkillKey skillKey = 0; skillKey < SkillKey.StingAttack; skillKey++)
        {
            if (playerUnit.HasSkill(skillKey))
            {
                List<SkillKey> subSkills = DataMgr.GetSubSkillKeysByMain(skillKey);
                foreach (SkillKey key in subSkills)
                {
                    skills.Add(CreateSelectionDataBySub(key));
                }
            }

            else if (!DataMgr.IsSubSkill(skillKey))
            {
                skills.Add(CreateSelectionDataByMain(skillKey));
            }
        }
    }

    private SelectionData CreateSelectionDataByMain(SkillKey key)
    {
        SkillData skillData = DataMgr.GetSkillData(key);
        SelectionData data = PopSelectionData();
        data.Init(key, skillData.skillType, skillData.name, skillData.description, GameManager.Instance.resourceMgr.GetSkillIcon(key));
        return data;
    }

    private SelectionData CreateSelectionDataBySub(SkillKey key)
    {
        SubSkillData skillData = DataMgr.GetSubSkillData(key);
        SelectionData data = PopSelectionData();
        data.Init(key, SkillType.Sub, skillData.name, skillData.description, GameManager.Instance.resourceMgr.GetSkillIcon(key));
        return data;
    }

    private void GetRandomSkills(int count)
    {
        List<SelectionData> availableSkills = new(skills);
        selectedSkills.Clear();

        for (int i = 0; i < count && availableSkills.Count > 0; i++)
        {
            SelectionData picked = RandomPickerByWeight.PickOne(
                availableSkills, data => GetWeight(data));

            selectedSkills.Add(picked);
            availableSkills.Remove(picked);
        }
    }

    private float GetWeight(SelectionData data)
    {
        return data.skillType switch
        {
            SkillType.Active => ACTIVE_WEIGHT,
            SkillType.Passive => PASSIVE_WEIGHT,
            SkillType.Sub => SUB_WEIGHT,
            _ => 0f
        };
    }

    private void SelectSkill(SelectionData data)
    {
        if (data.skillType == SkillType.Sub)
        {
            SubSkillData subSkillData = DataMgr.GetSubSkillData(data.skillKey);
            playerUnit.LevelUpSkill(subSkillData.parentSkillKey, data.skillKey);
        }

        else
            playerUnit.AddSkill(data.skillKey);

        StartCoroutine(HideSelectionCoroutine());
    }

    #region Animation

    private IEnumerator ShowSelectionCoroutine()
    {
        // 1. 초기 상태: 하단에 배치, 투명도 0
        yield return StartCoroutine(InitSelection());

        // 2. 페이드 인 + 상단 중앙으로 이동
        yield return StartCoroutine(FadeInAndMoveSlots());

        // 3. 슬롯별 목표 위치 계산 (좌, 중앙, 우)
        yield return StartCoroutine(SetGoalPos());

        // 4. 곡선(혹은 직선)으로 각 슬롯 이동
        yield return StartCoroutine(MoveSlots());
    }

    private IEnumerator InitSelection()
    {
        GameManager.Instance.OnGamePause();
        SetActive(true);
        canvasGroup.alpha = 0f;

        int count = selectedSkills.Count;
        for (int i = 0; i < count; i++)
        {
            SelectionSlot slot = selectionSlots[i];
            slot.Init(selectedSkills[i], SelectSkill);
            slot.UpdatePosition(slotStartPosition);
            startPos[i] = slot.Rect.anchoredPosition;
        }

        for (int i = count; i < selectionSlots.Count; i++)
        {
            selectionSlots[i].Reset();
        }

        yield return null;
    }

    private IEnumerator FadeInAndMoveSlots()
    {
        float time = 0f;
        while (time < FADE_IN_DURATION)
        {
            time += Time.unscaledDeltaTime;
            float alpha = Mathf.Clamp01(time * FADE_IN_INV_DURATION);
            canvasGroup.alpha = alpha;
            for (int i = 0; i < selectedSkills.Count; i++)
            {
                selectionSlots[i].UpdatePosition(Vector2.Lerp(startPos[i], Vector2.zero, alpha));
            }

            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    private IEnumerator SetGoalPos()
    {
        for (int i = 0; i < selectedSkills.Count; i++)
        {
            startPos[i] = selectionSlots[i].Rect.anchoredPosition;
        }

        if (selectedSkills.Count == 3)
        {
            targetPos[0] = Vector2.left * SLOT_DIFF;
            targetPos[1] = Vector2.zero;
            targetPos[2] = Vector2.right * SLOT_DIFF;
        }

        else if (selectedSkills.Count == 2)
        {
            targetPos[0] = Vector2.left * SLOT_DIFF * 0.5f;
            targetPos[1] = Vector2.right * SLOT_DIFF * 0.5f;
        }

        else if (selectedSkills.Count == 1)
        {
            targetPos[0] = Vector2.zero;
        }

        yield return null;
    }

    private IEnumerator MoveSlots()
    {
        float time = 0f;
        while (time < MOVE_DURATION)
        {
            time += Time.unscaledDeltaTime;
            float alpha = Mathf.SmoothStep(0, 1, time * MOVE_INV_DURATION);
            for (int i = 0; i < selectedSkills.Count; i++)
            {
                Vector2 pos = VectorExtension.Bezier2D(startPos[i], targetPos[i], alpha);
                selectionSlots[i].UpdatePosition(pos);
            }

            yield return null;
        }

        for (int i = 0; i < selectedSkills.Count; i++)
            selectionSlots[i].UpdatePosition(targetPos[i]);
    }

    private IEnumerator HideSelectionCoroutine()
    {
        float time = 0f;
        while (time < FADE_OUT_DURATION)
        {
            time += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(1f, 0f, time * FADE_OUT_INV_DURATION);
            canvasGroup.alpha = alpha;
            yield return null;
        }

        SetActive(false);
        GameManager.Instance.OnGameResume();
    }

    #endregion

    #region Pool

    private SelectionData PopSelectionData()
    {
        if (skillPool.TryPop(out SelectionData data))
            return data;

        return new SelectionData();
    }

    private void PushSelectionData(SelectionData data)
    {
        skillPool.Push(data);
    }

    #endregion
}