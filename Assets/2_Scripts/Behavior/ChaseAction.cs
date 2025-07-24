using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using Google.GData.Extensions;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Chase", story: "[Self] Chase To [Target]", category: "Action", id: "7c931b370f1d561b39c6dd7a7e8a4a13")]
public partial class ChaseAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<Unit> Target;
    private Unit selfUnit;
    private Unit targetUnit;
    private Transform selfTransform;
    private Transform targetTransform;
    private const float ROTATION_SPEED = 5f;

    protected override Status OnStart()
    {
        if (Self.Value == null || Target.Value == null)
        {
            return Status.Failure;
        }

        return Initialize();
    }

    protected override Status OnUpdate()
    {
        if (selfUnit == null || !selfUnit.CanMove || targetUnit == null)
        {
            return Status.Failure;
        }

        if (targetUnit == null || targetUnit.IsDead)
        {
            return Status.Failure;
        }

        Vector3 direction = GetDirection();
        selfTransform.position += direction * selfUnit.MoveSpeed * Time.deltaTime;
        if (direction != Vector3.zero)
            selfTransform.rotation = GetRotation(selfTransform.rotation, direction);
        return Status.Success;
    }

    private Status Initialize()
    {
        selfUnit = Self.Value.GetComponent<Unit>();
        selfTransform = selfUnit.transform;
        targetUnit = Target.Value;
        targetTransform = Target.Value.transform;

        return Status.Running;
    }

    private Vector3 GetDirection()
    {
        Vector3 direction = (targetTransform.position - selfTransform.position).normalized;
        direction.y = 0f;
        return direction;
    }

    private Quaternion GetRotation(Quaternion rotation, Vector3 direction)
    {
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        return Quaternion.Slerp(rotation, targetRotation, ROTATION_SPEED * Time.deltaTime);
    }
}

