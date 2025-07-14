using UnityEngine;
using System.Collections;

public abstract class HealthBarBase : MonoBehaviour
{
    [SerializeField] private GameObject panelObject;

    protected AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    protected const float DAMAGE_DURATION = 0.5f;
    protected const float DAMAGE_INV_DURATION = 1f / DAMAGE_DURATION;
    protected Coroutine damageCoroutine;

    public virtual void UpdateHealthBar(float prevRatio, float nextRatio)
    {
        if (damageCoroutine != null)
        {
            StopCoroutine(damageCoroutine);
            damageCoroutine = null;
        }

        SetHealthBar(nextRatio);
        if (nextRatio > prevRatio)
            SetDamageBar(nextRatio);

        else
            damageCoroutine = StartCoroutine(AnimateDamageBar(prevRatio, nextRatio));
    }

    private IEnumerator AnimateDamageBar(float prevRatio, float endRatio)
    {
        float time = 0f;
        while (time < DAMAGE_DURATION)
        {
            time += Time.unscaledDeltaTime;
            float e = curve.Evaluate(Mathf.Clamp01(time * DAMAGE_INV_DURATION));
            SetDamageBar(Mathf.Lerp(prevRatio, endRatio, e));
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
