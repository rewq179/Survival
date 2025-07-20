using NUnit.Framework;
using UnityEngine;

public class BeamParticle : MonoBehaviour
{
    [Header("Beam Settings")]
    public GameObject hitEffect;
    public float hitOffset = 0.01f;
    public bool useBeamRotation = false;
    private float maxLength;

    [Header("Texture Settings")]
    public float mainTextureLength = 0.25f;
    public float noiseTextureLength = 0.3f;

    [Header("Components")]
    [SerializeField] private LineRenderer beamRenderer;
    private ParticleSystem[] beamEffects;
    private ParticleSystem[] hitEffects;

    private Vector3 direction;
    private Vector2 textMainLength = new Vector2(1, 1);
    private Vector2 textNoiseLength = new Vector2(1, 1);

    private void Awake()
    {
        beamRenderer = GetComponent<LineRenderer>();
        beamEffects = GetComponentsInChildren<ParticleSystem>();
        hitEffects = hitEffect.GetComponentsInChildren<ParticleSystem>();
    }

    public void Init(Vector3 direction, float length)
    {
        this.direction = direction;
        maxLength = length;
        gameObject.SetActive(true);
    }

    public void DisableBeam()
    {
        beamRenderer.enabled = false;
        StopEffect(beamEffects);
        StopEffect(hitEffects);
    }

    public void UpdateBeam()
    {
        beamRenderer.material.SetTextureScale("_MainTex", textMainLength);
        beamRenderer.material.SetTextureScale("_Noise", textNoiseLength);
        beamRenderer.SetPosition(0, transform.position);

        if (Physics.Raycast(transform.position, direction, out RaycastHit hit, maxLength))
        {
            beamRenderer.SetPosition(1, hit.point);

            hitEffect.transform.position = hit.point + hit.normal * hitOffset;
            if (useBeamRotation)
                hitEffect.transform.rotation = transform.rotation;
            else
                hitEffect.transform.LookAt(hit.point + hit.normal);

            PlayEffect(beamEffects);
            SetTextureLength(Vector3.Distance(transform.position, hit.point));
        }

        else
        {
            Vector3 endPos = transform.position + transform.forward * maxLength;
            beamRenderer.SetPosition(1, endPos);
            hitEffect.transform.position = endPos;
            StopEffect(hitEffects);
            SetTextureLength(Vector3.Distance(transform.position, endPos));
        }

        beamRenderer.enabled = true;
    }

    private void SetTextureLength(float distance)
    {
        textMainLength.x = mainTextureLength * distance;
        textNoiseLength.x = noiseTextureLength * distance;
    }

    private void PlayEffect(ParticleSystem[] effects)
    {
        if (effects == null)
            return;

        foreach (ParticleSystem effect in effects)
        {
            if (!effect.isPlaying)
                effect.Play();
        }
    }

    private void StopEffect(ParticleSystem[] effects)
    {
        if (effects == null)
            return;

        foreach (ParticleSystem effect in effects)
        {
            if (effect.isPlaying)
                effect.Stop();
        }
    }
}
