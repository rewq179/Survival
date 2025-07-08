using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Video;

/// <summary>
/// 스킬 효과 인터페이스
/// </summary>
public interface ISkillEffect
{
    void OnInitialize(SkillLauncher launcher);
    void OnUpdate(float deltaTime);
    void OnHit(Unit target);
    void OnDestroy();
}

/// <summary>
/// 발사체 이동 효과
/// </summary>
public class ProjectileMovementEffect : ISkillEffect
{
    private float damage;
    private float moveSpeed;
    private float maxRange;
    private Vector3 startPosition;
    private Vector3 direction;
    private bool isHit;
    private SkillLauncher launcher;

    public ProjectileMovementEffect(SkillData skillData, int index)
    {
        damage = skillData.skillElements[index].damage;
        IndicatorElement element = skillData.indicatorElements[index];
        moveSpeed = element.moveSpeed;
        maxRange = element.length;
    }

    public void OnInitialize(SkillLauncher launcher)
    {
        this.launcher = launcher;
        startPosition = launcher.Position;
        direction = launcher.transform.forward;
    }

    public void OnUpdate(float deltaTime)
    {
        if (isHit)
            return;

        float moveDistance = moveSpeed * deltaTime;
        launcher.transform.position += direction * moveDistance;

        // 최대 거리 체크
        float currentDistance = (launcher.Position - startPosition).sqrMagnitude;
        if (currentDistance >= maxRange * maxRange)
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

        target.TakeDamage(damage);
        isHit = true;
        launcher.transform.position = target.transform.position;
        launcher.Deactivate();
    }

    public void OnDestroy() { }
}

/// <summary>
/// 범위 데미지 효과
/// </summary>
public class AOEDamageEffect : ISkillEffect
{
    protected SkillLauncher launcher;
    protected SkillIndicatorType type;
    protected float radius;
    protected float maxDistance;
    protected float length;
    protected float width;
    protected float angle;
    protected float damage;

    public AOEDamageEffect(SkillData skillData, int index)
    {
        damage = skillData.skillElements[index].damage;

        IndicatorElement element = skillData.indicatorElements[index];
        type = element.type;
        radius = element.radius;
        length = element.length;
        width = element.width;
        angle = element.angle;

        maxDistance = type switch
        {
            SkillIndicatorType.Line => length * length,
            SkillIndicatorType.Sector => radius * radius,
            SkillIndicatorType.Circle => radius * radius,
            _ => 0f,
        };
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
public class PeriodicDamageEffect : AOEDamageEffect
{
    private float interval;
    private float lastDamageTime;

    public PeriodicDamageEffect(SkillData skillData, int index) : base(skillData, index)
    {
        interval = skillData.skillElements[index].interval;
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
public class InstantAttackEffect : ISkillEffect
{
    private float damage;
    private Unit target;
    private SkillLauncher launcher;

    public InstantAttackEffect(SkillData skillData, int index, Unit target)
    {
        damage = skillData.skillElements[index].damage;
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
