using System;
using UnityEngine;

public class SkillEffectController : MonoBehaviour
{
    [Header("Skill Effect Controller")]
    [SerializeField] private SkillCollision collision;
    [SerializeField] private SkillParticle particle;

    [Header("Sub Particles")]
    [SerializeField] private BeamParticle beamParticle;

    public void Reset()
    {
        collision?.Reset();
        particle.Reset();
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        gameObject.SetActive(false);
    }

    public void Init(SkillLauncher launcher, Action<Unit> onHitTarget, Action onParticleFinished)
    {
        if (collision != null) // 콜라이더
        {
            collision.Init();
            collision.OnHitTarget += onHitTarget;
        }

        // 파티클
        particle.Init();
        particle.OnParticleFinished += onParticleFinished;

        launcher.SetParticleFinished(false);
        transform.position = launcher.Position;
        gameObject.SetActive(true);
    }

    public void SetPosition(Vector3 position) => transform.position = position;
    public void Play() => particle.Play();
    public void PlayHit() => particle.PlayHit();
    public void StopMain() => particle.StopMain();
    public void StopHitted() => particle.StopHitted();

    // Sub Particles
    public BeamParticle GetBeamParticle() => beamParticle;
}
