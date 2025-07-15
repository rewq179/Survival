using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class SkillParticleController : MonoBehaviour
{
    [SerializeField] private ParticleSystem mainParticle;
    [SerializeField] private List<ParticleSystem> subParticles = new();

    private bool isPlaying = false;
    public event Action OnParticleFinished;

    public void Reset()
    {
        isPlaying = false;
        OnParticleFinished = null;
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;

        if (mainParticle != null)
            mainParticle.Stop();

        foreach (ParticleSystem particle in subParticles)
        {
            particle.Stop();
        }

        gameObject.SetActive(false);
    }

    public void Play()
    {
        if (isPlaying)
            return;

        isPlaying = true;
        gameObject.SetActive(true);

        mainParticle.Play();
        StartCoroutine(CheckParticleFinished(mainParticle));

        foreach (ParticleSystem particle in subParticles)
            particle.Play();
    }

    public void Stop()
    {
        if (!isPlaying)
            return;

        isPlaying = false;

        mainParticle.Stop();
        foreach (ParticleSystem particle in subParticles)
            particle.Stop();
    }

    private IEnumerator CheckParticleFinished(ParticleSystem particle)
    {
        while (particle.isPlaying)
        {
            yield return null;
        }

        OnParticleFinished?.Invoke();
    }
}