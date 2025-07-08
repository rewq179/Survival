using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.Experimental.AI;

public class Unit : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private HealthBarBase healthBar;
    [SerializeField] private PlayerController playerController;

    // 모듈
    private StatModule statModule;
    private BehaviourModule behaviourModule;
    private CombatModule combatModule;
    private SkillModule skillModule;

    // 기본 정보
    private PlayerSaveData playerSaveData = new();
    private int unitID;
    private bool isPlayer;

    // 기타
    private Camera mainCam;

    public int UnitID => unitID;
    public bool IsPlayer => isPlayer;

    public void Reset()
    {
        if (healthBar != null)
            OnHpChanged -= healthBar.UpdateHealthBar;

        combatModule.Reset();
        transform.position = Vector3.zero;
        gameObject.SetActive(false);
        healthBar?.ShowHealthBar(false);
    }

    public void Init(int unitID, Vector3 position)
    {
        this.unitID = unitID;
        isPlayer = unitID < 1000;
        InitModule();
        InitHealthBar();
        playerSaveData.Init(this);
        playerController?.Init(this);
        transform.position = position;
        mainCam = Camera.main;
        gameObject.SetActive(true);
    }

    private void InitModule()
    {
        if (statModule == null)
            statModule = new StatModule();
        if (behaviourModule == null)
            behaviourModule = isPlayer ? new BehaviourPlayerModule() : new BehaviourMonsterModule();
        if (combatModule == null)
            combatModule = new CombatModule();
        if (playerSaveData == null)
            playerSaveData = new PlayerSaveData();
        if (skillModule == null)
            skillModule = new SkillModule();

        statModule.Init(DataMgr.GetUnitData(unitID));
        combatModule.Init(this);
        behaviourModule.Init(this);
        skillModule.Init(this);
    }

    private void InitHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.Init(CurHp, MaxHp);
            OnHpChanged += healthBar.UpdateHealthBar;
        }
    }

    private void Update()
    {
        behaviourModule?.Update();
        skillModule.UpdateCooldowns();
    }

    private void LateUpdate()
    {
        if (healthBar != null)
            healthBar.transform.rotation = Quaternion.LookRotation(mainCam.transform.forward);
    }

    // 애니메이션
    public void PlayAnimation(string name) => animator.Play(name);
    public void SetTrigger(string name) => animator.SetTrigger(name);

    // StatModule
    public float MaxHp => statModule.GetFinalStat(StatType.Health);
    public float MoveSpeed => statModule.GetFinalStat(StatType.MoveSpeed);
    public void AddBaseStatValue(StatType statType, float value) => statModule.AddBaseStatValue(statType, value);
    public void AddStatModifier(StatType statType, float value) => statModule.AddStatModifier(statType, value); 

    // BehaviourModule
    public void OnAttackAnimationEnd() => behaviourModule.OnAttackAnimationEnd();

    // CombatModule
    public bool IsDead => combatModule.IsDead;
    public float CurHp => combatModule.CurHp;
    public event Action<float, float> OnHpChanged
    {
        add => combatModule.OnHpChanged += value;
        remove => combatModule.OnHpChanged -= value;
    }

    public void UpdateHp() => combatModule.UpdateHp();
    public void TakeHealRate(float healRate) => combatModule.TakeHeal(MaxHp * healRate);
    public void TakeHeal(float healAmount) => combatModule.TakeHeal(healAmount);
    public void TakeDamage(float damage) => combatModule.TakeDamage(damage);

    // PlayerSaveData
    public int Level => playerSaveData.level;
    public float CurExp => playerSaveData.exp;
    public float MaxExp => playerSaveData.GetRequiredExp(playerSaveData.level);
    public int Gold => playerSaveData.gold;
    
    public event Action<int> OnLevelChanged
    {
        add => playerSaveData.OnLevelChanged += value;
        remove => playerSaveData.OnLevelChanged -= value;
    }

    public event Action<float> OnExpChanged
    {
        add => playerSaveData.OnExpChanged += value;
        remove => playerSaveData.OnExpChanged -= value;
    }

    public event Action<int> OnGoldChanged
    {
        add => playerSaveData.OnGoldChanged += value;
        remove => playerSaveData.OnGoldChanged -= value;
    }

    public void AddGold(int amount) => playerSaveData.AddGold(amount);
    public int AddExp(float amount) => playerSaveData.AddExp(amount);

    // SkillModule
    public void LearnSkill(SkillKey skillKey) => skillModule.LearnSkill(skillKey);
    public bool CanUseSkill(SkillKey skillKey) => skillModule.CanUseSkill(skillKey);
    public void StartCooldown(SkillKey skillKey) => skillModule.StartCooldown(skillKey);
    public int GetSkillLevel(SkillKey skillKey) => skillModule.GetSkillLevel(skillKey);
    public bool HasSkill(SkillKey skillKey) => skillModule.HasSkill(skillKey);

    public event Action<SkillKey, float> OnSkillCooldownChanged
    {
        add => skillModule.OnSkillCooldownChanged += value;
        remove => skillModule.OnSkillCooldownChanged -= value;
    }

    public event Action<SkillKey> OnSkillCooldownEnded
    {
        add => skillModule.OnSkillCooldownEnded += value;
        remove => skillModule.OnSkillCooldownEnded -= value;
    }

    public event Action<SkillKey> OnSkillAdded
    {
        add => skillModule.OnSkillAdded += value;
        remove => skillModule.OnSkillAdded -= value;
    }

    public event Action<SkillKey> OnSkillRemoved
    {
        add => skillModule.OnSkillRemoved += value;
        remove => skillModule.OnSkillRemoved -= value;
    }

    public event Action<SkillKey, int> OnSkillLevelChanged
    {
        add => skillModule.OnSkillLevelChanged += value;
        remove => skillModule.OnSkillLevelChanged -= value;
    }
}
