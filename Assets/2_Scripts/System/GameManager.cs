using UnityEngine;

public class GameManager : MonoBehaviour
{
    private void Start()
    {
        InputManager.Instance.EnablePlayerInput();
    }

    private void OnGamePause()
    {
        InputManager.Instance.DisablePlayerInput();
    }
}
