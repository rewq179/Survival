using UnityEngine;
using TMPro;

public class UIMgr : MonoBehaviour
{
    public static UIMgr Instance { get; private set; }
    
    public CharacterInfo characterInfo;
    public AttackJoystick attackJoystick;
    public TopBar topBar;

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
    }
}