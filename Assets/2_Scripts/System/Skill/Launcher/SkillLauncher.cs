using UnityEngine;
using System.Collections.Generic;

public enum SkillLauncherType
{
    Projectile,
    InstantAOE,
    PersistentAOE,
    Max,
}

public abstract class SkillLauncher : MonoBehaviour
{
    public abstract SkillLauncherType Type { get; }
    protected float duration;
    protected float range;
    protected bool isAffectCaster;

    protected Vector3 startPosition;
    protected Vector3 direction;
    protected Unit caster;
    protected bool isActive = false;
    protected float elapsedTime;

    protected List<ISkillEffect> skillEffects = new();
    protected SkillParticleController particleController;

    public bool IsActive => isActive;
    public Vector3 Position => transform.position;
    public Unit Caster => caster;
    public float Range => range;
    public bool IsAffectCaster => isAffectCaster;

    public virtual void Initialize(SkillData skillData, Vector3 startPos, Vector3 dir, Unit caster, SkillParticleController particleController)
    {
        startPosition = startPos;
        direction = dir.normalized;
        this.caster = caster;
        this.particleController = particleController;
        isActive = true;
        elapsedTime = 0f;
        range = skillData.elements[0].length;
;
        transform.position = startPos;
        transform.rotation = Quaternion.LookRotation(direction);
        gameObject.SetActive(true);

        OnInitialize();

        if (particleController != null)
        {
            particleController.OnParticleFinished += OnParticleFinished;
            particleController.gameObject.SetActive(true);
            particleController.Play();
        }
    }

    public void Reset()
    {
        isActive = false;
        elapsedTime = 0f;
        skillEffects.Clear();

        if (particleController != null)
            particleController.OnParticleFinished -= OnParticleFinished;

        particleController = null;
        gameObject.SetActive(false);
    }

    protected virtual void Update()
    {
        if (!isActive)
            return;

        elapsedTime += Time.deltaTime;

        UpdateMovement();

        foreach (var effect in skillEffects)
        {
            effect.OnUpdate(Time.deltaTime);
        }

        if (particleController == null && duration > 0f && elapsedTime >= duration)
        {
            Deactivate();
        }
    }

    protected abstract void UpdateMovement();
    protected virtual void OnInitialize() { }
    protected virtual void OnDeactivate() { }

    public virtual void Deactivate()
    {
        if (!isActive)
            return;

        isActive = false;

        foreach (ISkillEffect effect in skillEffects)
        {
            effect.OnDestroy();
        }

        // 파티클 정지 및 이벤트 구독 해제
        if (particleController != null)
        {
            particleController.OnParticleFinished -= OnParticleFinished;
            particleController.Stop();
        }

        OnDeactivate();
        GameManager.Instance.skillManager.PushLauncher(this);
    }

    public void AddEffect(ISkillEffect effect)
    {
        skillEffects.Add(effect);
        effect.OnInitialize(this);
    }

    private void OnParticleFinished()
    {
        Deactivate();
    }

    public virtual void OnHitTarget(Unit target)
    {
        if (target == null || target == caster)
            return;

        foreach (var effect in skillEffects)
        {
            effect.OnHit(target);
        }
    }

    public List<Unit> GetHitTargets(float radius, bool isAffectCaster)
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, radius, GameValue.UNIT_LAYERS);

        List<Unit> hitTargets = new();
        foreach (Collider col in colliders)
        {
            Unit target = col.GetComponent<Unit>();
            if (target != null && (isAffectCaster || target != caster))
            {
                hitTargets.Add(target);
            }
        }

        return hitTargets;
    }
}