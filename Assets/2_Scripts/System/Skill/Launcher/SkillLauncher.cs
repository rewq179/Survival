using UnityEngine;
using System.Collections.Generic;

public enum SkillLauncherType
{
    Projectile,
    InstantAOE,
    PeriodicAOE,
    InstantAttack,
    Beam,
    Max,
}

public class SkillLauncher : MonoBehaviour
{
    protected SkillKey skillKey;
    protected float duration;
    protected bool isAffectCaster;

    protected Vector3 startPosition;
    protected Vector3 direction;
    protected Unit caster;
    protected bool isActive;
    protected float elapsedTime;

    protected List<SkillComponent> components = new();
    protected SkillParticleController particleController;

    public SkillKey SkillKey => skillKey;
    public bool IsActive => isActive;
    public Unit Caster => caster;
    public bool IsAffectCaster => isAffectCaster;
    public Vector3 Position => transform.position;
    public Vector3 Direction => direction;
    public List<SkillComponent> Components => components;

    public void Reset()
    {
        isActive = false;
        elapsedTime = 0f;
        components.Clear();
        particleController = null;
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        gameObject.SetActive(false);
    }

    protected virtual void Update()
    {
        if (!isActive)
            return;

        elapsedTime += Time.deltaTime;

        for (int i = components.Count - 1; i >= 0; i--)
        {
            if (components[i] != null)
            {
                components[i].OnUpdate(Time.deltaTime);
            }
        }

        if (particleController == null && duration > 0f && elapsedTime >= duration)
        {
            Deactivate();
        }
    }

    public void Init(SkillInstance inst, Vector3 startPos, Vector3 dir, SkillParticleController particleController,
        Unit caster, Unit fixedTarget = null)
    {
        skillKey = inst.skillKey;
        Init(caster, startPos, dir);
        this.particleController = particleController;

        // 스킬 데이터 기반으로 효과들 자동 추가
        bool isWaitAction = inst.skillKey == SkillKey.HitGroundAttack;
        SetupComponents(inst, fixedTarget, isWaitAction);
    }

    public void Init(Unit caster, Vector3 position, Vector3 direction)
    {
        this.caster = caster;
        startPosition = position;
        this.direction = direction.normalized;
        SetTransform(position, direction);
        isActive = true;
        isAffectCaster = false;
        elapsedTime = 0f;
        gameObject.SetActive(true);
    }

    public void SetTransform(Vector3 startPos, Vector3 dir)
    {
        transform.position = startPos;
        transform.rotation = Quaternion.LookRotation(dir);
    }

    private void SetupComponents(SkillInstance inst, Unit fixedTarget, bool isWaitAction)
    {
        components.Clear();

        foreach (InstanceValue instValue in inst.Values)
        {
            SkillComponent component = instValue.launcherType switch
            {
                SkillLauncherType.Projectile => new ProjectileComponent(instValue),
                SkillLauncherType.InstantAOE => new AOEComponent(instValue, isWaitAction),
                SkillLauncherType.PeriodicAOE => new PeriodicAOEComponent(instValue),
                SkillLauncherType.InstantAttack => new InstantComponent(instValue, fixedTarget),
                SkillLauncherType.Beam => new BeamComponent(instValue, fixedTarget.transform.position),
                _ => null,
            };

            if (component != null)
                AddSkillComponent(component);
        }

        switch (inst.skillKey)
        {
            case SkillKey.HitGroundAttack:
                LeapComponent component = new LeapComponent(fixedTarget.transform.position);
                AddSkillComponent(component);
                break;
        }
    }

    public void AddSkillComponent(SkillComponent component)
    {
        components.Add(component);
        component.OnInitialize(this, particleController);
    }

    public void Deactivate()
    {
        if (!isActive)
            return;

        isActive = false;

        for (int i = components.Count - 1; i >= 0; i--)
        {
            if (components[i] != null)
            {
                components[i].OnDestroy();
            }
        }

        if (particleController != null)
            GameMgr.Instance.skillMgr.PushParticle(skillKey, particleController);
        GameMgr.Instance.skillMgr.RemoveLauncher(this);
    }
}