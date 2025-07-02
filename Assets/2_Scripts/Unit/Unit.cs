using UnityEngine;
using System;
using System.Collections.Generic;

public class Unit : MonoBehaviour
{
    [SerializeField] private Animator animator;
    private StatModule statModule;
    private BehaviourModule behaviourModule;
    private PlayerSaveData playerSaveData = new();
    private CombatModule combatModule;
    private int unitID;
    private bool isPlayer;

    public int UnitID => unitID;

    public void Reset()
    {
        combatModule.Reset();
        transform.position = Vector3.zero;
        gameObject.SetActive(false);
    }

    public void Init(int unitID, Vector3 position)
    {
        this.unitID = unitID;
        isPlayer = unitID < 1000;
        InitModule();
        playerSaveData.Init(this);
        transform.position = position;
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

        statModule.Init(playerSaveData.level);
        combatModule.Init(this);
        behaviourModule.Init(this);
    }

    private void Update()
    {
        behaviourModule?.Update();
    }

    // 애니메이션
    public void PlayAnimation(string name) => animator.Play(name);
    public void SetTrigger(string name) => animator.SetTrigger(name);

    // StatModule
    public float MaxHp => statModule.MaxHp;
    public float MoveSpd => statModule.MoveSpd;

    // BehaviourModule
    public void OnAttackAnimationEnd() => behaviourModule.OnAttackAnimationEnd();

    // CombatModule
    public bool IsDead => combatModule.IsDead;
    public float CurHp => combatModule.CurHp;
    public event Action<float> OnHpChanged
    {
        add => combatModule.OnHpChanged += value;
        remove => combatModule.OnHpChanged -= value;
    }

    public void UpdateHp() => combatModule.UpdateHp();
    public void TakeDamage(float damage) => combatModule.TakeDamage(damage);

    // PlayerSaveData
    public int Level => playerSaveData.level;
    public float CurExp => playerSaveData.exp;
    public float MaxExp => playerSaveData.GetRequiredExp(playerSaveData.level);
    public int Gold => playerSaveData.gold;
    public List<SkillKey> SkillKeys => playerSaveData.SkillKeys;

    public event Action<List<SkillKey>> OnSkillChanged
    {
        add => playerSaveData.OnSkillChanged += value;
        remove => playerSaveData.OnSkillChanged -= value;
    }

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

    public void AddSkill(SkillKey skillKey) => playerSaveData.AddSkill(skillKey);
    public void RemoveSkill(SkillKey skillKey) => playerSaveData.RemoveSkill(skillKey);
    public void AddGold(int amount) => playerSaveData.AddGold(amount);
    public int AddExp(float amount) => playerSaveData.AddExp(amount);
}
