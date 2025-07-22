using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public enum SkillComponentType
{
    Projectile,
    InstantAOE,
    PeriodicAOE,
    InstantAttack,
    Beam,
    Linear,
    Leap,
    Gravity,
    Freeze,
    Explosion,
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

    public virtual void Init(SkillLauncher launcher, SkillHolder inst, Unit fixedTarget)
    {
        this.launcher = launcher;
        enemyType = launcher.Caster.UnitType == UnitType.Player ? UnitType.Monster : UnitType.Player;

        if (inst != null)
        {
            order = inst.order;
            timing = inst.timing;
        }
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

    public override void Init(SkillLauncher launcher, SkillHolder inst, Unit fixedTarget)
    {
        base.Init(launcher, inst, fixedTarget);
        damage = inst.damageFinal;
        buffKeys = inst.BuffKeys;

        // 공격 컴포넌트에만 파티클 할당
        effectController = GameMgr.Instance.skillMgr.PopSkillObject(launcher.SkillKey, launcher.transform);
        if (effectController == null)
            return;

        effectController.Init();
        launcher.SetParticleFinished(false);
        effectController.SubscribeParticleFinished(OnParticleFinished);
        effectController.SubscribeHitTarget(OnHit);
    }

    public override SkillEffectController EffectController => effectController;
    private void OnParticleFinished()
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

/// <summary> 투사체 공격 컴포넌트 </summary>
public class Attack_ProjectileComponent : Attack_Component
{
    private float moveSpeed;
    private float maxLength;
    private int richocet;
    private int piercing;
    private Vector3 startPos;
    private Vector3 direction;
    private bool isHit;
    private HashSet<int> hittedUnitIDs = new();

    public override SkillComponentType Type => SkillComponentType.Projectile;

    public override void Reset()
    {
        base.Reset();
        isHit = false;
        hittedUnitIDs.Clear();
    }

    public override void Init(SkillLauncher launcher, SkillHolder inst, Unit fixedTarget)
    {
        base.Init(launcher, inst, fixedTarget);
        moveSpeed = inst.moveSpeedFinal;
        richocet = inst.ricochetFinal.GetInt();
        piercing = inst.piercingFinal.GetInt();
        maxLength = GameValue.PROJECTILE_MAX_LENGTH_POW;
        startPos = launcher.Position;
        direction = launcher.transform.forward;
        hittedUnitIDs.Add(launcher.Caster.UniqueID);
    }

    public override void OnUpdate(float deltaTime)
    {
        if (isHit)
            return;

        float moveDistance = moveSpeed * deltaTime;
        launcher.transform.position += direction * moveDistance;

        // 최대 거리 체크
        float currentDistance = (launcher.Position - startPos).sqrMagnitude;
        if (currentDistance >= maxLength)
        {
            OnEnd();
            return;
        }
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
        isHit = true;
        effectController.PlayHit();

        // 도탄
        if (richocet > 0)
        {
            Unit nextTarget = FindRicochetTarget(launcher.Position, 8f);
            if (nextTarget != null)
            {
                richocet--;
                isHit = false;

                startPos = launcher.Position;
                Vector3 targetPos = nextTarget.transform.position;
                targetPos.y = startPos.y;
                direction = (targetPos - startPos).normalized;
                launcher.SetTransform(startPos, direction);
                return;
            }
        }

        // 관통
        if (piercing > 0)
        {
            isHit = false;
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
        Collider[] colliders = Physics.OverlapSphere(position, radius, GameValue.UNIT_LAYERS);

        Unit target = null;
        float maxDist = float.MaxValue;

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

/// <summary> 범위 공격 컴포넌트 </summary>
public class Attack_AOEComponent : Attack_Component
{
    public override SkillComponentType Type => SkillComponentType.InstantAOE;
    protected SkillIndicatorType type;
    protected float angle;
    protected float radius;
    protected float maxDistance;

    protected virtual bool IsInstantComplete => true;
    public override void Init(SkillLauncher launcher, SkillHolder inst, Unit fixedTarget)
    {
        base.Init(launcher, inst, fixedTarget);
        damage = inst.damageFinal;
        radius = inst.radiusFinal;
        angle = inst.angle;
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

    public override void Init(SkillLauncher launcher, SkillHolder inst, Unit fixedTarget)
    {
        base.Init(launcher, inst, fixedTarget);
        tick = inst.damageTickFinal;
        duration = inst.durationFinal;
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

    public override void Init(SkillLauncher launcher, SkillHolder inst, Unit fixedTarget)
    {
        base.Init(launcher, inst, fixedTarget);
        target = fixedTarget;
        damage = inst.damageFinal;
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

    public override void Init(SkillLauncher launcher, SkillHolder inst, Unit fixedTarget)
    {
        base.Init(launcher, inst, fixedTarget);
        damage = inst.damageFinal;
        length = GameValue.PROJECTILE_MAX_LENGTH;
        tick = inst.damageTickFinal;
        duration = inst.durationFinal;

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

    public override void Init(SkillLauncher launcher, SkillHolder inst, Unit fixedTarget)
    {
        base.Init(launcher, inst, fixedTarget);
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

    public override void Init(SkillLauncher launcher, SkillHolder inst, Unit fixedTarget)
    {
        base.Init(launcher, inst, fixedTarget);
        targetPos = fixedTarget.transform.position;
        isLeapCompleted = false;
        duration = inst.durationFinal;
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

    public override void Init(SkillLauncher launcher, SkillHolder inst, Unit fixedTarget)
    {
        base.Init(launcher, inst, fixedTarget);
        gravityForce = inst.gravityFinal;
        duration = inst.durationFinal;
        radius = inst.radiusFinal;
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

public class Effect_FreezeComponent : SkillComponent
{
    public override SkillComponentType Type => SkillComponentType.Freeze;
    private float time;
    private float duration;
    private List<Unit> monsters = new();
    private HashSet<int> monsterUniqueIDs = new();

    private const float SLOW_PERCENT = -0.5f;

    public override void Reset()
    {
        base.Reset();
        time = 0f;
        monsters.Clear();
        monsterUniqueIDs.Clear();
    }

    public override void Init(SkillLauncher launcher, SkillHolder inst, Unit fixedTarget)
    {
        base.Init(launcher, inst, fixedTarget);
        if (inst != null)
            duration = inst.durationFinal;
        else
            duration = 5f; // 아이템에 의한 지속 시간
    }

    public override void OnStart()
    {
        base.OnStart();

        monsters = new List<Unit>(GameMgr.Instance.spawnMgr.AliveEnemies);
        foreach (Unit monster in monsters)
        {
            monsterUniqueIDs.Add(monster.UniqueID);
        }

        ApplySlowEffect();
    }

    public override void OnUpdate(float deltaTime)
    {
        time += deltaTime;
        if (time < duration)
            return;

        ApplySlowEffect();
        OnEnd();
    }

    private void ApplySlowEffect()
    {
        foreach (Unit monster in monsters)
        {
            if (monster.IsDead || !monsterUniqueIDs.Contains(monster.UniqueID))
                continue;

            monster.AddStatModifier(StatType.MoveSpeed, SLOW_PERCENT);
            monster.UpdateMoveSpeed();
        }
    }
}

public class Effect_ExplosionComponent : Attack_AOEComponent
{
    public override SkillComponentType Type => SkillComponentType.Explosion;
    private const float EXPLOSION_RADIUS = 20f;
    private const float EXPLOSION_DAMAGE = 100f;

    public override void Init(SkillLauncher launcher, SkillHolder inst, Unit fixedTarget)
    {
        base.Init(launcher, inst, fixedTarget);
        radius = EXPLOSION_RADIUS;
        damage = EXPLOSION_DAMAGE;
    }
}

#endregion