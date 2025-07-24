using UnityEngine;
using System.Collections.Generic;
using static Easing;
using System.Linq;

public enum SkillComponentType
{
    // 공격
    Projectile,
    Boomerang,
    RotatingOrbs,
    InstantAOE,
    PeriodicAOE,
    RiseAOE,
    InstantAttack,
    Beam,
    KnockbackAOE,
    GravityAOE,
    // 이동
    Linear,
    Leap,
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

    protected List<ISkillEffect> skillEffects = new();

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

        foreach (ISkillEffect effect in skillEffects)
        {
            effect.Reset();
        }

        skillEffects.Clear();
    }

    public virtual void Init(SkillLauncher launcher, SkillHolder holder, Unit fixedTarget)
    {
        this.launcher = launcher;
        enemyType = launcher.Caster.UnitType == UnitType.Player ? UnitType.Monster : UnitType.Player;
        order = holder.order;
        timing = holder.timing;
    }

    protected void AddSkillEffect(ISkillEffect effect) => skillEffects.Add(effect);

    public void OnStart()
    {
        if (state != ComponentState.NotStarted)
            return;

        OnStartAction();
    }

    protected virtual void OnStartAction()
    {
        state = ComponentState.Running;
    }

    public void OnUpdate(float deltaTime)
    {
        if (state != ComponentState.Running)
            return;

        OnUpdateAction(deltaTime);
    }

    protected virtual void OnUpdateAction(float deltaTime) { }

    protected void OnEnd(bool forceEnd = false)
    {
        if (state == ComponentState.Completed)
            return;

        OnEndAction(forceEnd);
    }

    protected virtual void OnEndAction(bool forceEnd)
    {
        state = ComponentState.Completed;
        launcher.CheckDeactivate(forceEnd);
    }

    public void OnHit(Unit target)
    {
        if (!IsHittable(target))
            return;

        OnHitAction(target);
    }

    protected virtual bool IsHittable(Unit target)
    {
        if (target == null || target.IsDead || target.UnitType != enemyType)
            return false;

        return true;
    }

    protected virtual void OnHitAction(Unit target) { }

    protected List<Unit> GetTargetsByOverlapSphere(Vector3 position, float radius)
    {
        Collider[] colliders = Physics.OverlapSphere(position, radius, GameValue.UNIT_LAYERS);

        List<Unit> targets = new();
        foreach (Collider col in colliders)
        {
            Unit target = col.GetComponent<Unit>();
            if (IsHittable(target))
            {
                targets.Add(target);
            }
        }

        return targets;
    }

    protected bool IsTargetInSectorArea(Vector3 position, Vector3 direction, Vector3 targetPosition,
        float angle, float maxDistance)
    {
        float sqrDistance = (targetPosition - position).sqrMagnitude;
        if (sqrDistance > maxDistance)
            return false;

        float targetAngle = Vector3.Angle(direction, (targetPosition - position).normalized);
        return targetAngle <= angle * 0.5f;
    }

    protected bool IsTargetInRectangleArea(Vector3 position, Vector3 direction, Vector3 targetPosition)
    {
        return false; // TODO: 사각형 형태로 구현할 것
    }
}

/// <summary> 공격 컴포넌트 기본 클래스 </summary>
public abstract class Attack_Component : SkillComponent
{
    protected SkillEffectController effectController;

    public override void Reset()
    {
        base.Reset();
        effectController = null;
    }

    public override void Init(SkillLauncher launcher, SkillHolder holder, Unit fixedTarget)
    {
        base.Init(launcher, holder, fixedTarget);

        AddSkillEffect(new DamageEffect(launcher, holder.damageFinal));
        foreach (BuffKey buffKey in holder.BuffKeys)
        {
            AddSkillEffect(new BuffEffect(launcher, buffKey));
        }

        effectController = GameMgr.Instance.skillMgr.PopSkillObject(launcher.SkillKey, launcher.transform);
        if (effectController != null)
        {
            effectController.Init(launcher, OnHit, OnParticleFinished);
        }
    }

    public override SkillEffectController EffectController => effectController;

    protected override void OnStartAction()
    {
        base.OnStartAction();
        effectController?.Play();
    }

    protected override void OnHitAction(Unit target)
    {
        foreach (ISkillEffect effect in skillEffects)
        {
            effect.OnApply(EffectTiming.OnHit, target);
        }

        effectController?.PlayHit();
    }

    protected override void OnUpdateAction(float deltaTime)
    {
        base.OnUpdateAction(deltaTime);

        // 지속 실행할 효과들
        foreach (ISkillEffect effect in skillEffects)
        {
            effect.OnUpdate(EffectTiming.OnUpdate, deltaTime);
        }
    }

    protected void OnParticleFinished()
    {
        launcher.SetParticleFinished(true);
        launcher.CheckDeactivate(false);
    }
}

public abstract class BaseProjectile_Component : Attack_Component
{
    protected MovementEffect movementEffect;
    protected float maxLength;
    protected Vector3 startPos;

    public override void Init(SkillLauncher launcher, SkillHolder holder, Unit fixedTarget)
    {
        base.Init(launcher, holder, fixedTarget);

        maxLength = GameValue.PROJECTILE_MAX_LENGTH_POW;
        startPos = launcher.Position;
        movementEffect = new MovementEffect(holder.moveSpeedFinal, launcher.Direction, launcher.transform);
        AddSkillEffect(movementEffect);
    }

    protected bool HasReachedMaxDistance()
    {
        float currentDistance = (launcher.Position - startPos).sqrMagnitude;
        return currentDistance >= maxLength;
    }
}

/// <summary> 일반 발사체 컴포넌트 </summary>
public class ProjectileComponent : BaseProjectile_Component
{
    public override SkillComponentType Type => SkillComponentType.Projectile;
    private int ricohet;
    private int piercing;
    private HashSet<int> hittedUnitIDs = new();
    private const float RICOCHET_RADIUS = 6f;

    public override void Reset()
    {
        base.Reset();
        hittedUnitIDs.Clear();
    }

    public override void Init(SkillLauncher launcher, SkillHolder holder, Unit fixedTarget)
    {
        base.Init(launcher, holder, fixedTarget);

        piercing = holder.ShotFinal.GetInt();
        ricohet = holder.ShotFinal.GetInt();
        hittedUnitIDs.Add(launcher.Caster.UniqueID);
    }

    protected override void OnUpdateAction(float deltaTime)
    {
        base.OnUpdateAction(deltaTime);

        if (HasReachedMaxDistance())
        {
            OnEnd();
        }
    }

    protected override bool IsHittable(Unit target)
    {
        if (base.IsHittable(target))
        {
            if (hittedUnitIDs.Contains(target.UniqueID))
                return false;

            return true;
        }

        return false;
    }

    protected override void OnHitAction(Unit target)
    {
        hittedUnitIDs.Add(target.UniqueID);
        base.OnHitAction(target);

        if (ricohet-- > 0)
        {
            DoRicochet();
            return;
        }

        if (piercing-- > 0)
        {
            return;
        }

        OnEnd();
    }

    private void DoRicochet()
    {
        Unit ricochetTarget = FindRicochetTarget(launcher.Position, RICOCHET_RADIUS);
        if (ricochetTarget == null)
            return;

        // 위치
        startPos = launcher.Position;
        Vector3 targetPos = ricochetTarget.transform.position;
        targetPos.y = startPos.y;

        // 방향
        Vector3 direction = (targetPos - startPos).normalized;
        launcher.SetTransform(startPos, direction);
        movementEffect.SetDirection(direction);
    }

    private Unit FindRicochetTarget(Vector3 position, float radius)
    {
        Collider[] colliders = Physics.OverlapSphere(position, radius, GameValue.UNIT_LAYERS);
        foreach (Collider col in colliders)
        {
            Unit target = col.GetComponent<Unit>();
            if (IsHittable(target))
                return target;
        }

        return null;
    }
}

/// <summary> 부메랑 발사체 컴포넌트 </summary>
public class BoomerangComponent : BaseProjectile_Component
{
    public override SkillComponentType Type => SkillComponentType.Boomerang;

    private bool isReturning;
    private int hittedCount;
    private float originalDamage;
    private float returnSpeed;
    private Vector3 endPos;

    // 상수
    private const float RETURN_SPEED_MULTIPLIER = 1.5f; // 복귀 속도 증가 배율
    private const float DAMAGE_REDUCTION_PER_HIT = 0.15f; // 데미지 감소 비율
    private const float MIN_DAMAGE_RATIO = 0.1f; // 최소 데미지 비율

    public override void Reset()
    {
        base.Reset();
        hittedCount = 0;
        isReturning = false;
    }

    public override void Init(SkillLauncher launcher, SkillHolder holder, Unit fixedTarget)
    {
        base.Init(launcher, holder, fixedTarget);

        // 부메랑 설정
        originalDamage = holder.damageFinal;
        returnSpeed = holder.moveSpeedFinal * RETURN_SPEED_MULTIPLIER;
    }

    protected override void OnUpdateAction(float deltaTime)
    {
        base.OnUpdateAction(deltaTime);

        if (isReturning)
        {
            if (HasReachedCaster())
            {
                OnEnd();
            }

            return;
        }

        if (HasReachedMaxDistance())
        {
            StartReturnShot();
        }
    }

    private void StartReturnShot()
    {
        hittedCount = 0;
        isReturning = true;

        // Y축 매칭
        startPos = launcher.Position;
        endPos = launcher.Caster.transform.position;
        endPos.y = startPos.y;

        // 방향
        Vector3 returnDirection = (endPos - startPos).normalized;
        launcher.SetTransform(startPos, returnDirection);
        movementEffect.SetDirection(returnDirection);
        movementEffect.SetSpeed(returnSpeed);

        // 데미지 복원
        foreach (ISkillEffect effect in skillEffects)
        {
            if (effect is DamageEffect damageEffect)
            {
                damageEffect.SetDamage(originalDamage);
            }
        }
    }

    private bool HasReachedCaster()
    {
        float currentDistance = (endPos - launcher.Position).sqrMagnitude;
        return currentDistance <= 0.01f;
    }

    protected override void OnHitAction(Unit target)
    {
        // 관통 횟수 별 데미지 감소
        float multiplier = 1f - hittedCount++ * DAMAGE_REDUCTION_PER_HIT;
        float currentDamage = Mathf.Max(originalDamage * multiplier, MIN_DAMAGE_RATIO);

        // 데미지 효과 업데이트
        foreach (ISkillEffect effect in skillEffects)
        {
            if (effect is DamageEffect damageEffect)
            {
                damageEffect.SetDamage(currentDamage);
            }
        }

        base.OnHitAction(target);
    }
}

/// <summary> 회전하는 구체들로 구성된 이동 공격 컴포넌트 </summary>
public class RotatingOrbs_Component : BaseProjectile_Component
{
    public override SkillComponentType Type => SkillComponentType.RotatingOrbs;
    private float rotationSpeed;
    private int orbCount;
    private float anglePerOrb;
    private float currentAngle;
    private List<SkillEffectController> orbEffects = new();

    private RotationEffect rotationEffect;

    // 구체들의 위치 계산용
    private List<Vector3> orbPositions = new();
    private float currentRadius;
    private const float CIRCLE_MAX_RADIUS = 2.5f;
    private const float CIRCLE_MIN_RADIUS = 0.5f;
    private const float RADIUS_DIFF = CIRCLE_MAX_RADIUS - CIRCLE_MIN_RADIUS;

    // 구체들의 확장/축소 용
    private float cycleTime;
    private const float EXPAND_DURATION = 0.4f;
    private const float INV_EXPAND_DURATION = 1f / EXPAND_DURATION;
    private const float WAIT_DURATION = 0.2f;
    private const float HALF_DURATION = EXPAND_DURATION + WAIT_DURATION;
    private const float CYCLE_DURATION = EXPAND_DURATION * 2f + WAIT_DURATION * 2f;

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
        cycleTime = 0f;
    }

    public override void Init(SkillLauncher launcher, SkillHolder holder, Unit fixedTarget)
    {
        base.Init(launcher, holder, fixedTarget);
        orbEffects.Add(launcher.GetComponentInChildren<SkillEffectController>());

        // 구체
        rotationSpeed = holder.rotationSpeedFinal;
        rotationEffect = new RotationEffect(launcher, rotationSpeed);
        AddSkillEffect(rotationEffect);

        orbCount = holder.ShotFinal.GetInt();
        anglePerOrb = 360f / orbCount; // 360도를 구체 개수로 나누어 균등 배치

        // 확장 축소
        currentRadius = CIRCLE_MIN_RADIUS;

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

    protected override void OnUpdateAction(float deltaTime)
    {
        base.OnUpdateAction(deltaTime);

        currentAngle += rotationSpeed * deltaTime;
        cycleTime += deltaTime;

        CalculateOrbRadius();
        CalculateOrbPositions();
        UpdateOrbPosition();
        UpdateOrbRotation();

        if (HasReachedMaxDistance())
        {
            OnEnd();
        }
    }

    private void CalculateOrbRadius()
    {
        if (cycleTime < EXPAND_DURATION) // 확장 단계
        {
            float easing = Quadratic.Out(cycleTime * INV_EXPAND_DURATION);
            currentRadius = CIRCLE_MIN_RADIUS + RADIUS_DIFF * easing;
        }

        else if (cycleTime < HALF_DURATION) // 확장 후 대기
        {
            currentRadius = CIRCLE_MAX_RADIUS;
        }

        else if (cycleTime < HALF_DURATION + EXPAND_DURATION) // 축소 단계
        {
            float easing = Quadratic.Out((cycleTime - HALF_DURATION) * INV_EXPAND_DURATION);
            currentRadius = CIRCLE_MAX_RADIUS - RADIUS_DIFF * easing;
        }

        else if (cycleTime < CYCLE_DURATION) // 축소 후 대기
        {
            currentRadius = CIRCLE_MIN_RADIUS;
        }

        else
        {
            cycleTime %= CYCLE_DURATION;
        }
    }

    private void CalculateOrbPositions()
    {
        orbPositions.Clear();
        Vector3 center = launcher.Position;

        for (int i = 0; i < orbCount; i++)
        {
            float angle = currentAngle + anglePerOrb * i;
            float radian = angle * Mathf.Deg2Rad;

            Vector3 orbPos = center + new Vector3(
                Mathf.Cos(radian) * currentRadius,
                0f,
                Mathf.Sin(radian) * currentRadius
            );
            orbPositions.Add(orbPos);
        }
    }

    private void UpdateOrbPosition()
    {
        for (int i = 0; i < orbEffects.Count && i < orbPositions.Count; i++)
        {
            orbEffects[i].transform.position = orbPositions[i];
        }
    }

    private void UpdateOrbRotation()
    {
        rotationEffect.SetCurrentAngle(currentAngle);
    }
}

/// <summary> 범위 공격 컴포넌트 </summary>
public class InstantAOE_Component : Attack_Component
{
    public override SkillComponentType Type => SkillComponentType.InstantAOE;
    protected SkillIndicatorType indicatorType;
    protected float radius;
    protected float angle;

    protected virtual bool IsInstantEnd => true;
    public override void Init(SkillLauncher launcher, SkillHolder holder, Unit fixedTarget)
    {
        base.Init(launcher, holder, fixedTarget);
        radius = holder.radiusFinal;
        angle = holder.angle;
        indicatorType = holder.angle == 360 ? SkillIndicatorType.Circle : SkillIndicatorType.Sector;
    }

    protected override void OnStartAction()
    {
        base.OnStartAction();

        ExecuteAction();
        if (IsInstantEnd)
        {
            OnEnd();
        }
    }

    protected void ExecuteAction()
    {
        List<Unit> targets = GetTargetsByOverlapSphere(launcher.Position, radius);

        Vector3 position = launcher.Position;
        Vector3 direction = launcher.Direction;
        foreach (Unit target in targets)
        {
            Vector3 targetPosition = target.transform.position;
            switch (indicatorType)
            {
                case SkillIndicatorType.Sector:
                    if (!IsTargetInSectorArea(position, direction, targetPosition, angle, radius))
                        continue;
                    break;

                case SkillIndicatorType.Rectangle:
                    if (!IsTargetInRectangleArea(position, direction, targetPosition))
                        continue;
                    break;
            }

            OnHit(target);
        }
    }
}

/// <summary> 넉백 범위 공격 컴포넌트 </summary>
public class KnockbackAOE_Component : InstantAOE_Component
{
    public override SkillComponentType Type => SkillComponentType.KnockbackAOE;

    protected override bool IsInstantEnd => false;
    public override void Init(SkillLauncher launcher, SkillHolder holder, Unit fixedTarget)
    {
        base.Init(launcher, holder, fixedTarget);
        AddSkillEffect(new KnockbackEffect(launcher, holder.gravityFinal, holder.durationFinal));
        AddSkillEffect(new StunEffect(launcher, holder.durationFinal));
    }
}

/// <summary> 주기적 범위 공격 컴포넌트 </summary>
public class PeriodicAOE_Component : InstantAOE_Component
{
    public override SkillComponentType Type => SkillComponentType.PeriodicAOE;
    protected float duration;
    protected float time;
    protected float tick;
    protected float lastTickTime;

    protected override bool IsInstantEnd => false;
    public override void Init(SkillLauncher launcher, SkillHolder holder, Unit fixedTarget)
    {
        base.Init(launcher, holder, fixedTarget);

        duration = holder.durationFinal;
        tick = holder.damageTickFinal;
        lastTickTime = Time.time;
    }

    protected override void OnUpdateAction(float deltaTime)
    {
        base.OnUpdateAction(deltaTime);

        time += deltaTime;
        float currentTime = Time.time;

        if (currentTime - lastTickTime >= tick)
        {
            ExecuteAction();
            lastTickTime = currentTime;
        }

        if (time >= duration)
        {
            OnEnd();
        }
    }
}

/// <summary> 수직 이동 & 스턴 범위 공격 컴포넌트 </summary>
public class RiseAOE_Component : PeriodicAOE_Component
{
    public override SkillComponentType Type => SkillComponentType.RiseAOE;
    private const float RISE_DURATION = 0.5f;

    public override void Init(SkillLauncher launcher, SkillHolder holder, Unit fixedTarget)
    {
        base.Init(launcher, holder, fixedTarget);
        AddSkillEffect(new StunEffect(launcher, RISE_DURATION));
        AddSkillEffect(new VerticalMovementEffect(RISE_DURATION));
    }
}

/// <summary> 중력 범위 공격 컴포넌트 </summary>
public class GravityAOE_Component : PeriodicAOE_Component
{
    public override SkillComponentType Type => SkillComponentType.GravityAOE;
    private HashSet<Unit> hitUnits = new();

    public override void Reset()
    {
        base.Reset();
        hitUnits.Clear();
    }

    public override void Init(SkillLauncher launcher, SkillHolder holder, Unit fixedTarget)
    {
        base.Init(launcher, holder, fixedTarget);
        AddSkillEffect(new GravityEffect(launcher, duration, radius, holder.gravityFinal));
        AddSkillEffect(new StunEffect(launcher, duration));
    }

    protected override bool IsHittable(Unit target)
    {
        return base.IsHittable(target) && !hitUnits.Contains(target);
    }

    protected override void OnHitAction(Unit target)
    {
        base.OnHitAction(target);
        hitUnits.Add(target);
    }
}

/// <summary> 즉시 공격 컴포넌트 </summary>
public class InstantAttack_Component : Attack_Component
{
    public override SkillComponentType Type => SkillComponentType.InstantAttack;
    private Unit fixedTarget;

    public override void Init(SkillLauncher launcher, SkillHolder holder, Unit fixedTarget)
    {
        base.Init(launcher, holder, fixedTarget);
        this.fixedTarget = fixedTarget;
    }

    protected override void OnStartAction()
    {
        base.OnStartAction();
        OnHit(fixedTarget);
        OnEnd(true);
    }
}

/// <summary> 빔 공격 컴포넌트 </summary>
public class Beam_Component : Attack_Component
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

    protected override void OnStartAction()
    {
        base.OnStartAction();

        SkillData data = DataMgr.GetSkillData(launcher.SkillKey);
        SkillElement element = data.skillElements[0];
        SkillMgr skillMgr = GameMgr.Instance.skillMgr;

        // 스킬 인디케이터
        startIndicator = skillMgr.CreateIndicator(element, false);
        startIndicator.DrawIndicator(startPos, targetPos);
        finalIndicator = skillMgr.CreateIndicator(element, false);
        finalIndicator.DrawIndicator(startPos, targetPos);
    }

    protected override void OnUpdateAction(float deltaTime)
    {
        base.OnUpdateAction(deltaTime);

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
}

/// <summary> 도약 이동 컴포넌트 </summary>
public class Leap_Component : SkillComponent
{
    public override SkillComponentType Type => SkillComponentType.Leap;
    private SkillIndicator startIndicator;
    private SkillIndicator finalIndicator;
    private Vector3 startPos;
    private Vector3 targetPos;
    private Vector3 casterAdjustedPos;
    private float time;
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
        duration = holder.durationFinal;
        invDuration = 1f / duration;
        startPos = launcher.Position;
        Vector3 dir = (launcher.Position - targetPos).normalized;
        casterAdjustedPos = dir * 2f;
    }

    protected override void OnStartAction()
    {
        base.OnStartAction();

        // 스킬 인디케이터
        SkillData data = DataMgr.GetSkillData(launcher.SkillKey);
        SkillElement element = data.skillElements[0];
        SkillMgr skillMgr = GameMgr.Instance.skillMgr;

        startIndicator = skillMgr.CreateIndicator(element, false);
        startIndicator.DrawIndicator(targetPos, targetPos);
        finalIndicator = skillMgr.CreateIndicator(element, false);
        finalIndicator.DrawIndicator(targetPos, targetPos);
    }

    protected override void OnUpdateAction(float deltaTime)
    {
        time += deltaTime;
        float p = Mathf.Clamp01(time * invDuration);
        startIndicator.UpdateIndicatorScale(p);

        // 도약 위치
        Vector3 pos = Vector3.Lerp(startPos, targetPos + casterAdjustedPos, curve.Evaluate(p));
        pos.y = Mathf.Sin(p * Mathf.PI);
        launcher.SetTransform(pos, launcher.Direction);

        // 유닛 이동
        Unit caster = launcher.Caster;
        caster.transform.position = pos;
        caster.SetForceMoving(true);

        if (p >= 1f) // 도약 종료
        {
            caster.SetForceMoving(false);
            GameMgr.Instance.skillMgr.RemoveIndicator(startIndicator);
            GameMgr.Instance.skillMgr.RemoveIndicator(finalIndicator);
            OnEnd();
        }
    }
}