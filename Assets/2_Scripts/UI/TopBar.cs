using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TopBar : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private Button pauseButton;

    public void Init(Unit playerUnit)
    {
        goldText.text = string.Empty;
        playerUnit.OnGoldChanged += UpdateGoldDisplay;
        UpdateGoldDisplay(playerUnit.Gold);
        pauseButton.onClick.AddListener(OnPauseButtonClicked);
    }

    private void UpdateGoldDisplay(int gold)
    {
        goldText.text = gold.ToString("N0");
    }

    private void OnPauseButtonClicked()
    {
        UIMgr.Instance.pauseUI.Show();
    }
}
