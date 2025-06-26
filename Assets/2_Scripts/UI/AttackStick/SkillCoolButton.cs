using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillCoolButton : BaseAttackButton
{
    [SerializeField] private Slider cooldownSlider;
    [SerializeField] private TextMeshProUGUI cooldownText;
    private SkillManager skillManager;
    private float maxCooldown;

    public override void Reset()
    {
        base.Reset();
        cooldownSlider.value = 1;
        cooldownText.text = string.Empty;
    }

    public override void Init(SkillKey skillKey)
    {
        base.Init(skillKey);

        maxCooldown = skillData.cooldown;
        cooldownSlider.maxValue = maxCooldown;
        cooldownSlider.value = maxCooldown;
        SubscribeToSkillManager();
    }

    private void SubscribeToSkillManager()
    {
        if (skillManager == null)
            skillManager = GameManager.Instance.skillManager;

        if (skillManager != null)
        {
            skillManager.OnSkillCooldownChanged += OnSkillCooldownChanged;
            skillManager.OnSkillCooldownEnded += OnSkillCooldownEnded;
        }
    }

    private void OnSkillCooldownChanged(SkillKey skillKey, float cooldown)
    {
        if (skillKey == skillData.skillKey)
            UpdateCooldown(cooldown);
    }

    private void OnSkillCooldownEnded(SkillKey skillKey)
    {
        if (skillKey == skillData.skillKey)
            EndCooldown();
    }

    public override void OnClick()
    {
        base.OnClick();

        if (skillManager != null)
            skillManager.UseSkill(skillData.skillKey);
    }

    public void UpdateCooldown(float cooldown)
    {
        cooldownSlider.value = maxCooldown - skillManager.GetCooldown(skillData.skillKey);
        cooldownText.text = $"{cooldown:F1}";
    }

    public void EndCooldown()
    {
        cooldownSlider.value = maxCooldown;
        cooldownText.text = string.Empty;
        attackButton.interactable = true;
    }

    private void OnDestroy()
    {
        if (skillManager != null)
        {
            skillManager.OnSkillCooldownChanged -= OnSkillCooldownChanged;
            skillManager.OnSkillCooldownEnded -= OnSkillCooldownEnded;
        }
    }
}
