using UnityEngine;
using System;
using System.Collections.Generic;

public enum AnimEvent
{
    Attack,
    Die,
}

public enum UnitType
{
    Player,
    Monster,
}

public enum StatusEffect
{
    None = 0,
    Stunned = 1 << 0,
    Silenced = 1 << 2,

    CanNotMove = Stunned,
    CanNotAttack = Stunned | Silenced,
}

public class Unit : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private HealthBarBase healthBar;
    [SerializeField] private PlayerController playerController;
    public Transform firePoint;

    // 모듈
    private StatModule statModule = new();
    private BehaviourModule behaviourModule;
    private CombatModule combatModule = new();
    private SkillModule skillModule = new();
    private BuffModule buffModule = new();

    // 기본 정보
    private PlayerSaveData playerSaveData = new();
    private int uniqueID;
    private int unitID;
    private UnitType unitType;

    // 기타
    private Camera mainCamera;

    public int UniqueID => uniqueID;
    public int UnitID => unitID;
    public bool IsPlayer => unitType == UnitType.Player;
    public bool CanMove => IsActionable && buffModule.CanMove;
    public bool CanAttack => IsActionable && buffModule.CanAttack;
    public bool IsActionable => !IsDead && !IsForceMoving && !IsAttacking;
    public UnitType UnitType => unitType;

    public void Reset()
    {
        statModule.Reset();
        combatModule.Reset();
        skillModule.Reset();
        behaviourModule?.Reset();
        playerSaveData.Reset();

        uniqueID = -1;
        unitID = 0;
        unitType = UnitType.Player;
        mainCamera = null;
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;

        if (healthBar != null)
        {
            OnHpChanged -= healthBar.UpdateHealthBar;
            healthBar.ShowHealthBar(false);
        }

        gameObject.SetActive(false);
    }

    public void Init(int uniqueID, int unitID, Vector3 position)
    {
        this.uniqueID = uniqueID;
        this.unitID = unitID;
        unitType = unitID < 1000 ? UnitType.Player : UnitType.Monster;
        InitModule();
        InitHealthBar();
        playerSaveData.Init(this);
        playerController?.Init(this);
        transform.position = position;
        mainCamera = Camera.main;
        gameObject.SetActive(true);

#if UNITY_EDITOR
        UnitData data = DataMgr.GetUnitData(unitID);
        gameObject.name = $"{data.name}_{uniqueID}";
#endif
    }

    private void InitModule()
    {
        if (behaviourModule == null)
            behaviourModule = IsPlayer ? new BehaviourPlayerModule() : new BehaviourMonsterModule();

        UnitData data = DataMgr.GetUnitData(unitID);
        statModule.Init(data);
        combatModule.Init(this);
        skillModule.Init(this, data);
        buffModule.Init(this);
        behaviourModule.Init(this);
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
        float deltaTime = Time.deltaTime;
        behaviourModule.UpdateBehaviour();
        skillModule.UpdateCooldowns(deltaTime);
        skillModule.UpdateAutoAttack(deltaTime);
        buffModule.UpdateBuff(deltaTime);
    }

    private void LateUpdate()
    {
        if (healthBar != null)
            healthBar.transform.rotation = Quaternion.LookRotation(mainCamera.transform.forward);
    }

    public void UpdateMoveSpeed()
    {
        if (IsPlayer)
            playerController.UpdateMoveSpeed();
        else
            behaviourModule.UpdateMoveSpeed();
    }

    // StatModule
    public float MaxHp => statModule.MaxHP;
    public float MoveSpeed => statModule.MoveSpeed;
    public float GetFinalStat(StatType statType) => statModule.GetFinalStat(statType);
    public void AddStatModifier(StatType statType, float value) => statModule.AddStatModifier(statType, value);

    // BehaviourModule
    public bool IsAttacking => behaviourModule.IsAttacking;
    public bool IsForceMoving => behaviourModule.IsForceMoving;
    public void SetForceMoving(bool isForceMoving) => behaviourModule.SetForceMoving(isForceMoving);
    public void SetAIState(AIState state) => behaviourModule.SetAIState(state);
    public void SetAttacking(bool isAttacking) => behaviourModule.SetAttacking(isAttacking);

    public void PlayAnimation(string name, AnimationType type) => behaviourModule.PlayAnimation(name, type);
    public void PlayAnimation(AnimationType type) => behaviourModule.PlayAnimation(type);

    public void OnAnimationEnd(AnimEvent animEvent)
    {
        behaviourModule.OnAnimationEnd(animEvent);
    }

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
    public float MaxExp => GameValue.GetRequiredExp(playerSaveData.level);
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
    public SkillModule SkillModule => skillModule;
    public void SetAutoAttack(bool isAutoAttack) => skillModule.SetAutoAttack(isAutoAttack);
    public void LearnSkill(SkillKey skillKey) => skillModule.LearnSkill(skillKey);
    public bool CanUseSkill(SkillKey skillKey) => skillModule.CanUseSkill(skillKey);
    public void StartCooldown(SkillKey skillKey) => skillModule.StartCooldown(skillKey);
    public int GetSkillLevel(SkillKey skillKey) => skillModule.GetSkillLevel(skillKey);
    public List<SkillKey> GetSubSkills(SkillKey skillKey) => skillModule.GetSubSkills(skillKey);
    public bool HasSkill(SkillKey skillKey) => skillModule.HasSkill(skillKey);
    public bool HasRangedAttackSkill() => skillModule.HasRangedAttackSkill();
    public bool IsSkillLearnable(SkillKey skillKey) => skillModule.IsSkillLearnable(skillKey);
    public SkillInstance GetSkillInstance(SkillKey skillKey) => skillModule.GetSkillInstance(skillKey);
    public void UseRandomSkill(Unit target, RangeType rangeType) => skillModule.UseRandomSkill(target, rangeType);

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

    // BuffModule
    public BuffModule BuffModule => buffModule;
    public BuffInstance GetBuffInstance(BuffKey buffKey) => buffModule.GetBuffInstance(buffKey);
    public List<BuffInstance> GetActiveBuffInstances() => buffModule.GetActiveBuffInstances();

    public bool HasBuff(BuffKey buffKey) => buffModule.HasBuff(buffKey);
    public void AddBuff(BuffKey buffKey, Unit giver) => buffModule.AddBuff(buffKey, giver);
    public void ReduceBuff(BuffKey buffKey, int value) => buffModule.ReduceBuff(buffKey, value);
    public void RemoveBuff(BuffKey buffKey) => buffModule.RemoveBuff(buffKey);
}
