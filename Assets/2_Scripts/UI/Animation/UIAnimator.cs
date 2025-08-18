using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

// 애니메이션 전용 컴포넌트
public abstract class UIAnimator : MonoBehaviour
{
    [Serializable]
    public class AnimationSettings
    {
        public AnimationCurve slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public float fadeInDuration = 0.3f;
        public float moveDuration = 0.5f;
        public float delayBetweenSlots = 0.1f;
        public float slotSpacing = 200f; // 슬롯 간격
    }

    [SerializeField] protected CanvasGroup canvasGroup;
    [SerializeField] protected AnimationSettings settings;

    public void AnimateSlotsIn(List<RectTransform> slots, Vector2[] startPositions, Vector2[] endPositions, Action onComplete)
    {
        StartCoroutine(ShowSelectionCoroutine(slots, startPositions, endPositions, onComplete));
    }

    public void AnimateSlotsOut(List<RectTransform> slots, Vector2[] endPositions, Action onComplete)
    {
        StartCoroutine(HideSelectionCoroutine(slots, endPositions, onComplete));
    }

    protected abstract IEnumerator ShowSelectionCoroutine(List<RectTransform> slots, Vector2[] startPositions, Vector2[] endPositions, Action onComplete);
    protected abstract IEnumerator HideSelectionCoroutine(List<RectTransform> slots, Vector2[] endPositions, Action onComplete);
}