using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterInfo : MonoBehaviour
{
    [SerializeField] private Slider hpBar;
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
        UpdateAllUI();
    }

    private void SubscribeToEvents()
    {
        if (unit == null)
            return;
            
        unit.OnHpChanged += UpdateHpUI;
        unit.OnExpChanged += UpdateExpUI;
        unit.OnLevelChanged += UpdateLevelUI;
    }

    private void UnsubscribeFromEvents()
    {
        if (unit == null)
            return;
            
        unit.OnHpChanged -= UpdateHpUI;
        unit.OnExpChanged -= UpdateExpUI;
        unit.OnLevelChanged -= UpdateLevelUI;
    }

    private void UpdateAllUI()
    {
        UpdateHpUI(unit.CurHp);
        UpdateExpUI(unit.CurExp);
        UpdateLevelUI(unit.Level);
        SetName(string.Empty);
    }

    private void UpdateHpUI(float hp)
    {
        hpBar.maxValue = unit.MaxHp;
        hpBar.value = hp;
        hpText.text = $"{hpBar.value}/{hpBar.maxValue}";
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
