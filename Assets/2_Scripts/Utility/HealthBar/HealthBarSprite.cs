using UnityEngine;

public class HealthBarSprite : HealthBarBase
{
    [SerializeField] private SpriteRenderer healthBar;
    [SerializeField] private SpriteRenderer damageBar;
    private Vector2 healthBarSize;
    private Vector2 damageBarSize;

    public override void Init(float hp, float maxHp)
    {
        healthBarSize = healthBar.size;
        damageBarSize = damageBar.size;
        SetHealthBar(1f);
        SetDamageBar(1f);
    }

    public override void ShowHealthBar(bool isShow)
    {
        healthBar.enabled = isShow;
        damageBar.enabled = isShow;
    }

    protected override void SetHealthBar(float ratio)
    {
        healthBar.size = new Vector2(healthBarSize.x * ratio, healthBarSize.y);
    }

    protected override void SetDamageBar(float ratio)
    {
        damageBar.size = new Vector2(damageBarSize.x * ratio, damageBarSize.y);
    }
}
