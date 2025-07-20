using System;
using UnityEngine;

public class SkillEffectController : MonoBehaviour
{
    [SerializeField] private SkillCollision collision;
    [SerializeField] private SkillParticle particle;

    public void Reset()
    {
        collision?.Reset();
        particle.Reset();
    }

    public void Init()
    {
        collision?.Init();
        particle.Init();
    }

    public void Play() => particle.Play();
    public void PlayHit() => particle.PlayHit();
    public void StopMain() => particle.StopMain();
    public void StopHitted() => particle.StopHitted();
    
    public void SubscribeHitTarget(Action<Unit> onHitTarget)
    {
        if (collision != null)
            collision.OnHitTarget += onHitTarget;
    }

    public void SubscribeParticleFinished(Action onParticleFinished)
    {
        particle.OnParticleFinished += onParticleFinished;
    }
}
