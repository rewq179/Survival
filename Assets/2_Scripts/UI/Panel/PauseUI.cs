using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PauseUI : MonoBehaviour
{
    [SerializeField] private RectTransform panel;
    [SerializeField] private TextMeshProUGUI stageText;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button exitButton;

    public void Init()
    {
        resumeButton.onClick.AddListener(OnResumeButtonClicked);
        restartButton.onClick.AddListener(OnRestartButtonClicked);
        exitButton.onClick.AddListener(OnExitButtonClicked);
    }

    public void Show()
    {
        GameMgr.Instance.OnGamePause();
        stageText.text = $"Stage {GameMgr.Instance.Stage}";
        panel.gameObject.SetActive(true);
    }

    private void OnResumeButtonClicked()
    {
        GameMgr.Instance.OnGameResume();
        panel.gameObject.SetActive(false);
    }

    private void OnRestartButtonClicked()
    {
        GameMgr.Instance.OnGameRestart();
        panel.gameObject.SetActive(false);
    }

    private void OnExitButtonClicked()
    {
        // GameMgr.Instance.OnGameExit();
        panel.gameObject.SetActive(false);
    }
}
