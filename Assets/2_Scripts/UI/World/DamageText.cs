using UnityEngine;
using TMPro;
using System.Collections;

public class DamageText : MonoBehaviour
{
    [SerializeField] private TextMeshPro damageText; // 3D용
    private AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    private DamageTextMgr manager;

    // 애니메이션 설정
    private Vector3 startPos;
    private Vector3 endPos;
    private float moveDistance;
    float height;
    private float currentTime;
    private float fadeTime;
    private float fadeInvTime;

    public void Reset()
    {
        StopAllCoroutines();
        damageText.text = string.Empty;
        damageText.alpha = 1f;
        gameObject.SetActive(false);
    }

    public void Initialize(DamageTextMgr manager, float fadeTime, float moveDistance)
    {
        this.manager = manager;
        this.fadeTime = fadeTime;
        this.moveDistance = moveDistance;
        height = moveDistance;
        fadeInvTime = 1f / fadeTime;
        gameObject.SetActive(false);
    }

    public void ShowDamageText(Vector3 worldPosition, int damage, Color color)
    {
        worldPosition.y += 2f;
        transform.position = worldPosition;
        startPos = worldPosition;
        endPos = startPos + Vector3.down * moveDistance;

        // 텍스트 설정
        damageText.text = damage.ToString();
        damageText.color = color;
        damageText.alpha = 1f;

        gameObject.SetActive(true);
        currentTime = 0f;
        StartCoroutine(AnimateDamageText());
    }

    private IEnumerator AnimateDamageText()
    {
        while (currentTime < fadeTime)
        {
            currentTime += Time.deltaTime;
            float progress = currentTime * fadeInvTime;
            float ease = curve.Evaluate(progress);

            // 위치 조정
            float x = Mathf.Lerp(startPos.x, endPos.x, ease);
            float z = Mathf.Lerp(startPos.z, endPos.z, ease);
            float y = Mathf.Lerp(startPos.y, endPos.y, ease)
                    + (-4f * height * Mathf.Pow(ease - 0.5f, 2f) + height);
            transform.position = new Vector3(x, y, z);

            // 페이드아웃
            float alpha = Mathf.Lerp(1f, 0f, ease);
            damageText.alpha = alpha;
            yield return null;
        }

        manager.PushDamageText(this);
    }
}
