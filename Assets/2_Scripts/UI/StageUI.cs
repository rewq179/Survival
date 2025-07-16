using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class StageUI : MonoBehaviour
{
    [Header("Top Components")]
    [SerializeField] private RectTransform topStageRect;
    [SerializeField] private TextMeshProUGUI topStageText;
    [SerializeField] private TextMeshProUGUI topStagePrevText;
    [SerializeField] private TextMeshProUGUI topStageNextText;
    [SerializeField] private Slider topStageSlider;

    [Header("Center Components")]
    [SerializeField] private RectTransform centerStageRect;
    [SerializeField] private CanvasGroup centerStageCanvasGroup;
    [SerializeField] private TextMeshProUGUI centerStageText;
    private Vector2 centerStageEndPos;

    // 애니메이션
    private const float FALL_DURATION = 0.25f;
    private const float FALL_INV_DURATION = 1 / FALL_DURATION;
    private const float BOUNCE_STRENGTH = 30f;
    private const float BOUNCE_DURATION = 0.3f;
    private const float BOUNCE_INV_DURATION = 1 / BOUNCE_DURATION;
    private const float FADEOUT_DURATION = 0.15f;
    private const float FADEOUT_INV_DURATION = 1 / FADEOUT_DURATION;
    private readonly string stageFormat = "Stage {0}";

    private void Start()
    {
        centerStageEndPos = centerStageRect.anchoredPosition;

        Init(1);
    }

    public void Init(int stage)
    {
        string curStage = string.Format(stageFormat, stage);
        topStageText.text = curStage;
        centerStageText.text = curStage;
        topStagePrevText.text = (stage - 1).ToString();
        topStageNextText.text = stage.ToString();
        topStageSlider.value = 0f;

        // 초기 상태 설정
        centerStageRect.anchoredPosition = topStageRect.anchoredPosition;
        centerStageCanvasGroup.alpha = 0f;

        // 애니메이션 시작
        StartCoroutine(PlayStageAnimation());
    }

    private IEnumerator PlayStageAnimation()
    {
        yield return StartCoroutine(PlayFallAnimation());
        yield return StartCoroutine(PlayBounceAnimation());
        yield return StartCoroutine(PlayFadeOutAnimation());
    }

    /// <summary>
    /// 낙하 애니메이션
    /// </summary>
    private IEnumerator PlayFallAnimation()
    {
        Vector2 startPos = topStageRect.anchoredPosition;
        Vector2 targetPos = centerStageEndPos;
        float time = 0f;

        while (time < FALL_DURATION)
        {
            time += Time.deltaTime;
            float p = time * FALL_INV_DURATION;
            float e = p * p;

            // 위치 & 알파 조정
            centerStageRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, e);
            centerStageCanvasGroup.alpha = Mathf.Lerp(0f, 1f, p);
            yield return null;
        }

        centerStageRect.anchoredPosition = targetPos;
        centerStageCanvasGroup.alpha = 1f;
        yield return null;
    }

    private IEnumerator PlayBounceAnimation()
    {
        Vector2 originalPos = centerStageRect.anchoredPosition;
        float time = 0f;

        while (time < BOUNCE_DURATION)
        {
            time += Time.deltaTime;
            float p = time * BOUNCE_INV_DURATION;

            float offset = Mathf.Sin(p * Mathf.PI) * BOUNCE_STRENGTH * (1f - p);
            Vector2 bouncePos = originalPos + Vector2.up * offset;
            centerStageRect.anchoredPosition = bouncePos;
            yield return null;
        }

        centerStageRect.anchoredPosition = originalPos;
        yield return null;
    }

    private IEnumerator PlayFadeOutAnimation()
    {
        float time = 0f;

        while (time < FADEOUT_DURATION)
        {
            time += Time.deltaTime;
            float p = time * FADEOUT_INV_DURATION;
            centerStageCanvasGroup.alpha = Mathf.Lerp(1f, 0f, p);
            yield return null;
        }

        centerStageCanvasGroup.alpha = 0f;
        yield return null;
    }

    public void UpdateStageSlider(float ratio) => topStageSlider.value = ratio;
}
