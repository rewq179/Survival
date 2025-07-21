using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Button retryButton;

    public void Init()
    {
        panel.SetActive(false);
        retryButton.onClick.AddListener(GameMgr.Instance.OnGameRestart);
    }

    public void ShowGameOverUI()
    {
        panel.SetActive(true);
        GameMgr.Instance.OnGamePause();
    }
}
