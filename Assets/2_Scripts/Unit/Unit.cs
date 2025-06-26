using UnityEngine;
using System;
using System.Collections.Generic;

public class Unit : MonoBehaviour
{
    private StatModule statModule = new();
    private BehaviourModule behaviourModule = new();
    private CombatModule combatModule = new();
    private PlayerSaveData playerSaveData = new();

    public void Init()
    {
        InitStatModule();
        SetHp();
        playerSaveData.Init(this);
    }

    // StatModule
    public float MaxHp => statModule.MaxHp;
    private void InitStatModule() => statModule.Init(playerSaveData.level);

    // CombatModule
    public float CurHp => combatModule.CurHp;
    public event Action<float> OnHpChanged
    {
        add => combatModule.OnHpChanged += value;
        remove => combatModule.OnHpChanged -= value;
    }

    public void SetHp() => combatModule.SetHp(statModule);
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
