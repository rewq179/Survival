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

    public void Init(Unit unit)
    {
        SetHp(unit);
        SetExp(unit);
        SetLevel(unit.Level);
        SetName(string.Empty);
    }

    private void SetHp(Unit unit)
    {
        hpBar.maxValue = unit.MaxHp;
        hpBar.value = unit.CurHp;
        hpText.text = $"{hpBar.value}/{hpBar.maxValue}";
    }

    private void SetExp(Unit unit)
    {
        expBar.maxValue = unit.MaxExp;
        expBar.value = unit.CurExp;
    }

    private void SetLevel(int level)
    {
        levelText.text = level.ToString();
    }

    private void SetName(string name)
    {
        nameText.text = name;
    }
}
