using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Video;

/// <summary>
/// 스킬 효과 인터페이스
/// </summary>
public interface ISkillComponent
{
    void OnInitialize(SkillLauncher launcher);
    void OnUpdate(float deltaTime);
    void OnHit(Unit target);
    void OnDestroy();
}

/// <summary>
/// 투사체 효과
/// </summary>
public class ProjectileComponent : ISkillComponent
{
    private SkillLauncher launcher;
    private float damage;
    private float moveSpeed;
    private float maxLength;
    private int richocet;
    private int piercing;
    private Vector3 startPos;
    private Vector3 direction;
    private bool isHit;
    private HashSet<int> hittedUnitIDs = new();

    public ProjectileComponent(InstanceValue inst)
    {
        damage = inst.damageFinal;
        moveSpeed = inst.moveSpeedFinal;
        richocet = inst.ricochetFinal.GetInt();
        piercing = inst.piercingFinal.GetInt();
        maxLength = GameValue.PROJECTILE_MAX_LENGTH_POW;
        hittedUnitIDs.Clear();
    }

    public void OnInitialize(SkillLauncher launcher)
    {
        this.launcher = launcher;
        startPos = launcher.Position;
        direction = launcher.transform.forward;
        hittedUnitIDs.Add(launcher.Caster.UniqueID);
    }

    public void OnUpdate(float deltaTime)
    {
        if (isHit)
            return;

        float moveDistance = moveSpeed * deltaTime;
        launcher.transform.position += direction * moveDistance;

        // 최대 거리 체크
        float currentDistance = (launcher.Position - startPos).sqrMagnitude;
        if (currentDistance >= maxLength)
        {
            launcher.Deactivate();
            return;
        }

        // 충돌 체크
        if (Physics.Raycast(launcher.Position, direction, out RaycastHit hit, moveDistance, GameValue.UNIT_LAYERS))
        {
            OnHit(hit.collider.GetComponent<Unit>());
        }
    }

    public void OnHit(Unit target)
    {
        if (target == null)
            return;

        if (hittedUnitIDs.Contains(target.UniqueID))
            return;

        hittedUnitIDs.Add(target.UniqueID);
        target.TakeDamage(damage);
        isHit = true;

        if (richocet > 0)
        {
            Unit nextTarget = FindRicochetTarget(launcher.Position, 8f);
            if (nextTarget != null)
            {
                richocet--;
                isHit = false;

                startPos = launcher.Position;
                direction = (nextTarget.transform.position - launcher.Position).normalized;
                launcher.SetTransform(startPos, direction);
                return;
            }
        }

        if (piercing > 0)
        {
            isHit = false;
            piercing--;
            return;
        }

        launcher.Deactivate();
    }

    /// <summary>
    /// 도탄 대상(가장 가까운 유닛) 찾기
    /// </summary>
    private Unit FindRicochetTarget(Vector3 position, float radius)
    {
        Collider[] colliders = Physics.OverlapSphere(position, radius, GameValue.UNIT_LAYERS);

        Unit target = null;
        float maxDist = float.MaxValue;

        foreach (Collider col in colliders)
        {
            Unit unit = col.GetComponent<Unit>();
            if (unit == null || hittedUnitIDs.Contains(unit.UniqueID))
                continue;

            float dist = (position - unit.transform.position).sqrMagnitude;
            if (dist < maxDist)
            {
                maxDist = dist;
                target = unit;
            }
        }

        return target;
    }

    public void OnDestroy() { }
}

/// <summary>
/// 범위 효과
/// </summary>
public class AOEComponent : ISkillComponent
{
    protected SkillLauncher launcher;
    protected SkillIndicatorType type;
    protected float damage;
    protected float angle;
    protected float radius;
    protected float maxDistance;

    public AOEComponent(InstanceValue inst)
    {
        damage = inst.damageFinal;
        radius = inst.radiusFinal;
        angle = inst.angle;
        maxDistance = radius * radius;
        type = angle == 360f ? SkillIndicatorType.Circle : SkillIndicatorType.Sector;
    }

    public void OnInitialize(SkillLauncher launcher)
    {
        this.launcher = launcher;

        List<Unit> targets = GetHitTargets(launcher.Position, launcher.Direction, launcher.IsAffectCaster);
        foreach (Unit target in targets)
        {
            OnHit(target);
        }
    }

    protected List<Unit> GetHitTargets(Vector3 position, Vector3 direction, bool isAffectCaster)
    {
        Collider[] colliders = Physics.OverlapSphere(position, radius, GameValue.UNIT_LAYERS);

        List<Unit> hitTargets = new();
        foreach (Collider col in colliders)
        {
            Unit target = col.GetComponent<Unit>();
            if (target == null || (!isAffectCaster && target == launcher.Caster))
                continue;

            if (IsTargetInSkillArea(position, direction, target.transform.position))
            {
                hitTargets.Add(target);
            }
        }

        return hitTargets;
    }

    protected bool IsTargetInSkillArea(Vector3 position, Vector3 direction, Vector3 targetPosition)
    {
        float sqrDistance = (targetPosition - position).sqrMagnitude;

        return type switch
        {
            SkillIndicatorType.Line => false,
            SkillIndicatorType.Sector => IsTargetInSectorArea(position, direction, targetPosition, sqrDistance),
            SkillIndicatorType.Circle => IsTargetInCircleArea(sqrDistance),
            _ => true,
        };
    }

    private bool IsTargetInSectorArea(Vector3 position, Vector3 direction, Vector3 targetPosition, float sqrDistance)
    {
        if (sqrDistance > maxDistance)
            return false;

        float angle = Vector3.Angle(direction, (targetPosition - position).normalized);
        return angle <= this.angle * 0.5f;
    }

    private bool IsTargetInCircleArea(float sqrDistance)
    {
        return sqrDistance <= maxDistance;
    }

    public virtual void OnUpdate(float deltaTime) { }
    public virtual void OnHit(Unit target) { target.TakeDamage(damage); }
    public void OnDestroy() { }
}

/// <summary>
/// 주기적 데미지 효과
/// </summary>
public class PeriodicAOEComponent : AOEComponent
{
    private float interval;
    private float lastDamageTime;

    public PeriodicAOEComponent(InstanceValue inst) : base(inst)
    {
        interval = inst.damageTickFinal;
    }

    public override void OnUpdate(float deltaTime)
    {
        if (Time.time - lastDamageTime >= interval)
        {
            List<Unit> targets = GetHitTargets(launcher.Position, launcher.Direction, launcher.IsAffectCaster);
            foreach (Unit target in targets)
            {
                OnHit(target);
            }

            lastDamageTime = Time.time;
        }
    }
}

/// <summary>
/// 즉시 공격 효과
/// </summary>
public class InstantComponent : ISkillComponent
{
    private float damage;
    private Unit target;
    private SkillLauncher launcher;

    public InstantComponent(InstanceValue inst, Unit target)
    {
        damage = inst.damageFinal;
        this.target = target;
    }

    public void OnInitialize(SkillLauncher launcher)
    {
        this.launcher = launcher;
        OnHit(target);
    }

    public void OnUpdate(float deltaTime) { }
    public void OnHit(Unit target)
    {
        target.TakeDamage(damage);
        launcher.Deactivate();
    }

    public void OnDestroy() { }
}
