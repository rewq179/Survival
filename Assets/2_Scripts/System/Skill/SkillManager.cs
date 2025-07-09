using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;

public class SkillManager : MonoBehaviour
{
    // 스킬 인디케이터
    [SerializeField] private SkillIndicator indicatorPrefab;
    [SerializeField] private Material indicatorMaterial;
    private List<SkillIndicator> activeIndicators = new();
    private Dictionary<SkillKey, List<SkillIndicator>> indicatorGroups = new();
    private Stack<SkillIndicator> indicatorPool = new();
    private PlayerController playerController;

    // 메시 풀 관리
    private Dictionary<SkillIndicatorType, Stack<Mesh>> meshPools = new();

    // 스킬 인스턴스 관리
    [Header("스킬 런처 프리팹")]
    [SerializeField] private SkillLauncher skillLauncherPrefab;
    private List<SkillLauncher> activeLaunchers = new();
    private Stack<SkillLauncher> launcherPools = new();
    private Dictionary<SkillKey, Stack<SkillParticleController>> particlePools = new();

    private void Start()
    {
        for (SkillIndicatorType i = 0; i < SkillIndicatorType.Max; i++)
        {
            meshPools.Add(i, new Stack<Mesh>());
        }
    }

    private void Update()
    {
        UpdateIndicator();
        UpdateSkillLauncher();
    }

    public void Init(Unit unit)
    {
        playerController = unit.GetComponentInChildren<PlayerController>();
    }

    /// <summary>
    /// 스킬 버튼 클릭으로 인디케이터 소환
    /// </summary>
    public void UseSkill(SkillKey skillKey, Unit caster)
    {
        if (caster.CanUseSkill(skillKey))
        {
            RemoveIndicatorsByskillKey(skillKey);
            ShowIndicator(skillKey, caster.transform.position, true);
        }
    }

    /// <summary>
    /// 인디케이터 이후 클릭으로 스킬 시전
    /// </summary>
    public void ActivateSkill(Unit caster)
    {
        if (activeIndicators.Count == 0)
            return;

        SkillKey skillKey = SkillKey.None;
        Vector3 startPosition = caster.transform.position;
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

        // 스킬 인스턴스 생성
        SkillInstance inst = caster.GetSkillInstance(skillKey);
        caster.StartCooldown(skillKey);

        // 스킬 런처 생성
        CreateSkillLauncher(inst, startPosition, targetPosition, caster);
        RemoveIndicatorsByskillKey(skillKey);
    }

    #region Indicator

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
                RemoveIndicator(i);
            }
        }
    }

    public void ShowIndicator(SkillKey skillKey, Vector3 start, bool isPlayerIndicator)
    {
        SkillData skillData = DataMgr.GetSkillData(skillKey);

        if (skillData.indicatorElements.Count > 1) // 복합 인디케이터
        {
            ShowMultipleIndicators(skillData, start, isPlayerIndicator);
        }

        else // 단일 인디케이터
        {
            SkillIndicator indicator = CreateIndicator(skillData.indicatorElements[0], isPlayerIndicator);
            indicator.DrawIndicator(start, Vector3.zero);
        }
    }

    private void ShowMultipleIndicators(SkillData skillData, Vector3 start, bool isPlayerIndicator)
    {
        for (int i = 0; i < skillData.indicatorElements.Count; i++)
        {
            SkillIndicator indicator = CreateIndicator(skillData.indicatorElements[i], isPlayerIndicator);

            if (i == 0)
            {
                indicator.DrawIndicator(start, Vector3.zero);
            }

            else
            {
                Vector3 previousEndPoint = SkillIndicator.GetElementEndPoint(start, Vector3.zero, skillData.indicatorElements[i - 1]);
                indicator.DrawIndicator(previousEndPoint, Vector3.zero);
            }
        }
    }

    private SkillIndicator CreateIndicator(IndicatorElement element, bool isPlayerIndicator)
    {
        SkillIndicator indicator = PopIndicator();
        indicator.Init(element, indicatorMaterial, PopMesh(element), isPlayerIndicator);

        activeIndicators.Add(indicator);
        SkillKey skillKey = indicator.Element.skillKey;
        if (indicatorGroups.TryGetValue(skillKey, out List<SkillIndicator> group))
            group.Add(indicator);
        else
            indicatorGroups.Add(skillKey, new List<SkillIndicator> { indicator });

        return indicator;
    }

    private void RemoveIndicator(int index)
    {
        if (activeIndicators.Count <= index)
            return;

        SkillIndicator indicator = activeIndicators[index];
        activeIndicators.RemoveAt(index);
        PushMesh(indicator.Element.type, indicator.Mesh);
        PushIndicator(indicator);
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

    private SkillLauncher CreateSkillLauncher(SkillInstance skillInstance, Vector3 startPos, Vector3 targetPos, Unit caster, Unit target = null)
    {
        targetPos.y = 0f;
        Vector3 direction = (targetPos - startPos).normalized;
        SkillLauncher launcher = PopSkillLauncher();
        activeLaunchers.Add(launcher);
        SkillParticleController particle = PopParticle(skillInstance.skillKey, launcher.transform);
        
        // 스킬 인스턴스 데이터 전달
        launcher.Initialize(skillInstance, startPos, direction, particle, caster, target);
        return launcher;
    }

    public void RemoveLauncher(SkillLauncher launcher)
    {
        PushLauncher(launcher);
        activeLaunchers.Remove(launcher);
    }

    /// <summary>
    /// 몬스터 공격용
    /// </summary>
    public void ExecuteMonsterAttack(SkillKey skillKey, Unit caster, Unit target)
    {
        Vector3 startPos = caster.transform.position;
        Vector3 targetPos = target.transform.position;
        SkillInstance inst = caster.GetSkillInstance(skillKey);
        CreateSkillLauncher(inst, startPos, targetPos, caster, target);
    }

    /// <summary>
    /// 몬스터 공격용 임시 런처 (GameObject 없이 동작)
    /// </summary>
    private class TempSkillLauncher
    {
        public Unit Caster { get; }
        public Vector3 Position { get; }
        public bool IsAffectCaster => false;

        public TempSkillLauncher(Unit caster, Vector3 position)
        {
            Caster = caster;
            Position = position;
        }

        public void Deactivate() { }
    }

    #endregion

    #region Object Pool

    private void PushIndicator(SkillIndicator indicator)
    {
        indicator.Reset();
        indicatorPool.Push(indicator);
    }

    private SkillIndicator PopIndicator()
    {
        if (indicatorPool.TryPop(out SkillIndicator indicator))
            return indicator;

        return Instantiate(indicatorPrefab, transform);
    }

    private void PushMesh(SkillIndicatorType type, Mesh mesh)
    {
        meshPools[type].Push(mesh);
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

    private void PushLauncher(SkillLauncher launcher)
    {
        launcher.Reset();
        launcherPools.Push(launcher);
    }

    private SkillLauncher PopSkillLauncher()
    {
        if (launcherPools.TryPop(out SkillLauncher launcher))
            return launcher;

        return Instantiate(skillLauncherPrefab, Vector3.zero, Quaternion.identity);
    }

    private SkillParticleController PopParticle(SkillKey skillKey, Transform parent)
    {
        if (particlePools.TryGetValue(skillKey, out Stack<SkillParticleController> particles) && particles.Count > 0)
        {
            SkillParticleController particle = particles.Pop();
            particle.transform.SetParent(parent);
            return particle;
        }

        return GameManager.Instance.resourceMgr.GetSkillEffect(parent, skillKey);
    }

    public void PushParticle(SkillKey skillKey, SkillParticleController particle)
    {
        if (particle == null)
            return;

        if (!particlePools.ContainsKey(skillKey))
            particlePools[skillKey] = new Stack<SkillParticleController>();

        particle.Reset();
        particle.transform.SetParent(transform);
        particlePools[skillKey].Push(particle);
    }

    #endregion
}
