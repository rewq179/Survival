using UnityEngine;
using System;
using UnityEngine.Video;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private GameObject playerUnitPrefab;

    private Unit playerUnit;

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

    private void Start()
    {
        InitializeGame();
    }

    private void InitializeGame()
    {
        CreatePlayerUnit();
        SetupUnit();
        SetupUI();

        InputManager.Instance.EnablePlayerInput();
    }

    private void CreatePlayerUnit()
    {
        if (playerUnit == null)
            playerUnit = Instantiate(playerUnitPrefab, Vector3.zero, Quaternion.identity).GetComponent<Unit>();
    }

    private void SetupUnit()
    {
        // 유닛 컴포넌트 초기화
        // 예: 스탯, 애니메이션, 컨트롤러 등
    }

    private void SetupUI()
    {
        UIMgr.Instance.characterInfo.Init(playerUnit);
        // 예: 체력바, 스킬 아이콘, 인벤토리 등
    }

    private void OnGamePause()
    {
        InputManager.Instance.DisablePlayerInput();
    }
}
