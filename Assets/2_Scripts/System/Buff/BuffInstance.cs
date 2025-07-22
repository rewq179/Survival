using UnityEngine;

public class BuffInstance
{
    public Unit giver;
    public Unit receiver;
    public BuffInputer inputer;
    private BuffHolder holder;
    public BuffKey buffKey;

    public int stack;
    public int maxStack;
    public float duration;
    public float durationTime;
    public float tick;
    public float tickTime;
    public float dmg;

    public bool IsInfinityStack => maxStack == -1;
    public bool IsMaxStack => stack >= maxStack;

    public void Reset()
    {
        giver = null;
        receiver = null;
        inputer = null;
        holder = null;
        buffKey = BuffKey.Max;
        stack = 0;
        maxStack = 0;
        duration = 0;
        durationTime = 0;
        tick = 0;
        tickTime = 0;
    }

    public void Init(Unit giver, Unit receiver, BuffHolder holder)
    {
        this.giver = giver;
        this.receiver = receiver;
        InitHolder(holder);
    }

    private void InitHolder(BuffHolder holder)
    {
        this.holder = holder;
        buffKey = holder.BuffKey;
        inputer = holder.GetBuffInputer();
    }

    public void AddInputer(BuffHolder holder)
    {
        inputer.Init(this, holder);
    }

    public StatusEffect GetStatusEffect() => holder?.GetStatusEffect() ?? StatusEffect.None;
    public void AddStack(int amount) => SetStack(stack + amount);
    public void RemoveStack(int amount) => SetStack(stack - amount);
    public void SetStack(int amount)
    {
        stack = amount;

        if (!IsInfinityStack && IsMaxStack)
            stack = maxStack;
    }

    public void SetMaxStack(int amount)
    {
        maxStack = amount;
    }

    public void SetDuration(float duration)
    {
        this.duration = duration;
    }

    public void SetTick(float tick)
    {
        this.tick = tick;
    }

    public void SetDmg(float dmg)
    {
        this.dmg = dmg;
    }

    public void OnUpdate(float deltaTime)
    {
        durationTime += deltaTime;
        tickTime += deltaTime;

        if (tickTime >= tick)
        {
            tickTime -= tick;
            holder.OnTick(this);
        }

        if (durationTime >= duration)
        {
            holder.OnEnd(this);
            receiver.RemoveBuff(buffKey);
        }
    }
}
