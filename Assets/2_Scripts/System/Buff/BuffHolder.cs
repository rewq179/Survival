using UnityEngine;

public abstract class BuffHolder
{
    public static BuffHolder GetBuffHolder(BuffKey key)
    {
        BuffData data = DataMgr.GetBuffData(key);
        if (data == null)
        {
            Debug.LogError($"BuffData is null: {key}");
            return null;
        }

        BuffHolder holder = key switch
        {
            BuffKey.Burn => new Buff_Burn(data.element),
            BuffKey.Freeze => new Buff_Freeze(data.element),
            BuffKey.Stun => new Buff_Stun(data.element),
            _ => null,
        };

        if (holder == null)
        {
            Debug.LogError($"BuffHolder is null: {key}");
        }


        return holder;
    }

    /// <summary>
    /// Stack : N
    /// MaxStack : Infinity
    /// Duration : N
    /// Tick : N
    /// Dmg : N
    /// </summary>
    protected static BuffInputer fullDmgInfinityStack;

    /// <summary>
    /// Stack : N
    /// MaxStack : N
    /// Duration : N
    /// Tick : N
    /// Dmg : N
    /// </summary>
    protected static BuffInputer fullDmg;

    /// <summary>
    /// Stack : N
    /// MaxStack : Infinity
    /// Duration : N
    /// Tick : N
    /// </summary>
    protected static BuffInputer fullInfinity;

    /// <summary>
    /// Stack : N
    /// MaxStack : N
    /// Duration : N
    /// Tick : N
    /// </summary>
    protected static BuffInputer full;

    /// <summary>
    /// Stack : N
    /// MaxStack : Infinity
    /// Duration : N
    /// </summary>
    protected static BuffInputer stackInfinityDuration;

    /// <summary>
    /// Stack : N
    /// MaxStack : N
    /// Duration : N
    /// </summary>
    protected static BuffInputer stackDuration;

    /// <summary>
    /// Stack : N
    /// MaxStack : Infinity
    /// </summary>
    protected static BuffInputer onlyStackInfinity;

    /// <summary>
    /// Stack : N
    /// MaxStack : N
    /// </summary>
    protected static BuffInputer onlyStack;

    public static void InitInputer()
    {
        fullDmgInfinityStack = BuffInputerBuilder.GetInstance()
            .SetStack(BuffInputerBuilder.StackType.Add)
            .SetMaxStack(BuffInputerBuilder.MaxStackType.Infinity)
            .SetDuration(BuffInputerBuilder.DurationType.Set)
            .SetTick(BuffInputerBuilder.TickType.Set)
            .SetDmg(BuffInputerBuilder.DmgType.Set)
            .Build();

        fullDmg = BuffInputerBuilder.GetInstance()
            .SetStack(BuffInputerBuilder.StackType.Add)
            .SetMaxStack(BuffInputerBuilder.MaxStackType.Set)
            .SetDuration(BuffInputerBuilder.DurationType.Set)
            .SetTick(BuffInputerBuilder.TickType.Set)
            .SetDmg(BuffInputerBuilder.DmgType.Set)
            .Build();

        fullInfinity = BuffInputerBuilder.GetInstance()
            .SetStack(BuffInputerBuilder.StackType.Add)
            .SetMaxStack(BuffInputerBuilder.MaxStackType.Set)
            .SetDuration(BuffInputerBuilder.DurationType.Set)
            .SetTick(BuffInputerBuilder.TickType.Set)
            .Build();

        full = BuffInputerBuilder.GetInstance()
            .SetStack(BuffInputerBuilder.StackType.Add)
            .SetMaxStack(BuffInputerBuilder.MaxStackType.Set)
            .SetDuration(BuffInputerBuilder.DurationType.Set)
            .SetTick(BuffInputerBuilder.TickType.Set)
            .Build();

        stackDuration = BuffInputerBuilder.GetInstance()
            .SetStack(BuffInputerBuilder.StackType.Set)
            .SetMaxStack(BuffInputerBuilder.MaxStackType.Set)
            .SetDuration(BuffInputerBuilder.DurationType.Set)
            .Build();

        stackInfinityDuration = BuffInputerBuilder.GetInstance()
            .SetStack(BuffInputerBuilder.StackType.Add)
            .SetMaxStack(BuffInputerBuilder.MaxStackType.Infinity)
            .SetDuration(BuffInputerBuilder.DurationType.Set)
            .Build();

        onlyStack = BuffInputerBuilder.GetInstance()
            .SetStack(BuffInputerBuilder.StackType.Add)
            .SetMaxStack(BuffInputerBuilder.MaxStackType.Set)
            .Build();
    }

    public abstract BuffKey BuffKey { get; }
    public int stack;
    public int maxStack;
    public float duration;
    public float tick;
    public float dmg;
    public abstract BuffInputer GetBuffInputer();

    public BuffHolder(BuffElement element)
    {
        this.stack = element.stack;
        this.maxStack = element.maxStack;
        this.duration = element.duration;
        this.tick = element.tick;
        this.dmg = element.dmg;
    }

    public virtual StatusEffect GetStatusEffect() => StatusEffect.None;
    public virtual void OnTick(BuffInstance inst) { }
    public virtual void OnEnd(BuffInstance inst) { }
}

public abstract class Buff_TickDamage : BuffHolder
{
    public Buff_TickDamage(BuffElement element) : base(element)
    {
    }

    public override BuffInputer GetBuffInputer() => fullDmgInfinityStack;
    public override void OnTick(BuffInstance inst)
    {
        Vector3 hitPoint = inst.receiver.transform.position;
        CombatMgr.ApplyDamageByBuff(inst.giver, inst.receiver, inst.dmg * inst.stack, hitPoint, BuffKey);
    }

}

public class Buff_Burn : Buff_TickDamage
{
    public override BuffKey BuffKey => BuffKey.Burn;
    public Buff_Burn(BuffElement element) : base(element)
    {

    }
}

public class Buff_Freeze : BuffHolder
{
    public override BuffKey BuffKey => BuffKey.Freeze;
    public Buff_Freeze(BuffElement element) : base(element) { }
    public override BuffInputer GetBuffInputer() => stackDuration;
    public override StatusEffect GetStatusEffect() => StatusEffect.Stunned;
}

public class Buff_Stun : BuffHolder
{
    public override BuffKey BuffKey => BuffKey.Stun;
    public Buff_Stun(BuffElement element) : base(element) { }
    public override BuffInputer GetBuffInputer() => stackDuration;
    public override StatusEffect GetStatusEffect() => StatusEffect.Stunned;
}
