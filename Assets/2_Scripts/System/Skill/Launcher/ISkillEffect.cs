using System.Collections.Generic;
using UnityEngine;

/// <summary> 이펙트 실행 시점 </summary>
public enum EffectTiming
{
    OnHit,      // 피격 시 실행
    OnUpdate,   // 지속적으로 실행
}

/// <summary> 스킬 효과 인터페이스 </summary>
public interface ISkillEffect
{
    void Reset();
    void OnApply(EffectTiming timing, Unit target);
    void OnUpdate(EffectTiming timing, float deltaTime);
}

/// <summary> 데미지 효과 </summary>
public class DamageEffect : ISkillEffect
{
    private float damage;
    private Unit caster;
    private SkillKey skillKey;

    public DamageEffect(SkillLauncher launcher, float damage)
    {
        this.damage = damage;
        this.caster = launcher.Caster;
        this.skillKey = launcher.SkillKey;
    }

    public void Reset() { }

    public void OnApply(EffectTiming timing, Unit target)
    {
        if (timing != EffectTiming.OnHit)
            return;

        Vector3 hitPoint = target.transform.position;
        CombatMgr.ApplyDamageBySkill(caster, target, damage, hitPoint, skillKey);
    }

    public void OnUpdate(EffectTiming timing, float deltaTime) { }

    public void SetDamage(float damage) => this.damage = damage;
}

/// <summary> 버프 부여 효과 </summary>
public class BuffEffect : ISkillEffect
{
    private BuffKey buffKey;
    private Unit caster;

    public BuffEffect(SkillLauncher launcher, BuffKey buffKey)
    {
        this.caster = launcher.Caster;
        this.buffKey = buffKey;
    }

    public void Reset() { }

    public void OnApply(EffectTiming timing, Unit target)
    {
        if (timing != EffectTiming.OnHit)
            return;

        target.AddBuff(buffKey, caster);
    }

    public void OnUpdate(EffectTiming timing, float deltaTime) { }
}

/// <summary> 일정 시간 스턴 부여 효과 </summary>
public class StunEffect : ISkillEffect
{
    private Unit caster;
    private float duration;
    private Dictionary<Unit, float> stunData = new();

    public StunEffect(SkillLauncher launcher, float duration)
    {
        this.caster = launcher.Caster;
        this.duration = duration;
    }

    public void Reset()
    {
        foreach (var pair in stunData)
        {
            pair.Key.RemoveBuff(BuffKey.Stun);
        }
        stunData.Clear();
    }

    public void OnApply(EffectTiming timing, Unit target)
    {
        if (timing != EffectTiming.OnHit)
            return;

        if (stunData.ContainsKey(target))
            return;

        target.AddBuff(BuffKey.Stun, caster);
        stunData[target] = duration;
    }

    public void OnUpdate(EffectTiming timing, float deltaTime)
    {
        if (timing != EffectTiming.OnUpdate)
            return;

        List<Unit> completedUnits = new();
        List<Unit> updateUnits = new();

        foreach (var pair in stunData)
        {
            Unit unit = pair.Key;
            if (unit.IsDead)
            {
                completedUnits.Add(unit);
            }
            else
            {
                updateUnits.Add(unit);
            }
        }

        foreach (Unit unit in updateUnits)
        {
            stunData[unit] -= deltaTime;
            if (stunData[unit] <= 0f)
            {
                unit.RemoveBuff(BuffKey.Stun);
                completedUnits.Add(unit);
            }
        }

        foreach (Unit unit in completedUnits)
        {
            stunData.Remove(unit);
        }
    }
}

/// <summary> 수직 이동 효과 </summary>
public class VerticalMovementEffect : ISkillEffect
{
    private float duration;
    private const float RISE_FORCE = 2.5f;
    private Dictionary<Unit, MovementData> movementData = new();

    public VerticalMovementEffect(float duration)
    {
        this.duration = duration;
    }

    public void Reset()
    {
        foreach (var pair in movementData)
        {
            Unit unit = pair.Key;
            unit.transform.position = pair.Value.startPos;
            MovementDataPool.PushMovementData(pair.Value);
        }
        movementData.Clear();
    }

    public void OnApply(EffectTiming timing, Unit target)
    {
        if (timing != EffectTiming.OnHit)
            return;

        if (movementData.ContainsKey(target))
            return;

        movementData[target] = MovementDataPool.CreateMovementData(target, Vector3.up, duration);
    }

    public void OnUpdate(EffectTiming timing, float deltaTime)
    {
        if (timing != EffectTiming.OnUpdate)
            return;

        List<Unit> completedUnits = new();

        foreach (var pair in movementData)
        {
            Unit unit = pair.Key;
            if (unit.IsDead)
            {
                completedUnits.Add(unit);
                continue;
            }

            MovementData data = pair.Value;
            data.remainingTime -= deltaTime;

            if (data.remainingTime <= 0f)
            {
                unit.transform.position = data.startPos;
                completedUnits.Add(unit);
            }

            else
            {
                float p = 1f - (data.remainingTime * data.totalInvTime);
                float offsetY = Mathf.Sin(p * Mathf.PI) * RISE_FORCE;
                Vector3 pos = data.startPos;
                pos.y += offsetY;
                unit.transform.position = pos;
            }
        }

        foreach (Unit unit in completedUnits)
        {
            MovementDataPool.PushMovementData(movementData[unit]);
            movementData.Remove(unit);
        }
    }
}

/// <summary> 넉백 효과 </summary>
public class KnockbackEffect : ISkillEffect
{
    private Unit caster;
    private float knockbackForce;
    private float duration;
    private Dictionary<Unit, MovementData> knockbackData = new();

    public KnockbackEffect(SkillLauncher launcher, float knockbackForce, float duration)
    {
        this.caster = launcher.Caster;
        this.knockbackForce = knockbackForce;
        this.duration = duration;
    }

    public void Reset()
    {
        foreach (var pair in knockbackData)
        {
            pair.Key.RemoveBuff(BuffKey.Stun);
            MovementDataPool.PushMovementData(pair.Value);
        }
        knockbackData.Clear();
    }

    public void OnApply(EffectTiming timing, Unit target)
    {
        if (timing != EffectTiming.OnHit)
            return;

        if (knockbackData.ContainsKey(target))
            return;

        Vector3 targetPos = target.transform.position;
        Vector3 direction = (targetPos - caster.transform.position).normalized;
        direction.y = 0f;

        knockbackData[target] = MovementDataPool.CreateMovementData(target, direction, duration);
        target.AddBuff(BuffKey.Stun, caster);
    }

    public void OnUpdate(EffectTiming timing, float deltaTime)
    {
        if (timing != EffectTiming.OnUpdate)
            return;

        List<Unit> completedUnits = new();

        foreach (var pair in knockbackData)
        {
            Unit unit = pair.Key;
            if (unit.IsDead)
            {
                completedUnits.Add(unit);
                continue;
            }

            MovementData data = pair.Value;
            data.remainingTime -= deltaTime;
            if (data.remainingTime <= 0f)
            {
                completedUnits.Add(unit);
            }

            else
            {
                // 넉백 거리 계산 (시간에 따른 감쇠)
                float p = 1f - (data.remainingTime * data.totalInvTime);
                float force = knockbackForce * (1f - p);

                // 넉백 적용
                Vector3 knockbackMovement = data.direction * force * deltaTime;
                unit.transform.position += knockbackMovement;
            }
        }

        foreach (Unit unit in completedUnits)
        {
            unit.RemoveBuff(BuffKey.Stun);
            MovementDataPool.PushMovementData(knockbackData[unit]);
            knockbackData.Remove(unit);
        }
    }
}

/// <summary> 중력 효과 </summary>
public class GravityEffect : ISkillEffect
{
    private Unit caster;
    private Vector3 startPos;
    private float duration;
    private float radius;
    private float spiralSpeed;
    private Dictionary<Unit, MovementData> gravityData = new();
    private static Stack<MovementData> gravityPools = new();

    public GravityEffect(SkillLauncher launcher, float duration, float radius, float spiralSpeed)
    {
        this.caster = launcher.Caster;
        this.startPos = launcher.Position;
        this.duration = duration;
        this.radius = radius;
        this.spiralSpeed = spiralSpeed;
    }

    public void Reset()
    {
        foreach (var pair in gravityData)
        {
            PushMovementData(pair.Value);
        }
        gravityData.Clear();
    }

    public void OnApply(EffectTiming timing, Unit target)
    {
        if (timing != EffectTiming.OnHit)
            return;

        if (gravityData.ContainsKey(target))
            return;

        // 나선형 이동을 위한 초기 각도 계산
        Vector3 direction = (target.transform.position - startPos).normalized;
        float angle = Mathf.Atan2(direction.z, direction.x) * Mathf.Rad2Deg;
        gravityData[target] = CreateMovementData(target, direction, duration, angle);
    }

    public void OnUpdate(EffectTiming timing, float deltaTime)
    {
        if (timing != EffectTiming.OnUpdate)
            return;

        List<Unit> completedUnits = new();

        foreach (var pair in gravityData)
        {
            Unit unit = pair.Key;
            if (unit.IsDead)
            {
                completedUnits.Add(unit);
                continue;
            }

            MovementData data = pair.Value;
            data.remainingTime -= deltaTime;

            if (data.remainingTime <= 0f)
            {
                completedUnits.Add(unit);
            }

            else
            {
                // 나선형 중력 이동
                float p = 1f - (data.remainingTime * data.totalInvTime);

                // 각도 계산 (나선형)
                float currentAngle = data.startAngle + spiralSpeed * p * 360f;
                float radian = currentAngle * Mathf.Deg2Rad;

                // 중심간 거리 계산
                float currentRadius = radius * Mathf.Pow(1f - p, 2f);

                Vector3 pos = startPos + new Vector3(
                    Mathf.Cos(radian) * currentRadius,
                    unit.transform.position.y,
                    Mathf.Sin(radian) * currentRadius
                );

                unit.transform.position = pos;
            }
        }

        foreach (Unit unit in completedUnits)
        {
            Vector3 direction = (caster.transform.position - unit.transform.position).normalized;
            direction.y = 0f;
            unit.transform.rotation = Quaternion.LookRotation(direction);

            PushMovementData(gravityData[unit]);
            gravityData.Remove(unit);
        }
    }

    private MovementData CreateMovementData(Unit target, Vector3 direct, float duration, float startAngle)
    {
        MovementData data = PopMovementData();
        data.Init(target.transform.position, direct, duration);
        data.startAngle = startAngle;
        return data;
    }

    private void PushMovementData(MovementData data)
    {
        gravityPools.Push(data);
    }

    private MovementData PopMovementData()
    {
        if (!gravityPools.TryPop(out MovementData data))
            data = new MovementData();

        return data;
    }
}

/// <summary> 이동 효과 </summary>
public class MovementEffect : ISkillEffect
{
    private Transform launcher;
    private Vector3 direction;
    private float speed;

    public MovementEffect(float speed, Vector3 direction, Transform launcher)
    {
        this.speed = speed;
        this.direction = direction;
        this.launcher = launcher;
    }

    public void SetSpeed(float speed) => this.speed = speed;
    public void SetDirection(Vector3 direction) => this.direction = direction;

    public void Reset() { }

    public void OnApply(EffectTiming timing, Unit target) { }

    public void OnUpdate(EffectTiming timing, float deltaTime)
    {
        float moveDistance = speed * deltaTime;
        launcher.position += direction * moveDistance;
    }
}

public class RotationEffect : ISkillEffect
{
    private Transform launcher;
    private float rotationSpeed;
    private float currentAngle;

    public RotationEffect(SkillLauncher launcher, float rotationSpeed)
    {
        this.rotationSpeed = rotationSpeed;
        this.launcher = launcher.transform;
    }

    public void SetCurrentAngle(float angle) => currentAngle = angle;

    public void Reset() { }

    public void OnApply(EffectTiming timing, Unit target) { }

    public void OnUpdate(EffectTiming timing, float deltaTime)
    {
        if (timing != EffectTiming.OnUpdate)
            return;

        currentAngle += rotationSpeed * deltaTime;
        launcher.transform.rotation = Quaternion.Euler(0f, currentAngle, 0f);
    }
}

/// <summary> 이동 데이터 클래스 </summary>
public class MovementData
{
    public Vector3 startPos;
    public Vector3 direction;
    public float remainingTime;
    public float totalInvTime;
    public float startAngle; // 나선형 이동을 위한 각도 정보

    public void Init(Vector3 startPos, Vector3 direction, float totalTime)
    {
        this.startPos = startPos;
        this.direction = direction;
        this.remainingTime = totalTime;
        this.totalInvTime = 1f / totalTime;
        this.startAngle = 0f;
    }
}

public static class MovementDataPool
{
    private static readonly Stack<MovementData> pool = new();

    public static void PushMovementData(MovementData data)
    {
        if (data == null)
            return;

        pool.Push(data);
    }

    public static MovementData CreateMovementData(Unit target, Vector3 direct, float duration)
    {
        MovementData data = PopMovementData();
        data.Init(target.transform.position, direct, duration);
        return data;
    }

    private static MovementData PopMovementData()
    {
        if (!pool.TryPop(out MovementData data))
            data = new MovementData();

        return data;
    }
}