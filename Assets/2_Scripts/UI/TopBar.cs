using UnityEngine;
using TMPro;

public class TopBar : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI goldText;

    public void Init(Unit playerUnit)
    {
        goldText.text = string.Empty;
        playerUnit.OnGoldChanged += UpdateGoldDisplay;
        UpdateGoldDisplay(playerUnit.Gold);
    }

    private void UpdateGoldDisplay(int gold)
    {
        goldText.text = gold.ToString("N0");
    }
}
