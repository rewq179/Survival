using Unity.Burst.Intrinsics;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : HealthBarBase
{
    [SerializeField] private Slider healthBar;
    [SerializeField] private Slider damageBar;

    public override void Init(float hp, float maxHp)
    {
        healthBar.maxValue = maxHp;
        damageBar.maxValue = maxHp;
        SetHealthBar(1f);
        SetDamageBar(maxHp);
    }

    public override void ShowHealthBar(bool isShow)
    {
        healthBar.gameObject.SetActive(isShow);
        damageBar.gameObject.SetActive(isShow);
    }

    protected override void SetHealthBar(float ratio)
    {
        healthBar.value = healthBar.maxValue * ratio;
    }

    protected override void SetDamageBar(float ratio)
    {
        damageBar.value = damageBar.maxValue * ratio;
    }
}
