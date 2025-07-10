using UnityEngine;
using System;
using System.Collections.Generic;

public enum AnimEvent
{
    Attack,
    Die,
}

public class Unit : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private HealthBarBase healthBar;
    [SerializeField] private PlayerController playerController;

    // 모듈
    private StatModule statModule = new();
    private BehaviourModule behaviourModule;
    private CombatModule combatModule = new();
    private SkillModule skillModule = new();

    // 기본 정보
    private PlayerSaveData playerSaveData = new();
    private int uniqueID;
    private int unitID;
    private bool isPlayer;

    // 기타
    private Camera mainCam;

    public int UniqueID => uniqueID;
    public int UnitID => unitID;
    public bool IsPlayer => isPlayer;
    public SkillModule SkillModule => skillModule;

    public void Reset()
    {
        if (healthBar != null)
            OnHpChanged -= healthBar.UpdateHealthBar;

        combatModule.Reset();
        transform.position = Vector3.zero;
        gameObject.SetActive(false);
        healthBar?.ShowHealthBar(false);
    }

    public void Init(int uniqueID, int unitID, Vector3 position)
    {
        this.uniqueID = uniqueID;
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
        if (behaviourModule == null)
            behaviourModule = isPlayer ? new BehaviourPlayerModule() : new BehaviourMonsterModule();

        UnitData data = DataMgr.GetUnitData(unitID);
        statModule.Init(data);
        combatModule.Init(this);
        behaviourModule.Init(this);
        skillModule.Init(this, data);
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
    public float MaxHp => statModule.MaxHP;
    public float MoveSpeed => statModule.MoveSpeed;
    public float GetFinalStat(StatType statType) => statModule.GetFinalStat(statType);
    public void AddStatModifier(StatType statType, float value) => statModule.AddStatModifier(statType, value);

    // BehaviourModule
    public void OnAnimationEnd(AnimEvent animEvent) => behaviourModule.OnAnimationEnd(animEvent);

    // CombatModule
    public bool IsDead => combatModule.IsDead;
    public float CurHp => combatModule.CurHp;
    public event Action<float, float> OnHpChanged
    {
        add => combatModule.OnHpChanged += value;
        remove => combatModule.OnHpChanged -= value;
    }

    public void UpdateHp() => combatModule.UpdateHp();
    public void UpdateMoveSpeed() => playerController.UpdateMoveSpeed();
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
    public List<SkillKey> GetSubSkills(SkillKey skillKey) => skillModule.GetSubSkills(skillKey);
    public bool HasSkill(SkillKey skillKey) => skillModule.HasSkill(skillKey);
    public bool IsSkillLearnable(SkillKey skillKey) => skillModule.IsSkillLearnable(skillKey);
    public SkillInstance GetSkillInstance(SkillKey skillKey) => skillModule.GetSkillInstance(skillKey);

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
