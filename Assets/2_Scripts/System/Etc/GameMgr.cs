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
        ResetStaticValues();
        resourceMgr.LoadAllIcons();
        dataManager.Init();

        CreatePlayerUnit();

        spawnMgr.Init();
        InputMgr.Instance.EnablePlayerInput();
    }

    private void ResetStaticValues()
    {
        SpawnMgr.UNIT_UNIQUE_ID = 0;
    }

    private void CreatePlayerUnit()
    {
        if (playerUnit == null)
        {
            playerUnit = resourceMgr.GetPlayerUnit();
            spawnMgr.SetPlayerTransform(playerUnit.transform);
            cameraMgr.SetTarget(playerUnit.transform);
        }

        skillMgr.Init(playerUnit);
        rewardMgr.Init(playerUnit);
        UIMgr.Instance.Init(playerUnit);
        playerUnit.Init(SpawnMgr.UNIT_UNIQUE_ID++, 100, Vector3.zero);

        UIMgr.Instance.UpdateUI();
        playerUnit.LearnSkill(SkillKey.Meteor);
        playerUnit.LearnSkill(SkillKey.EnergyExplosion);
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
