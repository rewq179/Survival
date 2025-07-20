using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class SkillParticleController : MonoBehaviour
{
    [Header("Main")]
    [SerializeField] private List<ParticleSystem> playParticles = new();

    [Header("Hitted")]
    [SerializeField] private List<ParticleSystem> hittedParticles = new();
    private List<float> hittedInvDurations = new();

    // 파티클
    private bool isPlaying = false;
    private Coroutine particleCheckCoroutine;
    public event Action OnParticleFinished;

    public void Reset()
    {
        isPlaying = false;
        OnParticleFinished = null;

        if (particleCheckCoroutine != null)
        {
            StopCoroutine(particleCheckCoroutine);
            particleCheckCoroutine = null;
        }

        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;

        StopAllParticles();
        gameObject.SetActive(false);
    }

    public void Play()
    {
        if (isPlaying)
            return;

        isPlaying = true;
        gameObject.SetActive(true);
        particleCheckCoroutine = StartCoroutine(CheckParticleFinished());

        foreach (ParticleSystem particle in playParticles)
        {
            particle.Play();
        }
    }

    public void PlayHit()
    {
        bool addInvDuration = hittedInvDurations.Count == 0;

        foreach (ParticleSystem particle in hittedParticles)
        {
            particle.Play();

            if (addInvDuration)
                hittedInvDurations.Add(1f / particle.main.duration);
        }
    }

    private void StopAllParticles()
    {
        if (particleCheckCoroutine != null)
        {
            StopCoroutine(particleCheckCoroutine);
            particleCheckCoroutine = null;
        }

        StopMain();
        StopHitted();
    }

    public void StopMain()
    {
        foreach (ParticleSystem particle in playParticles)
        {
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    public void StopHitted()
    {
        foreach (ParticleSystem particle in hittedParticles)
        {
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private IEnumerator CheckParticleFinished()
    {
        bool IsMainPlaying(List<ParticleSystem> particles)
        {
            foreach (ParticleSystem particle in particles)
            {
                if (particle.isPlaying)
                    return true;
            }
            return false;
        }

        bool IsHittedPlaying(List<ParticleSystem> particles)
        {
            for (int i = 0; i < particles.Count; i++)
            {
                ParticleSystem particle = particles[i];
                if (particle.isPlaying && particle.time * hittedInvDurations[i] < 0.5f)
                    return true;
            }

            return false;
        }

        while (IsMainPlaying(playParticles) || IsHittedPlaying(hittedParticles))
        {
            yield return null;
        }

        OnParticleFinished?.Invoke();
    }
}