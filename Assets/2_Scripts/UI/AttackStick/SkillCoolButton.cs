
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillCoolButton : BaseAttackButton
{
    [SerializeField] private Slider cooldownSlider;
    [SerializeField] private TextMeshProUGUI cooldownText;

    public override void Reset()
    {
        base.Reset();
        cooldownSlider.value = 0;
        cooldownText.text = string.Empty;
    }

    public override void Init(int skillId)
    {
        base.Init(skillId);

        cooldownSlider.maxValue = DataManager.GetSkillData(skillId).cooldown;
    }

    public void UpdateCooldown(float cooldown)
    {
        cooldownSlider.value = cooldown;
        cooldownText.text = $"{cooldown:F1}";
    }
}
