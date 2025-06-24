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

    public override void Init(int skillId)
    {
        base.Init(skillId);

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

    private void OnSkillCooldownChanged(int skillId, float cooldown)
    {
        if (skillId == skillData.id)
            UpdateCooldown(cooldown);
    }

    private void OnSkillCooldownEnded(int skillId)
    {
        if (skillId == skillData.id)
            EndCooldown();
    }

    public override void OnClick()
    {
        base.OnClick();

        if (skillManager != null)
            skillManager.UseSkill(skillData.id);
    }

    public void UpdateCooldown(float cooldown)
    {
        cooldownSlider.value = maxCooldown - skillManager.GetCooldownRatio(skillData.id);
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
