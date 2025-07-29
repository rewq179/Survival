using UnityEngine;
using System;

public class SkillCollision : MonoBehaviour
{
    [Header("Collision")]
    [SerializeField] private Collider skillCollider;
    public event Action<Unit> OnHitTarget;

    private bool canUseCollision => skillCollider != null;
    public Collider Collider => skillCollider;

    public void Reset()
    {
        skillCollider.enabled = false;
        OnHitTarget = null; 
    }

    public void Init()
    {
        skillCollider.enabled = canUseCollision;
        skillCollider.isTrigger = true;
    }

    public void OnTriggerEnter(Collider other)
    {
        if (!canUseCollision)
            return;

        Unit target = other.GetComponent<Unit>();
        if (target != null)
        {
            OnHitTarget?.Invoke(target);
        }
    }
}
