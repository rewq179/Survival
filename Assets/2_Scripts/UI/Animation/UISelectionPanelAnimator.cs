using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UISelectionPanelAnimator : UIAnimator
{
    protected override IEnumerator ShowSelectionCoroutine(List<RectTransform> slots, Vector2[] startPositions, Vector2[] endPositions, Action onComplete)
    {
        // 1. 초기 상태: 하단에 배치, 투명도 0
        yield return StartCoroutine(InitSelection(slots, startPositions));

        // 2. 페이드 인 + 상단 중앙으로 이동
        yield return StartCoroutine(FadeInAndMoveSlots(slots, startPositions));

        // 3. 슬롯별 목표 위치 계산 (좌, 중앙, 우)
        Vector2[] targetPositions = CalculateTargetPositions(slots.Count);

        // 4. 베지어 곡선으로 각 슬롯 이동
        yield return StartCoroutine(MoveSlotsToTargets(slots, targetPositions));

        onComplete?.Invoke();
    }


    private IEnumerator InitSelection(List<RectTransform> slots, Vector2[] startPositions)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].anchoredPosition = startPositions[i];
        }
        canvasGroup.alpha = 0f;
        yield return null;
    }

    private IEnumerator FadeInAndMoveSlots(List<RectTransform> slots, Vector2[] startPositions)
    {
        float time = 0f;
        Vector2 centerPosition = Vector2.zero;
        float fadeInInvDuration = 1 / settings.fadeInDuration;

        while (time < settings.fadeInDuration)
        {
            time += Time.unscaledDeltaTime;
            float alpha = Mathf.Clamp01(time * fadeInInvDuration);
            float easedAlpha = settings.slideCurve.Evaluate(alpha);

            canvasGroup.alpha = alpha;
            for (int i = 0; i < slots.Count; i++)
            {
                Vector2 targetPos = Vector2.Lerp(startPositions[i], centerPosition, easedAlpha);
                slots[i].anchoredPosition = targetPos;
            }

            yield return null;
        }

        // 정확한 중앙 위치로 설정
        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].anchoredPosition = centerPosition;
        }
    }

    private Vector2[] CalculateTargetPositions(int slotCount)
    {
        Vector2[] targetPositions = new Vector2[slotCount];
        float spacing = settings.slotSpacing;

        if (slotCount == 3)
        {
            targetPositions[0] = Vector2.left * spacing;
            targetPositions[1] = Vector2.zero;
            targetPositions[2] = Vector2.right * spacing;
        }
        else if (slotCount == 2)
        {
            targetPositions[0] = Vector2.left * spacing * 0.5f;
            targetPositions[1] = Vector2.right * spacing * 0.5f;
        }
        else if (slotCount == 1)
        {
            targetPositions[0] = Vector2.zero;
        }

        return targetPositions;
    }

    private IEnumerator MoveSlotsToTargets(List<RectTransform> slots, Vector2[] targetPositions)
    {
        Vector2[] startPositions = new Vector2[slots.Count];
        for (int i = 0; i < slots.Count; i++)
        {
            startPositions[i] = slots[i].anchoredPosition;
        }

        float time = 0f;
        float invDuration = 1 / settings.moveDuration;
        while (time < settings.moveDuration)
        {
            time += Time.unscaledDeltaTime;
            float alpha = Mathf.SmoothStep(0, 1, time * invDuration);

            for (int i = 0; i < slots.Count; i++)
            {
                Vector2 pos = VectorExtension.Bezier2D(startPositions[i], targetPositions[i], alpha);
                slots[i].anchoredPosition = pos;
            }

            yield return null;
        }

        // 정확한 목표 위치로 설정
        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].anchoredPosition = targetPositions[i];
        }
    }

    protected override IEnumerator HideSelectionCoroutine(List<RectTransform> slots, Vector2[] endPositions, Action onComplete)
    {
        float time = 0f;
        float fadeOutDuration = settings.fadeInDuration * 0.8f; // 페이드아웃은 조금 빠르게
        float fadeOutInvDuration = 1 / fadeOutDuration;

        while (time < fadeOutDuration)
        {
            time += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(1f, 0f, time * fadeOutInvDuration);
            canvasGroup.alpha = alpha;
            yield return null;
        }

        // 정확한 끝 위치로 설정
        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].anchoredPosition = endPositions[i];
        }

        onComplete?.Invoke();
    }
}