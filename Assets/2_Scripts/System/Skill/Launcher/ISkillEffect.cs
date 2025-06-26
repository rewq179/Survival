using UnityEngine;
using System.Collections.Generic;

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
/// 직접 데미지 효과
/// </summary>
public class DirectDamageEffect : ISkillEffect
{
    private float damage;
    private SkillLauncher launcher;

    public DirectDamageEffect(float damage)
    {
        this.damage = damage;
    }

    public void OnInitialize(SkillLauncher launcher) => this.launcher = launcher;
    public void OnUpdate(float deltaTime) { }

    public void OnHit(Unit target)
    {
        target.TakeDamage(damage);
    }

    public void OnDestroy() { }
}

/// <summary>
/// 주기적 데미지 효과
/// </summary>
public class PeriodicDamageEffect : ISkillEffect
{
    private float damage;
    private float interval;
    private float lastDamageTime;
    private SkillLauncher launcher;

    public PeriodicDamageEffect(float damage, float interval)
    {
        this.damage = damage;
        this.interval = interval;
    }

    public void OnInitialize(SkillLauncher launcher) => this.launcher = launcher;

    public void OnUpdate(float deltaTime)
    {
        if (Time.time - lastDamageTime >= interval)
        {
            List<Unit> targets = launcher.GetHitTargets(launcher.Range, launcher.IsAffectCaster);
            foreach (Unit target in targets)
            {
                target.TakeDamage(damage);
            }

            lastDamageTime = Time.time;
        }
    }

    public void OnHit(Unit target) { }
    public void OnDestroy() { }
}

/// <summary>
/// 주변 폭발 효과 (발사체용)
/// </summary>
public class TrailExplosionEffect : ISkillEffect
{
    private float explosionRadius;
    private float explosionDamage;
    private float interval;
    private float lastExplosionTime;
    private SkillLauncher launcher;

    public TrailExplosionEffect(float radius, float damage, float interval)
    {
        explosionRadius = radius;
        explosionDamage = damage;
        this.interval = interval;
    }

    public void OnInitialize(SkillLauncher launcher) => this.launcher = launcher;

    public void OnUpdate(float deltaTime)
    {
        if (Time.time - lastExplosionTime >= interval)
        {
            CreateExplosion();
            lastExplosionTime = Time.time;
        }
    }

    private void CreateExplosion()
    {
        Collider[] hits = Physics.OverlapSphere(launcher.Position, explosionRadius, GameValue.UNIT_LAYERS);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out Unit unit) && unit != launcher.Caster)
            {
                unit.TakeDamage(explosionDamage);
            }
        }
    }

    public void OnHit(Unit target) { }
    public void OnDestroy() { }
}

/// <summary>
/// 최종 폭발 효과
/// </summary>
public class FinalExplosionEffect : ISkillEffect
{
    private float explosionRadius;
    private float explosionDamage;
    private SkillLauncher launcher;

    public FinalExplosionEffect(float radius, float damage)
    {
        explosionRadius = radius;
        explosionDamage = damage;
    }

    public void OnInitialize(SkillLauncher launcher) => this.launcher = launcher;
    public void OnUpdate(float deltaTime) { }
    public void OnHit(Unit target) { }

    public void OnDestroy()
    {
        // 런처가 파괴될 때 최종 폭발
        Collider[] hits = Physics.OverlapSphere(launcher.Position, explosionRadius, GameValue.UNIT_LAYERS);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out Unit unit) && unit != launcher.Caster)
            {
                unit.TakeDamage(explosionDamage);
            }
        }
    }
}