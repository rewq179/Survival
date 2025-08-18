using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System;
using System.Linq;

public class SelectionPanel : MonoBehaviour, IUIComponent
{
    [Header("UI Components")]
    [SerializeField] private List<SelectionSlot> selectionSlots = new();
    [SerializeField] private GameObject panel;
    private List<RectTransform> selectionSlotRects = new();

    // 데이터 관련
    private Unit playerUnit;
    private int levelUpCount;

    // 분리된 시스템들
    [SerializeField] private UISelectionPanelAnimator animator;
    private SelectionDataManager dataManager = new();
    private PanelStateController stateController = new();

    // 애니메이션 관련
    private Vector2[] slotStartPositions;
    private Vector2[] slotEndPositions;

    private const int MAX_SELECTION_COUNT = 3;

    public void Reset()
    {
        // 모든 슬롯 초기화
        foreach (SelectionSlot slot in selectionSlots)
        {
            slot.Reset();
        }

        // 상태 초기화
        stateController.SetState(PanelStateController.PanelState.Hidden);
        UnsubscribeEvents();
    }

    public void Init(object data)
    {
        foreach (SelectionSlot slot in selectionSlots)
        {
            selectionSlotRects.Add(slot.Rect);
        }

        if (data is Unit unit)
        {
            playerUnit = unit;
            SubscribeEvents();

            // 초기 데이터 로드
            dataManager.UpdateAvailableSkills(playerUnit);

            // 애니메이션 위치 계산
            CalculateAnimationPositions();
        }
    }

    private void UnsubscribeEvents()
    {
        dataManager.OnSkillSelected -= OnSkillSelected;
        stateController.OnStateChanged -= OnStateChanged;

        if (GameEvents.Instance != null)
        {
            GameEvents.Instance.OnPlayerLevelUp -= AddLevelUpCount;
        }
    }

    private void SubscribeEvents()
    {
        dataManager.OnSkillSelected += OnSkillSelected;
        stateController.OnStateChanged += OnStateChanged;

        if (GameEvents.Instance != null)
        {
            GameEvents.Instance.OnPlayerLevelUp += AddLevelUpCount;
        }
    }

    private void CalculateAnimationPositions()
    {
        // 시작 위치
        Vector2 startPosition = Vector2.down * Screen.height * 0.3f;
        // 목표 위치
        Vector2 endPosition = Vector2.up * -200f;

        int slotCount = Mathf.Min(MAX_SELECTION_COUNT, selectionSlots.Count);
        slotStartPositions = new Vector2[slotCount];
        slotEndPositions = new Vector2[slotCount];
        for (int i = 0; i < slotCount; i++)
        {
            slotStartPositions[i] = startPosition;
            slotEndPositions[i] = endPosition;
        }
    }

    public void AddLevelUpCount(int count)
    {
        levelUpCount += count;
        Show();
    }

    public void Show()
    {
        if (!stateController.CanShow() || stateController.IsAnimating())
            return;

        if (levelUpCount <= 0)
            return;

        levelUpCount -= 1;
        stateController.SetState(PanelStateController.PanelState.Showing);
        panel.SetActive(true);

        // 스킬 데이터 업데이트
        dataManager.UpdateAvailableSkills(playerUnit);

        // 슬롯 애니메이션 시작
        StartShowAnimation();
    }

    private void StartShowAnimation()
    {
        // 활성화할 슬롯들 준비
        UpdateSelectionSlots(dataManager.AvailableSkills);

        // 애니메이션 시스템 사용
        animator.AnimateSlotsIn(selectionSlotRects, slotStartPositions, slotEndPositions, () =>
        {
            stateController.SetState(PanelStateController.PanelState.Visible);
        });
    }

    private void UpdateSelectionSlots(List<SelectionData> skills)
    {
        // 슬롯 초기화
        for (int i = 0; i < selectionSlots.Count; i++)
        {
            if (i < skills.Count)
            {
                SelectionSlot slot = selectionSlots[i];
                SelectionData skillData = skills[i];

                slot.Init(skillData, OnSlotClicked);
                slot.gameObject.SetActive(true);
            }

            else
            {
                selectionSlots[i].gameObject.SetActive(false);
            }
        }
    }

    public void Hide()
    {
        if (!stateController.CanHide())
            return;

        stateController.SetState(PanelStateController.PanelState.Hiding);
        StartHideAnimation();
    }

    private void StartHideAnimation()
    {
        animator.AnimateSlotsOut(selectionSlotRects, slotStartPositions, () =>
        {
            panel.SetActive(false);
            stateController.SetState(PanelStateController.PanelState.Hidden);
            Show();
        });
    }

    private void OnSlotClicked(SelectionData skillData)
    {
        dataManager.SelectSkill(skillData);
    }

    private void OnSkillSelected(SelectionData skillData)
    {
        Hide();
    }

    private void OnStateChanged(PanelStateController.PanelState newState)
    {
        switch (newState)
        {
            case PanelStateController.PanelState.Visible:
                break;

            case PanelStateController.PanelState.Hidden:
                break;
        }
    }
}