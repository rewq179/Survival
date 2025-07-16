using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public abstract class SkillComponent
{
    protected SkillLauncher launcher;
    protected SkillParticleController particleController;
    protected UnitType enemyType;
    protected float damage;

    public virtual void OnInitialize(SkillLauncher launcher, SkillParticleController particle)
    {
        this.launcher = launcher;
        enemyType = launcher.Caster.UnitType == UnitType.Player ? UnitType.Monster : UnitType.Player;

        if (particle == null)
            return;

        particleController = particle;
        particleController.OnParticleFinished += launcher.Deactivate;

        if (!IsWaitAction)
            particleController.Play();
    }

    protected void ApplyDamage(Unit target)
    {
        DamageInfo damageInfo = CombatMgr.PopDamageInfo();
        damageInfo.Init(launcher.Caster, target, damage, launcher.Position, launcher.SkillKey);
        CombatMgr.ProcessDamage(damageInfo);
    }

    public virtual bool IsWaitAction => false;
    public abstract void OnUpdate(float deltaTime);
    public abstract void OnHit(Unit target);
    public bool IsHittable(Unit target) => target != null && !target.IsDead && target.UnitType == enemyType;
    public virtual void OnDestroy() { }

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

/// <summary>
/// 투사체 효과
/// </summary>
public class ProjectileComponent : SkillComponent
{
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

    public override void OnInitialize(SkillLauncher launcher, SkillParticleController particle)
    {
        base.OnInitialize(launcher, particle);
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
            launcher.Deactivate();
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

/// <summary>
/// 범위 효과
/// </summary>
public class AOEComponent : SkillComponent
{
    protected SkillIndicatorType type;
    protected float angle;
    protected float radius;
    protected float maxDistance;
    protected bool isWaitAction;
    protected bool canAction;

    public override bool IsWaitAction => isWaitAction;

    public AOEComponent(InstanceValue inst, bool isWait)
    {
        damage = inst.damageFinal;
        radius = inst.radiusFinal;
        angle = inst.angle;
        maxDistance = radius * radius;
        this.isWaitAction = isWait;
        canAction = false;
        type = angle == 360f ? SkillIndicatorType.Circle : SkillIndicatorType.Sector;
    }

    public override void OnInitialize(SkillLauncher launcher, SkillParticleController particle)
    {
        base.OnInitialize(launcher, particle);

        if (!IsWaitAction)
            ExecuteAttack();
    }

    public void SetCanAction(bool canAction) => this.canAction = canAction;
    public override void OnUpdate(float deltaTime)
    {
        if (isWaitAction && canAction)
        {
            particleController.Play();
            isWaitAction = false;
            ExecuteAttack();
        }
    }

    public void ExecuteAttack()
    {
        Vector3 position = launcher.Position;
        Vector3 direction = launcher.Direction;

        List<Unit> targets = GetHitTargetsBySphere(position, radius);
        foreach (Unit target in targets)
        {
            if (IsTargetInSkillArea(position, direction, target.transform.position))
                OnHit(target);
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

    public override void OnHit(Unit target) => ApplyDamage(target);
    public override void OnDestroy() { }
}

/// <summary>
/// 주기적 데미지 효과
/// </summary>
public class PeriodicAOEComponent : AOEComponent
{
    private float interval;
    private float lastDamageTime;

    public PeriodicAOEComponent(InstanceValue inst) : base(inst, false)
    {
        interval = inst.damageTickFinal;
        lastDamageTime = Time.time;
    }

    public override void OnUpdate(float deltaTime)
    {
        if (Time.time - lastDamageTime >= interval)
        {
            ExecuteAttack();
            lastDamageTime = Time.time;
        }
    }
}

/// <summary>
/// 즉시 공격 효과
/// </summary>
public class InstantComponent : SkillComponent
{
    private Unit target;

    public InstantComponent(InstanceValue inst, Unit target)
    {
        damage = inst.damageFinal;
        this.target = target;
    }

    public override void OnInitialize(SkillLauncher launcher, SkillParticleController particle)
    {
        base.OnInitialize(launcher, particle);
        OnHit(target);
    }

    public override void OnUpdate(float deltaTime) { }
    public override void OnHit(Unit target)
    {
        ApplyDamage(target);
        launcher.Deactivate();
    }
}

/// <summary>
/// 도약 이동 컴포넌트
/// </summary>
public class LeapComponent : SkillComponent
{
    private SkillIndicator startIndicator;
    private SkillIndicator finalIndicator;
    private Vector3 startPos;
    private Vector3 targetPos;
    private Vector3 casterAdjustedPos;
    private float time;
    private AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    private bool isLeapCompleted = false;

    private const float LEAP_DURATION = 0.6f;
    private const float LEAP_INV_DURATION = 1f / LEAP_DURATION;

    public LeapComponent(Vector3 targetPos)
    {
        this.targetPos = targetPos;
        isLeapCompleted = false;
        time = 0f;
    }

    public override void OnInitialize(SkillLauncher launcher, SkillParticleController particle)
    {
        base.OnInitialize(launcher, particle);

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
        float p = Mathf.Clamp01(time * LEAP_INV_DURATION);
        startIndicator.UpdateIndicatorScale(p);

        // 도약 이동
        float e = curve.Evaluate(p);
        Vector3 newPos = Vector3.Lerp(startPos, targetPos, e);
        newPos.y = Mathf.Sin(p * Mathf.PI);

        launcher.SetTransform(newPos, launcher.Direction);
        launcher.Caster.transform.position = newPos + casterAdjustedPos;

        if (p >= 1f)
        {
            isLeapCompleted = true;
            ActivateAOEComponents();
            GameMgr.Instance.skillMgr.RemoveIndicator(startIndicator);
            GameMgr.Instance.skillMgr.RemoveIndicator(finalIndicator);
        }
    }

    private void ActivateAOEComponents()
    {
        foreach (SkillComponent component in launcher.Components)
        {
            if (component is AOEComponent aoeComponent)
                aoeComponent.SetCanAction(true);
        }
    }

    public override void OnHit(Unit target) { }
}

public class BeamComponent : SkillComponent
{
    private float length;
    private float damageTick;
    private float skillDuration;

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
    public override bool IsWaitAction => true;

    public BeamComponent(InstanceValue inst, Vector3 targetPos)
    {
        damage = inst.damageFinal;
        damageTick = inst.damageTickFinal;
        length = GameValue.PROJECTILE_MAX_LENGTH;
        skillDuration = inst.durationFinal;

        isIndicatorTime = true;
        this.targetPos = targetPos;
        indicatorDuration = 0.4f;
        invIndicatorDuration = 1f / indicatorDuration;
        tickTime = 0f;
        time = 0f;
    }

    public override void OnInitialize(SkillLauncher launcher, SkillParticleController particle)
    {
        base.OnInitialize(launcher, particle);
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

            beamParticle = particleController.GetComponent<BeamParticle>();
            beamParticle.Init(direction, length);
        }
    }

    private void UpdateSkill(float deltaTime)
    {
        if (isIndicatorTime)
            return;

        beamParticle.UpdateBeam();

        if (time < skillDuration)
        {
            tickTime += deltaTime;
            if (tickTime < damageTick)
                return;

            tickTime -= damageTick;

            if (Physics.Raycast(startPos, direction, out RaycastHit hit, GameValue.PROJECTILE_MAX_LENGTH, GameValue.UNIT_LAYERS))
                OnHit(hit.collider.GetComponent<Unit>());
        }

        else
        {
            beamParticle.DisableBeam();
        }
    }

    public override void OnHit(Unit target)
    {
        if (!IsHittable(target))
            return;


        ApplyDamage(target);
    }
}

public class FreezeItemEffect : SkillComponent
{
    private float time;
    private List<Unit> monsters = new();

    private const float SLOW_PERCENT = -0.5f;
    private const float DURATION = 5f;

    public override void OnInitialize(SkillLauncher launcher, SkillParticleController particle)
    {
        base.OnInitialize(launcher, particle);
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

        launcher.Deactivate();
    }

    public override void OnHit(Unit target) { }
    public override void OnDestroy() { }
}

public class ExplosionItemEffect : SkillComponent
{
    private const float EXPLOSION_RADIUS = 20f;
    private const float EXPLOSION_DAMAGE = 100f;

    public ExplosionItemEffect()
    {
        damage = EXPLOSION_DAMAGE;
    }

    public override void OnInitialize(SkillLauncher launcher, SkillParticleController particle)
    {
        base.OnInitialize(launcher, particle);

        List<Unit> targets = GetHitTargetsBySphere(launcher.Position, EXPLOSION_RADIUS);
        foreach (Unit target in targets)
        {
            OnHit(target);
        }

        launcher.Deactivate();
    }

    public override void OnUpdate(float deltaTime) { }
    public override void OnHit(Unit target) => ApplyDamage(target);
    public override void OnDestroy() { }
}