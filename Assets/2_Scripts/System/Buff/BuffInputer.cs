using UnityEngine;
using UnityEngine.UIElements;
using static BuffInputerBuilder;

public class BuffInputerBuilder
{
    public enum InputerType
    {
        Stack,
        MaxStack,
        Duration,
        Tick,
        Dmg,
        Max,
    }

    public enum StackType
    {
        Add,
        Set,
        FixOne,
    }

    public enum MaxStackType
    {
        Set,
        Infinity,
        FixOne,
    }

    public enum DurationType
    {
        Set,
    }

    public enum TickType
    {
        Set,
    }

    public enum DmgType
    {
        Set,
    }

    private static BuffInputerBuilder instance = new BuffInputerBuilder();
    private static BuffStack_Add stackAdd = new BuffStack_Add();
    private static BuffStack_Set stackSet = new BuffStack_Set();
    private static BuffStack_FixOne stackFixOne = new BuffStack_FixOne();
    private static BuffMaxStack_Set maxStackSet = new BuffMaxStack_Set();
    private static BuffMaxStack_Infinity maxStackInfinity = new BuffMaxStack_Infinity();
    private static BuffMaxStack_FixOne maxStackFixOne = new BuffMaxStack_FixOne();
    private static BuffDuration_Set durationSet = new BuffDuration_Set();
    private static BuffTick_Set tickSet = new BuffTick_Set();
    private static BuffDmg_Set dmgSet = new BuffDmg_Set();
    private BuffProcessor[] processors = new BuffProcessor[(int)InputerType.Max];

    public static BuffInputerBuilder GetInstance()
    {
        instance.Reset();
        return instance;
    }

    private void Reset()
    {
        for (int i = 0; i < processors.Length; i++)
        {
            processors[i] = null;
        }
    }

    public BuffInputerBuilder SetStack(StackType type)
    {
        processors[(int)InputerType.Stack] = type switch
        {
            StackType.Add => stackAdd,
            StackType.Set => stackSet,
            StackType.FixOne => stackFixOne,
            _ => null,
        };
        return this;
    }

    public BuffInputerBuilder SetMaxStack(MaxStackType type)
    {
        processors[(int)InputerType.MaxStack] = type switch
        {
            MaxStackType.Set => maxStackSet,
            MaxStackType.Infinity => maxStackInfinity,
            MaxStackType.FixOne => maxStackFixOne,
            _ => null,
        };
        return this;
    }

    public BuffInputerBuilder SetDuration(DurationType type)
    {
        processors[(int)InputerType.Duration] = type switch
        {
            DurationType.Set => durationSet,
            _ => null,
        };
        return this;
    }

    public BuffInputerBuilder SetTick(TickType type)
    {
        processors[(int)InputerType.Tick] = type switch
        {
            TickType.Set => tickSet,
            _ => null,
        };
        return this;
    }

    public BuffInputerBuilder SetDmg(DmgType type)
    {
        processors[(int)InputerType.Dmg] = type switch
        {
            DmgType.Set => dmgSet,
            _ => null,
        };
        return this;
    }

    public BuffInputer Build()
    {
        return new BuffInputer(processors);
    }

    public abstract class BuffProcessor
    {
        public virtual void SetStack(BuffInstance inst, int stack) { }
        public virtual void SetMaxStack(BuffInstance inst, int maxStack) { }
        public virtual void SetDuration(BuffInstance inst, float duration) { }
        public virtual void SetTick(BuffInstance inst, float tick) { }
        public virtual void SetDmg(BuffInstance inst, float dmg) { }
    }

    public class BuffStack_Add : BuffProcessor
    {
        public override void SetStack(BuffInstance inst, int stack)
        {
            inst.AddStack(stack);
        }
    }

    public class BuffStack_Set : BuffProcessor
    {
        public override void SetStack(BuffInstance inst, int stack)
        {
            inst.SetStack(stack);
        }
    }

    public class BuffStack_FixOne : BuffStack_Set
    {
        public override void SetStack(BuffInstance inst, int stack)
        {
            base.SetStack(inst, 1);
        }
    }

    public class BuffMaxStack_Set : BuffProcessor
    {
        public override void SetMaxStack(BuffInstance inst, int maxStack)
        {
            inst.SetMaxStack(maxStack);
        }
    }

    public class BuffMaxStack_Infinity : BuffProcessor
    {
        public override void SetMaxStack(BuffInstance inst, int maxStack)
        {
            inst.SetMaxStack(-1);
        }
    }

    public class BuffMaxStack_FixOne : BuffProcessor
    {
        public override void SetMaxStack(BuffInstance inst, int maxStack)
        {
            inst.SetMaxStack(1);
        }
    }

    public class BuffDuration_Set : BuffProcessor
    {
        public override void SetDuration(BuffInstance inst, float duration)
        {
            inst.SetDuration(duration);
        }
    }

    public class BuffTick_Set : BuffProcessor
    {
        public override void SetTick(BuffInstance inst, float tick)
        {
            inst.SetTick(tick);
        }
    }

    public class BuffDmg_Set : BuffProcessor
    {
        public override void SetDmg(BuffInstance inst, float dmg)
        {
            inst.SetDmg(dmg);
        }
    }
}

public class BuffInputer
{
    public readonly BuffProcessor stack;
    public readonly BuffProcessor maxStack;
    public readonly BuffProcessor duration;
    public readonly BuffProcessor tick;
    public readonly BuffProcessor dmg;

    public BuffInputer(BuffProcessor[] processors) : this(processors[0], processors[1], processors[2], processors[3], processors[4]) { }
    public BuffInputer(BuffProcessor stack, BuffProcessor maxStack, BuffProcessor duration, BuffProcessor tick, BuffProcessor dmg)
    {
        this.stack = stack;
        this.maxStack = maxStack;
        this.duration = duration;
        this.tick = tick;
        this.dmg = dmg;
    }

    public void Init(BuffInstance inst, BuffHolder holder)
    {
        maxStack.SetMaxStack(inst, holder.maxStack);
        stack.SetStack(inst, holder.stack);
        duration?.SetDuration(inst, holder.duration);
        tick?.SetTick(inst, holder.tick);
        dmg?.SetDmg(inst, holder.dmg);

        inst.durationTime = 0;
        inst.tickTime = 0;
    }
}
