using UnityEngine;
using System.Collections.Generic;
using NUnit.Framework.Constraints;

public class BuffModule
{
    private Unit owner;
    private BuffInstance[] buffInstances = new BuffInstance[(int)BuffKey.Max];

    // 상태 이상
    private StatusEffect statusEffects = StatusEffect.None;
    private Dictionary<StatusEffect, int> statusCounters = new();

    public void Reset()
    {
        for (int i = 0; i < buffInstances.Length; i++)
        {
            if (buffInstances[i] == null)
                continue;

            CombatMgr.PushBuffInstance(buffInstances[i]);
            buffInstances[i] = null;
        }

        buffInstances = new BuffInstance[(int)BuffKey.Max];
        statusEffects = StatusEffect.None;
        statusCounters.Clear();
    }

    public void Init(Unit owner)
    {
        this.owner = owner;
    }

    public void UpdateBuff(float deltaTime)
    {
        for (int i = 0; i < buffInstances.Length; i++)
        {
            BuffInstance inst = buffInstances[i];
            if (inst == null)
                continue;

            inst.OnUpdate(deltaTime);
        }
    }


    public bool CanMove => !HasStatusEffect(StatusEffect.CanNotMove);
    public bool CanAttack => !HasStatusEffect(StatusEffect.CanNotAttack);
    public BuffInstance GetBuffInstance(BuffKey buffKey) => buffInstances[(int)buffKey];
    public List<BuffInstance> GetActiveBuffInstances()
    {
        List<BuffInstance> activeBuffs = new();
        for (int i = 0; i < buffInstances.Length; i++)
        {
            if (buffInstances[i] != null && buffInstances[i].stack > 0)
                activeBuffs.Add(buffInstances[i]);
        }

        return activeBuffs;
    }

    private bool HasStatusEffect(StatusEffect effect) => (statusEffects & effect) != 0;
    public bool HasBuff(BuffKey buffKey) => buffInstances[(int)buffKey] != null;

    public void AddBuff(BuffKey buffKey, Unit giver)
    {
        BuffHolder holder = BuffHolder.GetBuffHolder(buffKey);
        if (holder == null)
            return;

        BuffInstance inst = GetBuffInstance(buffKey);
        if (inst == null)
        {
            inst = CombatMgr.PopBuffInstance();
            inst.Init(giver, owner, holder);
            buffInstances[(int)buffKey] = inst;
            AddStatusEffect(inst.GetStatusEffect());
        }

        inst.AddInputer(holder);
    }

    public void ReduceBuff(BuffKey buffKey, int value)
    {
        BuffInstance inst = GetBuffInstance(buffKey);
        if (inst == null)
            return;

        if (inst.stack > value)
            inst.RemoveStack(value);
        else
            RemoveBuff(buffKey);
    }

    public void RemoveBuff(BuffKey buffKey)
    {
        BuffInstance inst = GetBuffInstance(buffKey);
        if (inst == null)
            return;

        RemoveStatusEffect(inst.GetStatusEffect());
        CombatMgr.PushBuffInstance(inst);
        buffInstances[(int)buffKey] = null;
    }

    private void AddStatusEffect(StatusEffect effect)
    {
        if (effect == StatusEffect.None)
            return;

        if (!statusCounters.ContainsKey(effect))
            statusCounters[effect] = 0;

        statusCounters[effect]++;
        statusEffects |= effect;

        // 공격중에 상태 이상에 걸릴 경우 해제할 것
        if (owner.IsAttacking && HasStatusEffect(StatusEffect.CanNotMove))
        {
            owner.SetAttacking(false);
        }
    }

    private void RemoveStatusEffect(StatusEffect effect)
    {
        if (effect == StatusEffect.None || !statusCounters.ContainsKey(effect))
            return;

        statusCounters[effect]--;
        if (statusCounters[effect] <= 0)
        {
            statusEffects &= ~effect;
        }
    }
}
