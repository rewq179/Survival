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
    protected float damage;

    public ComponentState State => state;
    public bool IsCompleted => state == ComponentState.Completed;

    public virtual void Init(SkillLauncher launcher, InstanceValue inst, Unit fixedTarget)
    {
        this.launcher = launcher;
        order = inst.order;
        timing = inst.timing;
    }

    public virtual void OnStart(SkillLauncher launcher)
    {
        if (state != ComponentState.NotStarted)
            return;

        state = ComponentState.Running;
        enemyType = launcher.Caster.UnitType == UnitType.Player ? UnitType.Monster : UnitType.Player;
    }

    public virtual void OnUpdate(float deltaTime) { }
    protected virtual void OnEnd()
    {
        if (state == ComponentState.Completed)
            return;

        state = ComponentState.Completed;
        launcher.CheckDeactivate();
    }

    public virtual void OnHit(Unit target) { }
    protected void ApplyDamage(Unit target)
    {
        DamageInfo damageInfo = CombatMgr.PopDamageInfo();
        damageInfo.Init(launcher.Caster, target, damage, launcher.Position, launcher.SkillKey);
        CombatMgr.ProcessDamage(damageInfo);
    }

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
    protected SkillParticleController particle;

    public override void Init(SkillLauncher launcher, InstanceValue inst, Unit fixedTarget)
    {
        base.Init(launcher, inst, fixedTarget);

        // 공격 컴포넌트에만 파티클 할당
        particle = GameMgr.Instance.skillMgr.PopParticle(launcher.SkillKey, launcher.transform);
        if (particle == null)
            return;

        launcher.SetParticleFinished(false);
        particle.OnParticleFinished += OnParticleFinished;
    }

    private void OnParticleFinished()
    {
        launcher.SetParticleFinished(true);
        launcher.CheckDeactivate();
    }

    public override void OnStart(SkillLauncher launcher)
    {
        base.OnStart(launcher);
        particle?.Play();
    }

    protected override void OnEnd()
    {
        base.OnEnd();
        particle?.StopMain();
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

    public override void Init(SkillLauncher launcher, InstanceValue inst, Unit fixedTarget)
    {
        base.Init(launcher, inst, fixedTarget);
        damage = inst.damageFinal;
        moveSpeed = inst.moveSpeedFinal;
        richocet = inst.ricochetFinal.GetInt();
        piercing = inst.piercingFinal.GetInt();
        maxLength = GameValue.PROJECTILE_MAX_LENGTH_POW;
        hittedUnitIDs.Clear();
    }

    public override void OnStart(SkillLauncher launcher)
    {
        base.OnStart(launcher);
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

        // 충돌 체크
        if (Physics.Raycast(launcher.Position, direction, out RaycastHit hit, moveDistance, GameValue.UNIT_LAYERS))
        {
            OnHit(hit.collider.GetComponent<Unit>());
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
        ApplyDamage(target);
        isHit = true;
        particle?.PlayHit();

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

    public override void Init(SkillLauncher launcher, InstanceValue inst, Unit fixedTarget)
    {
        base.Init(launcher, inst, fixedTarget);
        damage = inst.damageFinal;
        radius = inst.radiusFinal;
        angle = inst.angle;
        maxDistance = radius * radius;
        type = angle == 360f ? SkillIndicatorType.Circle : SkillIndicatorType.Sector;
    }

    public override void OnStart(SkillLauncher launcher)
    {
        base.OnStart(launcher);
        ExecuteAttack();
        OnEnd();
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
        ApplyDamage(target);
        particle?.PlayHit();
    }
}

/// <summary> 주기적 데미지 공격 컴포넌트 </summary>
public class Attack_PeriodicAOEComponent : Attack_AOEComponent
{
    public override SkillComponentType Type => SkillComponentType.PeriodicAOE;
    private float duration;
    private float interval;
    private float time;
    private float lastDamageTime;

    public override void Init(SkillLauncher launcher, InstanceValue inst, Unit fixedTarget)
    {
        base.Init(launcher, inst, fixedTarget);
        interval = inst.damageTickFinal;
        duration = inst.durationFinal;
        time = 0f;
        lastDamageTime = Time.time;
    }

    public override void OnUpdate(float deltaTime)
    {
        time += deltaTime;
        float currentTime = Time.time;
        if (currentTime - lastDamageTime >= interval)
        {
            ExecuteAttack();
            lastDamageTime = currentTime;
        }

        if (time >= duration)
        {
            OnEnd();
        }
    }
}

/// <summary> 즉시 공격 컴포넌트 </summary>
public class Attack_ImmediateComponent : Attack_Component
{
    public override SkillComponentType Type => SkillComponentType.InstantAttack;
    private Unit target;

    public override void Init(SkillLauncher launcher, InstanceValue inst, Unit fixedTarget)
    {
        base.Init(launcher, inst, fixedTarget);
        target = fixedTarget;
        damage = inst.damageFinal;
    }

    public override void OnStart(SkillLauncher launcher)
    {
        base.OnStart(launcher);
        OnHit(target);
        OnEnd();
    }

    public override void OnHit(Unit target)
    {
        ApplyDamage(target);
        particle?.PlayHit();
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

    public override void Init(SkillLauncher launcher, InstanceValue inst, Unit fixedTarget)
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
        tickTime = 0f;
        time = 0f;
    }

    public override void OnStart(SkillLauncher launcher)
    {
        base.OnStart(launcher);
        startPos = launcher.Position;
        direction = (targetPos - startPos).normalized;

        // 스킬 인디케이터
        SkillData data = DataMgr.GetSkillData(launcher.SkillKey);
        SkillElement element = data.skillElements[0];
        startIndicator = GameMgr.Instance.skillMgr.CreateIndicator(element, false);
        startIndicator.DrawIndicator(startPos, targetPos);
        finalIndicator = GameMgr.Instance.skillMgr.CreateIndicator(element, false);
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

            beamParticle = particle.GetComponent<BeamParticle>();
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

        ApplyDamage(target);
    }
}

#endregion

#region 이동 컴포넌트

public class Movement_LinearComponent : SkillComponent
{
    public override SkillComponentType Type => SkillComponentType.Linear;

    public override void Init(SkillLauncher launcher, InstanceValue inst, Unit fixedTarget)
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

    public override void Init(SkillLauncher launcher, InstanceValue inst, Unit fixedTarget)
    {
        base.Init(launcher, inst, fixedTarget);
        this.targetPos = fixedTarget.transform.position;
        isLeapCompleted = false;
        duration = inst.durationFinal;
        invDuration = 1f / duration;
        time = 0f;
    }

    public override void OnStart(SkillLauncher launcher)
    {
        base.OnStart(launcher);

        // 위치
        startPos = launcher.Position;
        Vector3 dir = (launcher.Position - targetPos).normalized;
        casterAdjustedPos = dir * 2f;

        // 스킬 인디케이터
        SkillData data = DataMgr.GetSkillData(launcher.SkillKey);
        SkillElement element = data.skillElements[0];
        startIndicator = GameMgr.Instance.skillMgr.CreateIndicator(element, false);
        startIndicator.DrawIndicator(targetPos, targetPos);
        finalIndicator = GameMgr.Instance.skillMgr.CreateIndicator(element, false);
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

public class Effect_FreezeComponent : SkillComponent
{
    public override SkillComponentType Type => SkillComponentType.Freeze;
    private float time;
    private List<Unit> monsters = new();

    private const float SLOW_PERCENT = -0.5f;
    private const float DURATION = 5f;

    public override void OnStart(SkillLauncher launcher)
    {
        base.OnStart(launcher);
        time = 0f;

        monsters = new List<Unit>(GameMgr.Instance.spawnMgr.AliveEnemies);
        foreach (Unit monster in monsters)
        {
            monster.AddStatModifier(StatType.MoveSpeed, SLOW_PERCENT);
            monster.UpdateMoveSpeed();
        }
    }

    public override void OnUpdate(float deltaTime)
    {
        time += deltaTime;
        if (time < DURATION)
            return;

        foreach (Unit monster in monsters)
        {
            monster.AddStatModifier(StatType.MoveSpeed, SLOW_PERCENT);
        }

        OnEnd();
    }
}

public class Effect_ExplosionComponent : SkillComponent
{
    public override SkillComponentType Type => SkillComponentType.Explosion;
    private const float EXPLOSION_RADIUS = 20f;
    private const float EXPLOSION_DAMAGE = 100f;

    public void Init()
    {
        damage = EXPLOSION_DAMAGE;
    }

    public override void OnStart(SkillLauncher launcher)
    {
        base.OnStart(launcher);

        List<Unit> targets = GetHitTargetsBySphere(launcher.Position, EXPLOSION_RADIUS);
        foreach (Unit target in targets)
        {
            OnHit(target);
        }
    }

    public override void OnHit(Unit target) => ApplyDamage(target);
}