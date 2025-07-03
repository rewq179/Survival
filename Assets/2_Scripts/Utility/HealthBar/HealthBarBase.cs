using UnityEngine;
using System.Collections;

public abstract class HealthBarBase : MonoBehaviour
{
    protected AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    protected const float DAMAGE_DURATION = 0.5f;
    protected const float DAMAGE_INV_DURATION = 1f / DAMAGE_DURATION;
    protected Coroutine damageCoroutine;

    public virtual void UpdateHealthBar(float prevRatio, float nextRatio)
    {
        SetHealthBar(nextRatio);

        if (damageCoroutine != null)
            StopCoroutine(damageCoroutine);

        damageCoroutine = StartCoroutine(AnimateDamageBar(prevRatio, nextRatio));
    }

    private IEnumerator AnimateDamageBar(float prevRatio, float endRatio)
    {
        float elapsed = 0f;
        while (elapsed < DAMAGE_DURATION)
        {
            elapsed += Time.deltaTime;
            float ease = curve.Evaluate(Mathf.Clamp01(elapsed * DAMAGE_INV_DURATION));
            SetDamageBar(Mathf.Lerp(prevRatio, endRatio, ease));
            yield return null;
        }

        SetDamageBar(endRatio);
        damageCoroutine = null;
    }

    public abstract void Init(float hp, float maxHp);
    public abstract void ShowHealthBar(bool isShow);
    protected abstract void SetHealthBar(float ratio);
    protected abstract void SetDamageBar(float ratio);
}
