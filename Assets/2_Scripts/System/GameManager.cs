using UnityEngine;
using System;
using UnityEngine.Video;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private GameObject playerUnitPrefab;
    [SerializeField] private DataManager dataManager;
    [SerializeField] private IconManager iconManager;

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
        // 아이콘 먼저 로드
        iconManager.LoadAllIcons();

        dataManager.Init();
        CreatePlayerUnit();
        SetupUnit();
        SetupUI();

        playerUnit.Init();
        playerUnit.AddGold(100);
        playerUnit.AddSkill(1);
        playerUnit.AddSkill(2);
        playerUnit.AddSkill(3);

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
        UIMgr.Instance.attackJoystick.Init(playerUnit);

        // 예: 체력바, 스킬 아이콘, 인벤토리 등
    }

    private void OnGamePause()
    {
        InputManager.Instance.DisablePlayerInput();
    }

    public void Test()
    {
        playerUnit.AddExp(10);
        playerUnit.TakeDamage(UnityEngine.Random.Range(-10, 10));
    }
}
