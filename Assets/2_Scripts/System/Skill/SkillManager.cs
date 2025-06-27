using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Video;

public class SkillManager : MonoBehaviour
{
    private List<SkillKey> playerSkills = new();

    // 쿨타임 관리
    private List<SkillKey> activeCooldownSkills = new();
    private Dictionary<SkillKey, float> previousCooldowns = new();
    private Dictionary<SkillKey, float> currentCooldowns = new();
    private Dictionary<SkillKey, float> skillCooldownTimes = new();

    // 스킬 인디케이터
    [SerializeField] private GameObject indicatorPrefab;
    [SerializeField] private Material indicatorMaterial;
    private List<SkillIndicator> activeIndicators = new();
    private Dictionary<SkillKey, List<SkillIndicator>> indicatorGroups = new();
    private Stack<SkillIndicator> indicatorPool = new();
    private PlayerController playerController;

    // 메시 풀 관리
    private Dictionary<SkillIndicatorType, Stack<Mesh>> meshPools = new();

    // 이벤트
    public event Action<SkillKey, float> OnSkillCooldownChanged;
    public event Action<List<SkillKey>> OnSkillListChanged;
    public event Action<SkillKey> OnSkillCooldownEnded;

    // 스킬 인스턴스 관리
    [Header("스킬 런처 프리팹")]
    [SerializeField] private ProjectileLauncher projectileLauncherPrefab;
    [SerializeField] private InstantAOELauncher instantAOELauncherPrefab;
    [SerializeField] private PersistentAOELauncher persistentAOELauncherPrefab;
    private List<SkillLauncher> activeLaunchers = new();
    private Dictionary<SkillLauncherType, Stack<SkillLauncher>> launcherPools = new();
    private Dictionary<SkillKey, Stack<SkillParticleController>> particlePools = new();

    private void Awake()
    {
        InitializeMeshPools();
    }

    private void Update()
    {
        UpdateCooldowns();
        UpdateIndicator();
        UpdateSkillLauncher();
    }

    private void UpdateCooldowns()
    {
        for (int i = activeCooldownSkills.Count - 1; i >= 0; i--)
        {
            SkillKey skillKey = activeCooldownSkills[i];
            float newCooldown = GetCurrentCooldown(skillKey);

            if (Mathf.Abs(previousCooldowns[skillKey] - newCooldown) > 0.01f)
            {
                previousCooldowns[skillKey] = newCooldown;
                OnSkillCooldownChanged?.Invoke(skillKey, newCooldown);
            }

            if (newCooldown <= 0f)
            {
                activeCooldownSkills.RemoveAt(i);
                OnSkillCooldownEnded?.Invoke(skillKey);
            }
        }
    }

    public void Init(Unit unit)
    {
        InitializeSkills(unit.SkillKeys);
        unit.OnSkillChanged += InitializeSkills;
        playerController = unit.GetComponentInChildren<PlayerController>();
    }

    public void InitializeSkills(List<SkillKey> newSkillKeys)
    {
        playerSkills.Clear();
        activeCooldownSkills.Clear();
        currentCooldowns.Clear();
        previousCooldowns.Clear();
        skillCooldownTimes.Clear();
        playerSkills.AddRange(newSkillKeys);

        foreach (SkillKey skillKey in playerSkills)
        {
            skillCooldownTimes[skillKey] = GetSkillCooldown(skillKey);
            currentCooldowns[skillKey] = 0f;
            previousCooldowns[skillKey] = 0f;
        }

        OnSkillListChanged?.Invoke(playerSkills);
    }

    public float GetCooldown(SkillKey skillKey)
    {
        if (!currentCooldowns.ContainsKey(skillKey) || !skillCooldownTimes.ContainsKey(skillKey))
            return 1f;

        return currentCooldowns[skillKey];
    }

    private float GetSkillCooldown(SkillKey skillKey) => DataManager.GetSkillData(skillKey).cooldown;
    private float GetCurrentCooldown(SkillKey skillKey)
    {
        if (!currentCooldowns.ContainsKey(skillKey))
            return 0f;

        if (currentCooldowns[skillKey] > 0f)
        {
            currentCooldowns[skillKey] -= Time.deltaTime;
            if (currentCooldowns[skillKey] < 0f)
                currentCooldowns[skillKey] = 0f;
        }

        return currentCooldowns[skillKey];
    }

    /// <summary>
    /// 스킬 버튼 클릭으로 인디케이터 소환
    /// </summary>
    public void UseSkill(SkillKey skillKey)
    {
        if (currentCooldowns.ContainsKey(skillKey) && currentCooldowns[skillKey] <= 0f)
        {
            RemoveIndicatorsByskillKey(skillKey);
            CreateIndicator(skillKey, playerController.transform.position, true);
        }
    }

    /// <summary>
    /// 인디케이터 이후 클릭으로 스킬 시전
    /// </summary>
    public void ActivateSkill()
    {
        if (activeIndicators.Count == 0)
            return;

        SkillKey skillKey = SkillKey.None;

        Vector3 startPosition = playerController.transform.position;
        startPosition.y = 0f;
        Vector3 targetPosition = Vector3.zero;

        foreach (SkillIndicator indicator in activeIndicators)
        {
            if (indicator.isPlayerIndicator)
            {
                skillKey = indicator.Element.skillKey;
                targetPosition = SkillIndicator.GetElementEndPoint(startPosition, playerController.GetMouseWorldPosition(), indicator.Element);
                break;
            }
        }

        // 쿨다운 설정
        currentCooldowns[skillKey] = skillCooldownTimes[skillKey];
        if (!activeCooldownSkills.Contains(skillKey))
            activeCooldownSkills.Add(skillKey);

        SkillData skillData = DataManager.GetSkillData(skillKey);
        CreateSkillLauncher(skillData, startPosition, targetPosition);
        RemoveIndicatorsByskillKey(skillKey);
    }

    private void UpdateIndicator()
    {
        if (activeIndicators.Count > 0)
        {
            Vector3 playerPos = playerController.transform.position;
            Vector3 mousePos = playerController.GetMouseWorldPosition();

            foreach (var group in indicatorGroups)
            {
                UpdateSkillIndicators(playerPos, mousePos, group.Value);
            }
        }
    }

    private void UpdateSkillIndicators(Vector3 player, Vector3 mouse, List<SkillIndicator> indicators)
    {
        if (indicators.Count == 1) // 단일 인디케이터
            indicators[0].DrawIndicator(player, mouse);

        else // 복합 인디케이터
            UpdateMultipleIndicators(player, mouse, indicators);
    }

    private void UpdateMultipleIndicators(Vector3 player, Vector3 mouse, List<SkillIndicator> indicators)
    {
        if (indicators.Count == 0)
            return;

        for (int i = 0; i < indicators.Count; i++)
        {
            SkillIndicator indicator = indicators[i];

            if (indicator.Element.IsMainIndicator)
            {
                indicator.DrawIndicator(player, mouse);
            }

            else
            {
                IndicatorElement previousElement = indicators[i - 1].Element;
                Vector3 previousEndPoint = SkillIndicator.GetElementEndPoint(player, mouse, previousElement);
                indicator.DrawIndicator(previousEndPoint, mouse);
            }
        }
    }

    private void RemoveIndicatorsByskillKey(SkillKey skillKey)
    {
        for (int i = activeIndicators.Count - 1; i >= 0; i--)
        {
            if (activeIndicators[i].Element.skillKey == skillKey)
            {
                RemoveActiveIndicator(i);
            }
        }
    }

    public void CreateIndicator(SkillKey skillKey, Vector3 start, bool isPlayerIndicator)
    {
        SkillData skillData = DataManager.GetSkillData(skillKey);

        if (skillData.elements.Count > 1) // 복합 인디케이터
        {
            CreateMultipleIndicators(skillData, start, isPlayerIndicator);
        }

        else // 단일 인디케이터
        {
            SkillIndicator indicator = PopIndicator();
            indicator.Init(skillData.elements[0], indicatorMaterial, PopMesh(skillData.elements[0]), isPlayerIndicator);
            indicator.DrawIndicator(start, Vector3.zero);
        }
    }

    private void CreateMultipleIndicators(SkillData skillData, Vector3 start, bool isPlayerIndicator)
    {
        for (int i = 0; i < skillData.elements.Count; i++)
        {
            SkillIndicator indicator = PopIndicator();
            indicator.Init(skillData.elements[i], indicatorMaterial, PopMesh(skillData.elements[i]), isPlayerIndicator);

            if (i == 0)
            {
                indicator.DrawIndicator(start, Vector3.zero);
            }

            else
            {
                Vector3 previousEndPoint = SkillIndicator.GetElementEndPoint(start, Vector3.zero, skillData.elements[i - 1]);
                indicator.DrawIndicator(previousEndPoint, Vector3.zero);
            }
        }
    }

    private void RemoveActiveIndicator(int index)
    {
        if (activeIndicators.Count <= index)
            return;

        SkillIndicator indicator = activeIndicators[index];
        activeIndicators.RemoveAt(index);
        PushMesh(indicator.Element.type, indicator.Mesh);
        PushIndicator(indicator);
    }

    private SkillIndicator PopIndicator()
    {
        if (!indicatorPool.TryPop(out SkillIndicator indicator))
            indicator = Instantiate(indicatorPrefab, transform).GetComponent<SkillIndicator>();

        activeIndicators.Add(indicator);
        SkillKey skillKey = indicator.Element.skillKey;
        if (indicatorGroups.TryGetValue(skillKey, out List<SkillIndicator> group))
            group.Add(indicator);
        else
            indicatorGroups.Add(skillKey, new List<SkillIndicator> { indicator });

        return indicator;
    }

    private void PushIndicator(SkillIndicator indicator)
    {
        indicator.Reset();
        indicatorPool.Push(indicator);
    }

    #region Mesh

    private void InitializeMeshPools()
    {
        Array types = Enum.GetValues(typeof(SkillIndicatorType));
        foreach (SkillIndicatorType type in types)
        {
            meshPools[type] = new Stack<Mesh>();
        }
    }

    private Mesh PopMesh(IndicatorElement element)
    {
        if (meshPools[element.type].TryPop(out Mesh mesh))
            return mesh;

        return element.type switch
        {
            SkillIndicatorType.Line => SkillIndicator.CreateLineMesh(element.length, element.width),
            SkillIndicatorType.Sector => SkillIndicator.CreateSectorMesh(element.angle, element.radius),
            SkillIndicatorType.Circle => SkillIndicator.CreateCircleMesh(element.radius),
            _ => null,
        };
    }

    private void PushMesh(SkillIndicatorType type, Mesh mesh)
    {
        meshPools[type].Push(mesh);
    }

    #endregion

    #region SkillLuancher

    private void UpdateSkillLauncher()
    {
        for (int i = activeLaunchers.Count - 1; i >= 0; i--)
        {
            if (!activeLaunchers[i].IsActive)
            {
                activeLaunchers.RemoveAt(i);
            }
        }
    }

    private void CreateSkillLauncher(SkillData skillData, Vector3 startPos, Vector3 targetPos)
    {
        SkillLauncherType type = skillData.launcherType;
        targetPos.y = 0f;

        // 발사 위치 조정
        startPos = type switch
        {
            SkillLauncherType.Projectile => startPos,
            SkillLauncherType.InstantAOE => startPos,
            SkillLauncherType.PersistentAOE => targetPos,
            _ => Vector3.zero,
        };

        // 런처 생성
        Vector3 direction = (targetPos - startPos).normalized;
        SkillLauncher launcher = PopSkillLauncher(type);
        SkillParticleController particle = PopParticle(skillData.skillKey, launcher.transform);
        launcher.Initialize(skillData, startPos, direction, playerController.GetComponent<Unit>(), particle);
        AddSkillEffects(launcher, skillData.skillKey);
    }

    private SkillLauncher PopSkillLauncher(SkillLauncherType launcherType)
    {
        SkillLauncher launcher = null;
        if (launcherPools.TryGetValue(launcherType, out Stack<SkillLauncher> pool) && pool.Count > 0)
            launcher = pool.Pop();

        else
        {
            launcher = launcherType switch
            {
                SkillLauncherType.Projectile => Instantiate(projectileLauncherPrefab, Vector3.zero, Quaternion.identity),
                SkillLauncherType.InstantAOE => Instantiate(instantAOELauncherPrefab, Vector3.zero, Quaternion.identity),
                SkillLauncherType.PersistentAOE => Instantiate(persistentAOELauncherPrefab, Vector3.zero, Quaternion.identity),
                _ => null,
            };
        }

        activeLaunchers.Add(launcher);
        return launcher;
    }

    public void PushLauncher(SkillLauncher launcher)
    {
        if (!launcherPools.ContainsKey(launcher.Type))
            launcherPools[launcher.Type] = new Stack<SkillLauncher>();

        launcher.Reset();
        activeLaunchers.Remove(launcher);
        launcherPools[launcher.Type].Push(launcher);
    }

    private SkillParticleController PopParticle(SkillKey skillKey, Transform parent)
    {
        if (particlePools.TryGetValue(skillKey, out Stack<SkillParticleController> particles) && particles.Count > 0)
        {
            SkillParticleController particle = particles.Pop();
            particle.transform.SetParent(parent);
            return particle;
        }

        return GameManager.Instance.resourceManager.GetSkillEffect(parent, skillKey).GetComponent<SkillParticleController>();
    }

    public void PushParticle(SkillKey skillKey, SkillParticleController particle)
    {
        if (!particlePools.ContainsKey(skillKey))
            particlePools[skillKey] = new Stack<SkillParticleController>();

        particle.Reset();
        particle.transform.SetParent(transform);
        particlePools[skillKey].Push(particle);
    }

    private void AddSkillEffects(SkillLauncher launcher, SkillKey skillKey)
    {
        SkillData skillData = DataManager.GetSkillData(skillKey);

        switch (skillData.skillKey)
        {
            case SkillKey.Arrow:
                launcher.AddEffect(new DirectDamageEffect(30f));
                break;

            case SkillKey.Dagger:
                launcher.AddEffect(new DirectDamageEffect(25f));
                break;

            case SkillKey.Laser:
                launcher.AddEffect(new DirectDamageEffect(40f));
                launcher.AddEffect(new TrailExplosionEffect(1.5f, 10f, 0.2f));
                break;

            case SkillKey.Nova:
                launcher.AddEffect(new DirectDamageEffect(35f));
                break;

            case SkillKey.EnergyExplosion:
                launcher.AddEffect(new DirectDamageEffect(45f));
                break;

            case SkillKey.LightningStrike:
                launcher.AddEffect(new PeriodicDamageEffect(15f, 1f));
                break;

            case SkillKey.Meteor:
                launcher.AddEffect(new PeriodicDamageEffect(20f, 0.5f));
                launcher.AddEffect(new FinalExplosionEffect(4f, 30f));
                break;
        }
    }

    #endregion
}
