using UnityEngine;
using System;
using System.Collections;

public class SkillParticleController : MonoBehaviour
{
    [SerializeField] private ParticleSystem mainParticle;
    [SerializeField] private ParticleSystem[] subParticles;

    private bool isPlaying = false;
    public event Action OnParticleFinished;

    public void Reset()
    {
        isPlaying = false;
        OnParticleFinished = null;
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;

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