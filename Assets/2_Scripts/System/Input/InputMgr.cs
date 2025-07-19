using UnityEngine;
using UnityEngine.InputSystem;

public class InputMgr : MonoBehaviour
{
    public static InputMgr Instance { get; private set; }
    private PlayerInputAction PlayerInputAction;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        // 씬이 로드되기 전에 InputManager 생성
        GameObject inputManagerObj = new GameObject("InputManager");
        Instance = inputManagerObj.AddComponent<InputMgr>();
        DontDestroyOnLoad(inputManagerObj);
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            PlayerInputAction = new PlayerInputAction();
        }
        
        else
        {
            Destroy(gameObject);
        }
    }

    public void EnablePlayerInput()
    {
        PlayerInputAction.Player.Enable();
    }

    public void DisablePlayerInput()
    {
        PlayerInputAction.Player.Disable();
    }

    public Vector2 GetMoveInput()
    {
        return PlayerInputAction.Player.Move.ReadValue<Vector2>();
    }

    public bool IsAttacking()
    {
        return PlayerInputAction.Player.Attack.IsPressed();
    }
}
