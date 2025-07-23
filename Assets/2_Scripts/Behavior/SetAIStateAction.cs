using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Set Monster State", story: "Set [Self] AIState value to [AIState]", category: "Action", id: "505d1ba9a14ae5a71c4c4a731fe69d32")]
public partial class SetAIStateAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<AIState> AIState;

    protected override Status OnStart()
    {
        Unit unit = Self.Value.GetComponent<Unit>();
        unit.SetAIState(AIState.Value);
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}

