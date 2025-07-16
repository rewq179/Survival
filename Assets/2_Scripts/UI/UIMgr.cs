using UnityEngine;
using TMPro;

public class UIMgr : MonoBehaviour
{
    public static UIMgr Instance { get; private set; }

    [Header("UI Components")]
    public CharacterInfo characterInfo;
    public AttackJoystick attackJoystick;
    public TopBar topBar;
    public SelectionPanel selectionPanel;
    public StageUI stageUI;
    public WarningUI warningUI;
    public GameOverUI gameOverUI;
    public AutoAttackUI autoAttackUI;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        else
        {
            Destroy(gameObject);
        }
    }

    public void Init(Unit playerUnit)
    {
        characterInfo.Init(playerUnit);
        attackJoystick.Init(playerUnit);
        topBar.Init(playerUnit);
        selectionPanel.Init(playerUnit);
        stageUI.Init(1);
        gameOverUI.Init();
        autoAttackUI.Init(playerUnit);
    }

    public void UpdateUI()
    {
        characterInfo.UpdateAllUI();
    }
}