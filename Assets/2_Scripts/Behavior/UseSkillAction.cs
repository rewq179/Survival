using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[BlackboardEnum]
public enum RangeType
{
    None,
    Melee,
    Ranged,
}

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "UseSkill", story: "[Self] use [RangeType] skill to [Target]", category: "Action", id: "c56f7fee65a28cf35fa983cbd5b97c2e")]
public partial class UseSkillAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<RangeType> RangeType;

    protected override Status OnStart()
    {
        if (Self.Value == null)
        {
            return Status.Failure;
        }

        Unit selfUnit = Self.Value.GetComponent<Unit>();
        Unit targetUnit = Target.Value.GetComponent<Unit>();

        selfUnit.UseRandomSkill(targetUnit, RangeType.Value);
        return Status.Running;
    }
}

