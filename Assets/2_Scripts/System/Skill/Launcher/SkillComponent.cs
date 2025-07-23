using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public enum SkillComponentType
{
    Projectile,
    Boomerang,
    RotatingOrbs,
    InstantAOE,
    PeriodicAOE,
    InstantAttack,
    Beam,
    Linear,
    Leap,
    Gravity,
    Max,
}

public enum ComponentState
{
    NotStarted,
    Running,
    Completed
}

public abstract class SkillComponent
{
    public abstract SkillComponentType Type { get; }
    public ExecutionTiming timing;
    protected ComponentState state = ComponentState.NotStarted;
    public int order;

    protected SkillLauncher launcher;
    protected UnitType enemyType;

    public ComponentState State => state;
    public virtual SkillEffectController EffectController => null;
    public bool IsCompleted => state == ComponentState.Completed;

    public virtual void Reset()
    {
        state = ComponentState.NotStarted;
        launcher = null;
        order = 0;
        timing = ExecutionTiming.Instant;
        enemyType = UnitType.Monster;
    }

    public virtual void Init(SkillLauncher launcher, SkillHolder holder, Unit fixedTarget)
    {
        this.launcher = launcher;
        enemyType = launcher.Caster.UnitType == UnitType.Player ? UnitType.Monster : UnitType.Player;
        order = holder.order;
        timing = holder.timing;
    }

    public virtual void OnStart()
    {
        if (state != ComponentState.NotStarted)
            return;

        state = ComponentState.Running;
    }

    public virtual void OnUpdate(float deltaTime) { }
    protected virtual void OnEnd(bool forceEnd = false)
    {
        if (state == ComponentState.Completed)
            return;

        state = ComponentState.Completed;
        launcher.CheckDeactivate(forceEnd);
    }

    public virtual void OnHit(Unit target) { }
    public bool IsHittable(Unit target) => target != null && !target.IsDead && target.UnitType == enemyType;

    #region 타겟 Getter

    protected List<Unit> GetHitTargetsBySphere(Vector3 position, float radius)
    {
        Collider[] colliders = Physics.OverlapSphere(position, radius, GameValue.UNIT_LAYERS);

        List<Unit> targets = new();
        foreach (Collider col in colliders)
        {
            Unit target = col.GetComponent<Unit>();
            if (IsHittable(target))
                targets.Add(target);
        }

        return targets;
    }

    #endregion
}

#region 공격 컴포넌트

/// <summary> 공격 컴포넌트들의 기본 클래스 </summary>
public abstract class Attack_Component : SkillComponent
{
    protected SkillEffectController effectController;
    protected float damage;
    protected List<BuffKey> buffKeys;

    public override void Reset()
    {
        base.Reset();
        effectController = null;
        buffKeys = null;
    }

    public override void Init(SkillLauncher launcher, SkillHolder holder, Unit fixedTarget)
    {
        base.Init(launcher, holder, fixedTarget);
        damage = holder.damageFinal;
        buffKeys = holder.BuffKeys;

        // 공격 컴포넌트에만 파티클 할당
        effectController = GameMgr.Instance.skillMgr.PopSkillObject(launcher.SkillKey, launcher.transform);
        if (effectController == null)
            return;

        effectController.Init(launcher, OnHit, OnParticleFinished);
    }

    public override SkillEffectController EffectController => effectController;
    protected void OnParticleFinished()
    {
        launcher.SetParticleFinished(true);
        launcher.CheckDeactivate(false);
    }

    public override void OnStart()
    {
        base.OnStart();
        effectController?.Play();
    }

    protected override void OnEnd(bool forceEnd = false)
    {
        base.OnEnd(forceEnd);
        effectController?.StopMain();
    }

    protected void ApplyToTarget(Unit target)
    {
        Vector3 hitPoint = target.transform.position;
        CombatMgr.ApplyDamageBySkill(launcher.Caster, target, damage, hitPoint, launcher.SkillKey);

        if (buffKeys == null || target.IsDead)
            return;

        foreach (BuffKey buffKey in buffKeys)
        {
            target.AddBuff(buffKey, launcher.Caster);
        }
    }
}

/// <summary> 투사체 계열 컴포넌트들의 기본 클래스 </summary>
public abstract class Attack_ProjectileBaseComponent : Attack_Component
{
    protected float moveSpeed;
    protected float maxLength;
    protected Vector3 startPos;
    protected Vector3 direction;
    protected HashSet<int> hittedUnitIDs = new();

    public override void Reset()
    {
        base.Reset();
        hittedUnitIDs.Clear();
    }

    public override void Init(SkillLauncher launcher, SkillHolder holder, Unit fixedTarget)
    {
        base.Init(launcher, holder, fixedTarget);
        moveSpeed = holder.moveSpeedFinal;
        maxLength = GameValue.PROJECTILE_MAX_LENGTH_POW;
        startPos = launcher.Position;
        direction = launcher.transform.forward;
        hittedUnitIDs.Add(launcher.Caster.UniqueID);
    }

    public override void OnHit(Unit target)
    {
        if (!IsHittable(target))
            return;

        if (hittedUnitIDs.Contains(target.UniqueID))
            return;

        // 피해 적용
        hittedUnitIDs.Add(target.UniqueID);
        ApplyToTarget(target);
        effectController.PlayHit();

        OnProjectileHit(target);
    }

    protected abstract void OnProjectileHit(Unit target);
    protected void MoveProjectile(Vector3 moveDirection, float speed, float deltaTime)
    {
        float moveDistance = speed * deltaTime;
        launcher.transform.position += moveDirection * moveDistance;
    }

    protected bool HasReachedMaxDistance()
    {
        float currentDistance = (launcher.Position - startPos).sqrMagnitude;
        return currentDistance >= maxLength;
    }
}

/// <summary> 일반 투사체 컴포넌트 </summary>
public class Attack_ProjectileComponent : Attack_ProjectileBaseComponent
{
    public override SkillComponentType Type => SkillComponentType.Projectile;
    private int richocet;
    private int piercing;
    private const float RICOCET_RADIUS = 8f;

    public override void Reset()
    {
        base.Reset();
        richocet = 0;
        piercing = 0;
    }

    public override void Init(SkillLauncher launcher, SkillHolder holder, Unit fixedTarget)
    {
        base.Init(launcher, holder, fixedTarget);
        richocet = holder.ricochetFinal.GetInt();
        piercing = holder.piercingFinal.GetInt();
    }

    public override void OnUpdate(float deltaTime)
    {
        MoveProjectile(direction, moveSpeed, deltaTime);

        if (HasReachedMaxDistance())
        {
            OnEnd();
        }
    }

    protected override void OnProjectileHit(Unit target)
    {
        if (richocet > 0) // 도탄
        {
            Unit nextTarget = FindRicochetTarget(launcher.Position, RICOCET_RADIUS);
            if (nextTarget != null)
            {
                richocet--;

                startPos = launcher.Position;
                Vector3 targetPos = nextTarget.transform.position;
                targetPos.y = startPos.y;
                direction = (targetPos - startPos).normalized;
                launcher.SetTransform(startPos, direction);
                return;
            }
        }

        if (piercing > 0) // 관통
        {
            piercing--;
            return;
        }

        OnEnd();
    }

    /// <summary>
    /// 도탄 대상(가장 가까운 유닛) 찾기
    /// </summary>
    private Unit FindRicochetTarget(Vector3 position, float radius)
    {
        Unit target = null;
        float maxDist = float.MaxValue;

        Collider[] colliders = Physics.OverlapSphere(position, radius, GameValue.UNIT_LAYERS);
        foreach (Collider col in colliders)
        {
            Unit unit = col.GetComponent<Unit>();
            if (!IsHittable(unit) || hittedUnitIDs.Contains(unit.UniqueID))
                continue;

            Vector3 targetPos = unit.transform.position;
            targetPos.y = position.y;

            float dist = (position - targetPos).sqrMagnitude;
            if (dist < maxDist)
            {
                maxDist = dist;
                target = unit;
            }
        }

        return target;
    }
}

/// <summary> 부메랑 투사체 컴포넌트 </summary>
public class Attack_BoomerangComponent : Attack_ProjectileBaseComponent
{
    public override SkillComponentType Type => SkillComponentType.Boomerang;
    private float originalDamage;
    private float returnSpeed;
    private Vector3 returnDirection;
    private bool isReturning;

    // 상수
    private const float RETURN_SPEED_MULTIPLIER = 1.5f; // 돌아올 때 속도 증가 배율
    private const float DAMAGE_REDUCTION_PER_HIT = 0.15f; // 15%씩 감소
    private const float MIN_DAMAGE_RATIO = 0.1f; // 최소 10%까지

    public override void Reset()
    {
        base.Reset();
        isReturning = false;
    }

    public override void Init(SkillLauncher launcher, SkillHolder holder, Unit fixedTarget)
    {
        base.Init(launcher, holder, fixedTarget);
        originalDamage = damage;
        returnSpeed = moveSpeed * RETURN_SPEED_MULTIPLIER;
        returnDirection = -direction;
    }

    public override void OnUpdate(float deltaTime)
    {
        if (isReturning) // 돌아가는 중
        {
            MoveProjectile(returnDirection, returnSpeed, deltaTime);

            float distanceSqr = (launcher.Position - startPos).sqrMagnitude;
            if (distanceSqr <= 0.1f)
            {
                OnEnd();
            }
        }

        else // 앞으로 나가는 중
        {
            MoveProjectile(direction, moveSpeed, deltaTime);

            if (HasReachedMaxDistance())
            {
                damage = originalDamage;
                isReturning = true;
            }
        }
    }

    protected override void OnProjectileHit(Unit target)
    {
        // 관통 횟수 별 데미지 감소
        float multiplier = 1f - hittedUnitIDs.Count * DAMAGE_REDUCTION_PER_HIT;
        damage = Mathf.Max(originalDamage * multiplier, MIN_DAMAGE_RATIO);
    }
}

/// <summary> 회전하는 구체들로 구성된 이동 공격 컴포넌트 </summary>
public class Attack_RotatingOrbsComponent : Attack_ProjectileBaseComponent
{
    public override SkillComponentType Type => SkillComponentType.RotatingOrbs;
    private float rotationSpeed;
    private int orbCount;
    private float anglePerOrb;
    private float currentAngle;
    private List<SkillEffectController> orbEffects = new();

    // 구체들의 위치 계산용
    private List<Vector3> orbPositions = new();
    private const float CIRCLE_RADIUS = 2f;

    public override void Reset()
    {
        SkillKey skillKey = launcher.SkillKey;
        base.Reset();
        orbPositions.Clear();

        SkillMgr skillMgr = GameMgr.Instance.skillMgr;
        for (int i = orbEffects.Count - 1; i > 0; i--) // 0번째는 런처시 제거시 자동
        {
            skillMgr.PushSkillObject(skillKey, orbEffects[i]);
        }

        orbEffects.Clear();
        currentAngle = 0f;
    }

    public override void Init(SkillLauncher launcher, SkillHolder holder, Unit fixedTarget)
    {
        base.Init(launcher, holder, fixedTarget);
        rotationSpeed = holder.rotationSpeedFinal;
        orbCount = holder.ShotFinal.GetInt();
        anglePerOrb = 360f / orbCount; // 360도를 구체 개수로 나누어 균등 배치
        currentAngle = 0f;
        
        orbEffects.Add(launcher.GetComponentInChildren<SkillEffectController>());
        CalculateOrbPositions();
        CreateOrbs();
    }

    private void CreateOrbs()
    {
        SkillMgr skillMgr = GameMgr.Instance.skillMgr;
        for (int i = 1; i < orbCount; i++)
        {
            SkillEffectController effect = skillMgr.PopSkillObject(launcher.SkillKey, launcher.transform);
            effect.Init(launcher, OnHit, OnParticleFinished);
            effect.SetPosition(orbPositions[i]);
            orbEffects.Add(effect);
        }
    }

    public override void OnUpdate(float deltaTime)
    {
        currentAngle += rotationSpeed * deltaTime;
        
        MoveProjectile(direction, moveSpeed, deltaTime);
        CalculateOrbPositions();
        UpdateOrbTransform();

        if (HasReachedMaxDistance())
        {
            OnEnd();
        }
    }
    
    private void CalculateOrbPositions()
    {
        orbPositions.Clear();
        Vector3 center = launcher.Position;

        for (int i = 0; i < orbCount; i++)
        {
            // 각 구체를 원의 경계에 균등하게 배치
            float angle = currentAngle + anglePerOrb * i;
            float radian = angle * Mathf.Deg2Rad;

            // 현재 반지름으로 구체 배치
            Vector3 orbPos = center + new Vector3(
                Mathf.Cos(radian) * CIRCLE_RADIUS,
                0f,
                Mathf.Sin(radian) * CIRCLE_RADIUS
            );
            orbPositions.Add(orbPos);
        }
    }

    private void UpdateOrbTransform()
    {
        for (int i = 0; i < orbEffects.Count && i < orbPositions.Count; i++)
        {
            orbEffects[i].transform.position = orbPositions[i];
        }

        Quaternion rotation = Quaternion.Euler(0f, currentAngle, 0f);
        launcher.transform.rotation = rotation;
    }

    protected override void OnProjectileHit(Unit target) { }
}

/// <summary> 범위 공격 컴포넌트 </summary>
public class Attack_AOEComponent : Attack_Component
{
    public override SkillComponentType Type => SkillComponentType.InstantAOE;
    protected SkillIndicatorType type;
    protected float angle;
    protected float radius;
    protected float maxDistance;

    protected virtual bool IsInstantComplete => true;
    public override void Init(SkillLauncher launcher, SkillHolder holder, Unit fixedTarget)
    {
        base.Init(launcher, holder, fixedTarget);
        damage = holder.damageFinal;
        radius = holder.radiusFinal;
        angle = holder.angle;
        maxDistance = radius * radius;
        type = angle == 360f ? SkillIndicatorType.Circle : SkillIndicatorType.Sector;
    }

    public override void OnStart()
    {
        base.OnStart();
        ExecuteAttack();
        if (IsInstantComplete)
        {
            OnEnd();
        }
    }

    protected void ExecuteAttack()
    {
        Vector3 position = launcher.Position;
        Vector3 direction = launcher.Direction;

        List<Unit> targets = GetHitTargetsBySphere(position, radius);
        foreach (Unit target in targets)
        {
            if (IsTargetInSkillArea(position, direction, target.transform.position))
            {
                OnHit(target);
            }
        }
    }

    protected bool IsTargetInSkillArea(Vector3 position, Vector3 direction, Vector3 targetPosition)
    {
        float sqrDistance = (targetPosition - position).sqrMagnitude;

        return type switch
        {
            SkillIndicatorType.Line => false,
            SkillIndicatorType.Sector => IsTargetInSectorArea(position, direction, targetPosition, sqrDistance),
            SkillIndicatorType.Circle => IsTargetInCircleArea(sqrDistance),
            SkillIndicatorType.Rectangle => IsTargetInRectangleArea(position, direction, targetPosition, sqrDistance),
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

    private bool IsTargetInRectangleArea(Vector3 position, Vector3 direction, Vector3 targetPosition, float sqrDistance)
    {
        return false; // TODO: 사각형 형태로 구현할 것
    }

    public override void OnHit(Unit target)
    {
        ApplyToTarget(target);
        effectController.PlayHit();
    }
}

/// <summary> 주기적 데미지 공격 컴포넌트 </summary>
public class Attack_PeriodicAOEComponent : Attack_AOEComponent
{
    public override SkillComponentType Type => SkillComponentType.PeriodicAOE;
    private float duration;
    private float time;
    private float tick;
    private float lastTickTime;

    protected override bool IsInstantComplete => false;
    public override void Reset()
    {
        base.Reset();
        time = 0f;
        lastTickTime = 0f;
    }

    public override void Init(SkillLauncher launcher, SkillHolder holder, Unit fixedTarget)
    {
        base.Init(launcher, holder, fixedTarget);
        tick = holder.damageTickFinal;
        duration = holder.durationFinal;
        lastTickTime = Time.time;
    }

    public override void OnUpdate(float deltaTime)
    {
        time += deltaTime;
        float currentTime = Time.time;
        if (currentTime - lastTickTime >= tick)
        {
            ExecuteAttack();
            lastTickTime = currentTime;
        }

        if (time >= duration)
        {
            OnEnd();
        }
    }
}

/// <summary> 즉시 공격 컴포넌트 </summary>
public class Attack_InstantComponent : Attack_Component
{
    public override SkillComponentType Type => SkillComponentType.InstantAttack;
    private Unit target;

    public override void Init(SkillLauncher launcher, SkillHolder holder, Unit fixedTarget)
    {
        base.Init(launcher, holder, fixedTarget);
        target = fixedTarget;
        damage = holder.damageFinal;
    }

    public override void OnStart()
    {
        base.OnStart();
        OnHit(target);
        OnEnd(true);
    }

    public override void OnHit(Unit target)
    {
        ApplyToTarget(target);
    }
}

/// <summary> 빔 공격 컴포넌트 </summary>
public class Attack_BeamComponent : Attack_Component
{
    public override SkillComponentType Type => SkillComponentType.Beam;
    private float length;
    private float tick;
    private float duration;

    private BeamParticle beamParticle;
    private SkillIndicator startIndicator;
    private SkillIndicator finalIndicator;
    private Vector3 startPos;
    private Vector3 targetPos;
    private Vector3 direction;

    private bool isIndicatorTime;
    private float time;
    private float tickTime;
    private float indicatorDuration;
    private float invIndicatorDuration;

    public override void Reset()
    {
        base.Reset();
        tickTime = 0f;
        time = 0f;
        beamParticle = null;
        startIndicator = null;
        finalIndicator = null;
    }

    public override void Init(SkillLauncher launcher, SkillHolder holder, Unit fixedTarget)
    {
        base.Init(launcher, holder, fixedTarget);
        damage = holder.damageFinal;
        length = GameValue.PROJECTILE_MAX_LENGTH;
        tick = holder.damageTickFinal;
        duration = holder.durationFinal;

        isIndicatorTime = true;
        targetPos = fixedTarget.transform.position;
        indicatorDuration = 0.4f;
        invIndicatorDuration = 1f / indicatorDuration;

        startPos = launcher.Position;
        direction = (targetPos - startPos).normalized;
    }

    public override void OnStart()
    {
        base.OnStart();

        SkillData data = DataMgr.GetSkillData(launcher.SkillKey);
        SkillElement element = data.skillElements[0];
        SkillMgr skillMgr = GameMgr.Instance.skillMgr;

        // 스킬 인디케이터
        startIndicator = skillMgr.CreateIndicator(element, false);
        startIndicator.DrawIndicator(startPos, targetPos);
        finalIndicator = skillMgr.CreateIndicator(element, false);
        finalIndicator.DrawIndicator(startPos, targetPos);
    }

    public override void OnUpdate(float deltaTime)
    {
        time += deltaTime;

        if (isIndicatorTime)
            UpdateIndicator();
        else
            UpdateSkill(deltaTime);
    }

    private void UpdateIndicator()
    {
        if (!isIndicatorTime)
            return;

        if (time < indicatorDuration)
        {
            float p = Mathf.Clamp01(time * invIndicatorDuration);
            startIndicator.UpdateIndicatorScale(p);
        }

        else
        {
            isIndicatorTime = false;
            time = 0f;
            GameMgr.Instance.skillMgr.RemoveIndicator(startIndicator);
            GameMgr.Instance.skillMgr.RemoveIndicator(finalIndicator);

            startPos = launcher.Caster.firePoint.position;
            targetPos.y = startPos.y;
            direction = (targetPos - startPos).normalized;
            launcher.SetTransform(startPos, direction);

            beamParticle = effectController.GetComponent<BeamParticle>();
            beamParticle.Init(direction, length);
        }
    }

    private void UpdateSkill(float deltaTime)
    {
        if (isIndicatorTime)
            return;

        beamParticle.UpdateBeam();

        if (time < duration)
        {
            tickTime += deltaTime;
            if (tickTime < tick)
                return;

            tickTime -= tick;

            if (Physics.Raycast(startPos, direction, out RaycastHit hit, GameValue.PROJECTILE_MAX_LENGTH, GameValue.UNIT_LAYERS))
                OnHit(hit.collider.GetComponent<Unit>());
        }

        else
        {
            beamParticle.DisableBeam();
            OnEnd();
        }
    }

    public override void OnHit(Unit target)
    {
        if (!IsHittable(target))
            return;

        ApplyToTarget(target);
    }
}

#endregion

#region 이동 컴포넌트

public class Movement_LinearComponent : SkillComponent
{
    public override SkillComponentType Type => SkillComponentType.Linear;

    public override void Init(SkillLauncher launcher, SkillHolder holder, Unit fixedTarget)
    {
        base.Init(launcher, holder, fixedTarget);
    }

    public override void OnUpdate(float deltaTime)
    {
        // launcher.transform.position += launcher.Direction * moveSpeed * deltaTime;
    }
}

/// <summary> 도약 이동 컴포넌트 </summary>
public class Movement_LeapComponent : SkillComponent
{
    public override SkillComponentType Type => SkillComponentType.Leap;
    private SkillIndicator startIndicator;
    private SkillIndicator finalIndicator;
    private Vector3 startPos;
    private Vector3 targetPos;
    private Vector3 casterAdjustedPos;
    private float time;
    private bool isLeapCompleted;
    private float duration;
    private float invDuration;
    private AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public override void Reset()
    {
        base.Reset();
        time = 0f;
        startIndicator = null;
        finalIndicator = null;
    }

    public override void Init(SkillLauncher launcher, SkillHolder holder, Unit fixedTarget)
    {
        base.Init(launcher, holder, fixedTarget);
        targetPos = fixedTarget.transform.position;
        isLeapCompleted = false;
        duration = holder.durationFinal;
        invDuration = 1f / duration;
        startPos = launcher.Position;
        Vector3 dir = (launcher.Position - targetPos).normalized;
        casterAdjustedPos = dir * 2f;
    }

    public override void OnStart()
    {
        base.OnStart();

        // 스킬 인디케이터
        SkillData data = DataMgr.GetSkillData(launcher.SkillKey);
        SkillElement element = data.skillElements[0];
        SkillMgr skillMgr = GameMgr.Instance.skillMgr;

        startIndicator = skillMgr.CreateIndicator(element, false);
        startIndicator.DrawIndicator(targetPos, targetPos);
        finalIndicator = skillMgr.CreateIndicator(element, false);
        finalIndicator.DrawIndicator(targetPos, targetPos);
    }

    public override void OnUpdate(float deltaTime)
    {
        if (isLeapCompleted)
            return;

        time += deltaTime;
        float p = Mathf.Clamp01(time * invDuration);
        startIndicator.UpdateIndicatorScale(p);

        // 도약 이동
        float e = curve.Evaluate(p);
        Vector3 newPos = Vector3.Lerp(startPos, targetPos, e);
        newPos.y = Mathf.Sin(p * Mathf.PI);
        launcher.SetTransform(newPos, launcher.Direction);
        launcher.Caster.transform.position = newPos + casterAdjustedPos;

        if (p >= 1f) // 도약 종료
        {
            isLeapCompleted = true;
            GameMgr.Instance.skillMgr.RemoveIndicator(startIndicator);
            GameMgr.Instance.skillMgr.RemoveIndicator(finalIndicator);
            OnEnd();
        }
    }
}

#endregion

#region 효과 컴포넌트

public class Effect_GravityComponent : SkillComponent
{
    public override SkillComponentType Type => SkillComponentType.Gravity;

    private float gravityForce;        // 중력 강도
    private float duration;            // 지속 시간
    private float radius;              // 중력 범위
    private float radiusSqr;
    private float invRadiusSqr;
    private float rotationSpeed;       // 회전 속도

    private Vector3 gravityCenter;     // 중력 중심점
    private float time;
    private List<Unit> units = new();
    private Dictionary<Unit, float> startTimes = new();

    public override void Reset()
    {
        base.Reset();
        time = 0f;
        units.Clear();
        startTimes.Clear();
    }

    public override void Init(SkillLauncher launcher, SkillHolder holder, Unit fixedTarget)
    {
        base.Init(launcher, holder, fixedTarget);
        gravityForce = holder.gravityFinal;
        duration = holder.durationFinal;
        radius = holder.radiusFinal;
        radiusSqr = radius * radius;
        rotationSpeed = 15f;
        gravityCenter = launcher.Position;
        invRadiusSqr = 1f / radiusSqr;
    }

    public override void OnStart()
    {
        base.OnStart();

        List<Unit> targets = GetHitTargetsBySphere(gravityCenter, radius);
        foreach (Unit target in targets)
        {
            if (IsHittable(target))
            {
                units.Add(target);
                startTimes[target] = time;
            }
        }
    }

    public override void OnUpdate(float deltaTime)
    {
        time += deltaTime;

        for (int i = units.Count - 1; i >= 0; i--)
        {
            Unit unit = units[i];
            if (unit == null || unit.IsDead)
                continue;

            ApplyGravityEffect(unit, deltaTime);
        }

        if (time >= duration)
        {
            OnEnd();
        }
    }

    private void ApplyGravityEffect(Unit unit, float deltaTime)
    {
        Vector3 pos = unit.transform.position;
        float unitTime = time - startTimes[unit];

        float distanceToCenterSqr = (pos - gravityCenter).sqrMagnitude;
        if (distanceToCenterSqr <= 0.1f)
            return;

        // 나선형 이동
        Vector3 directionToCenter = (gravityCenter - pos).normalized;
        Vector3 rotatedDirection = Quaternion.AngleAxis(rotationSpeed * unitTime, Vector3.up) * directionToCenter;

        // 가까울수록 강해짐
        float distanceRatio = 1f - (distanceToCenterSqr * invRadiusSqr);
        float gravityPower = gravityForce * distanceRatio;

        Vector3 finalPos = pos + rotatedDirection * gravityPower * deltaTime;
        unit.transform.position = Vector3.Lerp(pos, finalPos, 0.1f);
    }
}

#endregion