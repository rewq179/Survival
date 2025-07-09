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
    }

    public void UpdateUI()
    {
        characterInfo.UpdateAllUI();
    }
}