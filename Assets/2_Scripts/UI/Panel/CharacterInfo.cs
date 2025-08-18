using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CharacterInfo : MonoBehaviour
{
    [SerializeField] private HealthBarUI healthBar;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private Slider expBar;
    [SerializeField] private Image profileImage;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI nameText;

    private Unit unit;

    public void Init(Unit unit)
    {
        this.unit = unit;

        UnsubscribeFromEvents();
        SubscribeToEvents();
    }

    private void SubscribeToEvents()
    {
        if (unit == null)
            return;

        GameEvents events = GameEvents.Instance;
        events.OnPlayerHpChanged += UpdateHpUI;
        events.OnPlayerExpChanged += UpdateExpUI;
        events.OnPlayerLevelChanged += UpdateLevelUI;
    }

    private void UnsubscribeFromEvents()
    {
        if (unit == null)
            return;

        GameEvents events = GameEvents.Instance;
        events.OnPlayerHpChanged -= UpdateHpUI;
        events.OnPlayerExpChanged -= UpdateExpUI;
        events.OnPlayerLevelChanged -= UpdateLevelUI;
    }

    public void UpdateAllUI()
    {
        healthBar.Init(unit.CurHp, unit.MaxHp);
        hpText.text = $"{unit.CurHp}/{unit.MaxHp}";

        UpdateExpUI(unit.CurExp);
        UpdateLevelUI(unit.Level);
        SetName(string.Empty);
    }

    private void UpdateHpUI(float prevRatio, float nextRatio)
    {
        healthBar.UpdateHealthBar(prevRatio, nextRatio);
        hpText.text = $"{unit.CurHp}/{unit.MaxHp}";
    }

    private void UpdateExpUI(float exp)
    {
        expBar.maxValue = unit.MaxExp;
        expBar.value = exp;
    }

    private void UpdateLevelUI(int level)
    {
        levelText.text = level.ToString();
        expBar.maxValue = unit.MaxExp;
    }

    private void SetName(string name)
    {
        nameText.text = name;
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
}
