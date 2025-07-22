using UnityEngine;
using System;

public class GameMgr : MonoBehaviour
{
    public static GameMgr Instance { get; private set; }

    [SerializeField] private DataMgr dataManager;
    public ResourceMgr resourceMgr;
    public SkillMgr skillMgr;
    public SpawnMgr spawnMgr;
    public CameraMgr cameraMgr;
    public DamageTextMgr damageTextMgr;
    public RewardMgr rewardMgr;

    private Unit playerUnit;
    private int stage = 1;

    public int Stage => stage;
    public Unit PlayerUnit => playerUnit;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
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
        ResetStaticValues();
        resourceMgr.LoadAllIcons();
        dataManager.Init();

        CreatePlayerUnit();
        OnGameStart();
    }

    private void ResetStaticValues()
    {
        SpawnMgr.UNIT_UNIQUE_ID = 0;
        BuffHolder.InitInputer();
    }

    private void CreatePlayerUnit()
    {
        if (playerUnit != null)
            return;

        playerUnit = resourceMgr.GetPlayerUnit();
        spawnMgr.SetPlayerTransform(playerUnit.transform);
        cameraMgr.SetTarget(playerUnit.transform);
    }

    public void OnGameStart()
    {
        skillMgr.Init(playerUnit);
        rewardMgr.Init(playerUnit);
        UIMgr.Instance.Init(playerUnit, stage);
        playerUnit.Init(SpawnMgr.UNIT_UNIQUE_ID++, 100, Vector3.zero);

        UIMgr.Instance.UpdateUI();

#if UNITY_EDITOR
        // playerUnit.LearnSkill(SkillKey.IseAttack);
        // playerUnit.AddBuff(BuffKey.Freeze, playerUnit);
        // playerUnit.AddBuff(BuffKey.Stun, playerUnit);
#endif

        spawnMgr.Init();
        OnGameResume();
    }

    public void OnGameRestart()
    {
        stage = 1;
        playerUnit.Reset();
        spawnMgr.Reset();
        OnGameStart();
    }

    public void OnGameResume()
    {
        Time.timeScale = 1f;
        InputMgr.Instance.EnablePlayerInput();
    }

    public void OnGamePause()
    {
        Time.timeScale = 0f;
        InputMgr.Instance.DisablePlayerInput();
    }

    public void Test()
    {
        // playerUnit.AddExp(playerUnit.MaxExp);
        skillMgr.ExecuteItemSkill(playerUnit, CollectibleType.Explosion);
    }
}
