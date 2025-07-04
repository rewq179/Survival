using UnityEngine;
using System;
using UnityEngine.Video;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private GameObject playerUnitPrefab;
    [SerializeField] private DataManager dataManager;
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
        skillManager.Init(playerUnit);
        rewardMgr.Init(playerUnit);
        UIMgr.Instance.Init(playerUnit);

        playerUnit.AddSkill(SkillKey.Arrow);
        playerUnit.AddSkill(SkillKey.Dagger);
        playerUnit.AddSkill(SkillKey.FrontSpike);
        playerUnit.AddSkill(SkillKey.Meteor);

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
        
        playerUnit.Init(100, Vector3.zero);
    }

    private void OnGamePause()
    {
        InputManager.Instance.DisablePlayerInput();
    }

    public void Test()
    {
        rewardMgr.CreateCollectibleItem(CollectibleType.Magnet, playerUnit.transform.position, 0.6f);
    }
}
