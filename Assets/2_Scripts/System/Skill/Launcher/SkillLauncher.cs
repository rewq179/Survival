using UnityEngine;
using System.Collections.Generic;

public enum SkillLauncherType
{
    Projectile,
    InstantAOE,
    PersistentAOE,
    InstantAttack,
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
    protected bool isActive = false;
    protected float elapsedTime;

    protected List<ISkillEffect> skillEffects = new();
    protected SkillParticleController particleController;

    public bool IsActive => isActive;
    public Vector3 Position => transform.position;
    public Unit Caster => caster;
    public bool IsAffectCaster => isAffectCaster;
    public Vector3 Direction => direction;

    public void Reset()
    {
        isActive = false;
        elapsedTime = 0f;
        skillEffects.Clear();
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

        for (int i = skillEffects.Count - 1; i >= 0; i--)
        {
            if (skillEffects[i] != null)
            {
                skillEffects[i].OnUpdate(Time.deltaTime);
            }
        }

        if (particleController == null && duration > 0f && elapsedTime >= duration)
        {
            Deactivate();
        }
    }

    public virtual void Initialize(SkillInstance skillInstance, Vector3 startPos, Vector3 dir, SkillParticleController particleController,
        Unit caster, Unit fixedTarget = null)
    {
        skillKey = skillInstance.skillKey;
        startPosition = startPos;
        direction = dir.normalized;
        this.caster = caster;
        this.particleController = particleController;
        isActive = true;
        elapsedTime = 0f;
        isAffectCaster = false;
        transform.rotation = Quaternion.LookRotation(direction);
        transform.position = startPos;
        gameObject.SetActive(true);

        // 스킬 데이터 기반으로 효과들 자동 추가
        SkillData skillData = DataMgr.GetSkillData(skillInstance.skillKey);
        SetupSkillEffects(skillData, fixedTarget);

        if (particleController != null)
        {
            particleController.OnParticleFinished += OnParticleFinished;
            particleController.Play();
        }
    }

    private void SetupSkillEffects(SkillData skillData, Unit fixedTarget)
    {
        skillEffects.Clear();

        for (int i = 0; i < skillData.skillElements.Count; i++)
        {
            switch (skillData.launcherType)
            {
                case SkillLauncherType.Projectile:
                    skillEffects.Add(new ProjectileMovementEffect(skillData, i));
                    break;

                case SkillLauncherType.InstantAOE:
                    skillEffects.Add(new AOEDamageEffect(skillData, i));
                    break;

                case SkillLauncherType.PersistentAOE:
                    skillEffects.Add(new PeriodicDamageEffect(skillData, i));
                    break;

                case SkillLauncherType.InstantAttack:
                    skillEffects.Add(new InstantAttackEffect(skillData, i, fixedTarget));
                    break;
            }

            skillEffects[i].OnInitialize(this);
        }
    }

    public void Deactivate()
    {
        if (!isActive)
            return;

        isActive = false;

        for (int i = skillEffects.Count - 1; i >= 0; i--)
        {
            if (skillEffects[i] != null)
            {
                skillEffects[i].OnDestroy();
            }
        }

        // 파티클 정지 및 이벤트 구독 해제
        if (particleController != null)
        {
            particleController.OnParticleFinished -= OnParticleFinished;
            particleController.Stop();
        }

        GameMgr.Instance.skillMgr.PushParticle(skillKey, particleController);
        GameMgr.Instance.skillMgr.RemoveLauncher(this);
    }

    private void OnParticleFinished()
    {
        Deactivate();
    }
}