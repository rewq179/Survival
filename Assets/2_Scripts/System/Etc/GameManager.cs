using UnityEngine;
using System;
using UnityEngine.Video;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private GameObject playerUnitPrefab;
    [SerializeField] private DataMgr dataManager;
    public ResourceMgr resourceMgr;
    public SkillManager skillManager;
    public SpawnManager spawnManager;
    public CameraManager cameraManager;
    public DamageTextMgr damageTextMgr;
    public RewardMgr rewardMgr;

    private Unit playerUnit;
    private int currentWave;

    public Unit PlayerUnit => playerUnit;

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
        resourceMgr.LoadAllIcons();
        dataManager.Init();

        CreatePlayerUnit();

        spawnManager.Init(currentWave);
        InputManager.Instance.EnablePlayerInput();
    }

    private void CreatePlayerUnit()
    {
        if (playerUnit == null)
        {
            playerUnit = Instantiate(playerUnitPrefab, Vector3.zero, Quaternion.identity).GetComponent<Unit>();
            spawnManager.SetPlayerTransform(playerUnit.transform);
            cameraManager.SetTarget(playerUnit.transform);

        }
        
        skillManager.Init(playerUnit);
        rewardMgr.Init(playerUnit);
        UIMgr.Instance.Init(playerUnit);
        playerUnit.Init(100, Vector3.zero);
        UIMgr.Instance.UpdateUI();
    }

    public void OnGameResume()
    {
        Time.timeScale = 1f;
        InputManager.Instance.EnablePlayerInput();
    }

    public void OnGamePause()
    {
        Time.timeScale = 0f;
        InputManager.Instance.DisablePlayerInput();
    }

    public void Test()
    {
        playerUnit.AddExp(250);
    }
}
