using UnityEngine;

public class UIMgr : MonoBehaviour
{
    public static UIMgr Instance { get; private set; }
    
    public CharacterInfo characterInfo;
    public AttackJoystick attackJoystick;

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
}
