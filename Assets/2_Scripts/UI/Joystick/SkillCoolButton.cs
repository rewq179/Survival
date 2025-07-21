using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillCoolButton : BaseAttackButton
{
    [SerializeField] private Slider cooldownSlider;
    [SerializeField] private TextMeshProUGUI cooldownText;
    private SkillMgr skillManager;
    private float maxCooldown;

    public override void Reset()
    {
        base.Reset();
        cooldownSlider.value = 1;
        cooldownText.text = string.Empty;
    }

    public override void Init(Unit unit, SkillKey skillKey, bool isActive)
    {
        base.Init(unit, skillKey, isActive);

        maxCooldown = skillData.cooldown;
        cooldownSlider.maxValue = maxCooldown;
        cooldownSlider.value = maxCooldown;
        SubscribeToSkillManager();
    }

    private void SubscribeToSkillManager()
    {
        if (skillManager == null)
            skillManager = GameMgr.Instance.skillMgr;

        playerUnit.OnSkillCooldownChanged += OnSkillCooldownChanged;
        playerUnit.OnSkillCooldownEnded += OnSkillCooldownEnded;
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
            skillManager.UseSkill(skillData.skillKey, playerUnit);
    }

    public void UpdateCooldown(float cooldown)
    {
        cooldownSlider.value = maxCooldown - cooldown;
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
        if (playerUnit != null)
        {
            playerUnit.OnSkillCooldownChanged -= OnSkillCooldownChanged;
            playerUnit.OnSkillCooldownEnded -= OnSkillCooldownEnded;
        }
    }
}
