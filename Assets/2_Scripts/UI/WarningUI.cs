using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class WarningUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;

    [Header("Warning Images")]
    [SerializeField] private List<RectTransform> topWarningRects;
    [SerializeField] private List<RectTransform> bottomWarningRects;

    [Header("Main Warning")]
    [SerializeField] private RectTransform mainWarningRect;
    [SerializeField] private RectMask2D mainMask;

    // 애니메이션 상태
    private bool isWarningActive = false;
    private List<Vector2> topWarningStartPositions = new List<Vector2>();
    private List<Vector2> bottomWarningStartPositions = new List<Vector2>();

    private float totalWidth;
    private float cellWidth;
    private float maskWidth;
    private Vector4 maskPadding = new Vector4(0f, 0f, 1f, 0f);
    private const float WARNING_SPACING = 40f;
    private const float WARNING_MOVE_SPEED = 800f;
    private const float FADE_IN_START_DELAY = 0.2f;
    private const float FADE_IN_DURATION = 0.8f;
    private const float FADE_IN_INV_DURATION = 1f / FADE_IN_DURATION;
    private const float HIDE_DELAY = 0.8f;

    private void Start()
    {
        SaveWarningStartPositions();
    }

    private void SaveWarningStartPositions()
    {
        topWarningStartPositions.Clear();
        bottomWarningStartPositions.Clear();

        foreach (RectTransform rect in topWarningRects)
        {
            Vector2 pos = rect.anchoredPosition;
            topWarningStartPositions.Add(pos);
        }

        foreach (RectTransform rect in bottomWarningRects)
        {
            Vector2 pos = rect.anchoredPosition;
            bottomWarningStartPositions.Add(pos);
        }
    }

    private void ResetWarningPositions()
    {
        ResetWarningPositions(topWarningRects, topWarningStartPositions);
        ResetWarningPositions(bottomWarningRects, bottomWarningStartPositions);
    }

    private void ResetWarningPositions(List<RectTransform> rects, List<Vector2> startPositions)
    {
        for (int i = 0; i < rects.Count; i++)
        {
            Vector2 pos = startPositions[i];
            pos.x = i * totalWidth;
            rects[i].anchoredPosition = pos;
        }
    }

    public void ShowWarning()
    {
        if (isWarningActive)
            return;

        StartCoroutine(PlayBossWarningAnimation());
    }

    private IEnumerator PlayBossWarningAnimation()
    {
        panel.SetActive(true);
        isWarningActive = true;

        // 셀
        cellWidth = topWarningRects[0].rect.width;
        totalWidth = cellWidth + WARNING_SPACING;
        ResetWarningPositions();

        // 마스크
        maskWidth = mainWarningRect.rect.width;
        mainMask.enabled = true;
        mainMask.padding = maskPadding * maskWidth;

        // 동시에 실행
        Coroutine warningMoveCoroutine = StartCoroutine(MoveWarningImages());
        Coroutine fadeInCoroutine = StartCoroutine(FadeInMainWarning());
        Coroutine autoHideCoroutine = StartCoroutine(HideWarning());

        yield return warningMoveCoroutine;
        yield return fadeInCoroutine;
        yield return autoHideCoroutine;
    }

    private IEnumerator HideWarning()
    {
        yield return new WaitForSeconds(FADE_IN_START_DELAY + FADE_IN_DURATION + HIDE_DELAY);
        panel.SetActive(false);
        isWarningActive = false;
    }

    private IEnumerator MoveWarningImages()
    {
        while (isWarningActive)
        {
            MoveWarningImage(topWarningRects);
            MoveWarningImage(bottomWarningRects);
            yield return null;
        }
    }

    private void MoveWarningImage(List<RectTransform> rects)
    {
        foreach (RectTransform rect in rects)
        {
            Vector2 currentPos = rect.anchoredPosition;
            currentPos.x -= WARNING_MOVE_SPEED * Time.unscaledDeltaTime;

            if (rect.offsetMax.x < 0f)
                currentPos.x = FindRightmostPosition(rects) + totalWidth;

            rect.anchoredPosition = currentPos;
        }
    }

    private float FindRightmostPosition(List<RectTransform> rects)
    {
        float maxX = float.MinValue;

        foreach (RectTransform rect in rects)
        {
            float x = rect.anchoredPosition.x;
            if (x > maxX)
                maxX = x;
        }

        return maxX;
    }

    private IEnumerator FadeInMainWarning()
    {
        yield return new WaitForSeconds(FADE_IN_START_DELAY);

        float time = 0f;
        while (time < FADE_IN_DURATION)
        {
            time += Time.unscaledDeltaTime;
            float progress = time * FADE_IN_INV_DURATION;

            // 좌측부터 우측으로 보이도록
            float revealWidth = maskWidth * progress;
            mainMask.padding = maskPadding * (maskWidth - revealWidth);
            yield return null;
        }

        mainMask.padding = Vector4.zero;
        mainMask.enabled = false;
    }
}
