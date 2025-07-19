using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;

public class SkillMgr : MonoBehaviour
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
            RemoveIndicatorByKey(skillKey);
            ShowIndicator(skillKey, caster.firePoint.position, true);
        }
    }

    /// <summary>
    /// 인디케이터 이후 클릭으로 스킬 시전
    /// </summary>
    public void ActivateSkill(Unit caster)
    {
        if (activeIndicators.Count == 0)
            return;

        SkillElement element = GetWaitedPlayerSkillElement();
        if (element == null)
            return;

        SkillKey skillKey = element.skillKey;
        Vector3 startPos = caster.firePoint.position;
        Vector3 endPos = SkillIndicator.GetElementEndPoint(startPos, playerController.GetMouseWorldPosition(), element);
        AdjustSkillPosY(element, ref startPos, ref endPos);
        if (element.indicatorType == SkillIndicatorType.Circle)
            startPos = endPos;

        // 생성
        SkillInstance inst = caster.GetSkillInstance(skillKey);
        CreateSkillLauncher(inst, startPos, endPos, caster);
        RemoveIndicatorByKey(skillKey);
    }

    private SkillElement GetWaitedPlayerSkillElement()
    {
        foreach (SkillIndicator indicator in activeIndicators)
        {
            if (indicator.isPlayerIndicator)
                return indicator.Element;
        }

        return null;
    }

    private void AdjustSkillPosY(SkillElement element, ref Vector3 startPos, ref Vector3 endPos)
    {
        switch (element.indicatorType)
        {
            case SkillIndicatorType.Line:
                startPos.y = 1f;
                endPos.y = 1f;
                break;

            case SkillIndicatorType.Sector:
            case SkillIndicatorType.Rectangle:
                startPos.y = 0f;
                endPos.y = 0f;
                break;

            case SkillIndicatorType.Circle:
                endPos.y = 0f;
                break;
        }
    }

    /// <summary>
    /// 지정된 스킬 시전
    /// </summary>
    public void ActivateSkill(SkillKey skillKey, Unit caster, Unit target)
    {
        Vector3 startPos = caster.firePoint.position;
        Vector3 endPos = target.transform.position;

        SkillData skillData = DataMgr.GetSkillData(skillKey);
        AdjustSkillPosY(skillData.skillElements[0], ref startPos, ref endPos);

        SkillInstance inst = caster.GetSkillInstance(skillKey);
        CreateSkillLauncher(inst, startPos, endPos, caster, target);
    }

    #region Indicator

    private void UpdateIndicator()
    {
        if (activeIndicators.Count <= 0)
            return;

        Vector3 playerPos = playerController.transform.position;
        Vector3 mousePos = playerController.GetMouseWorldPosition();

        foreach (var group in indicatorGroups)
        {
            UpdateSkillIndicators(playerPos, mousePos, group.Value);
        }
    }

    private void UpdateSkillIndicators(Vector3 player, Vector3 mouse, List<SkillIndicator> indicators)
    {
        if (indicators.Count == 0)
            return;

        if (indicators[0].isPlayerIndicator)
            UpdatePlayerIndicators(player, mouse, indicators);
        else
            UpdateMonsterIndicators(indicators);
    }

    private void UpdatePlayerIndicators(Vector3 player, Vector3 mouse, List<SkillIndicator> indicators)
    {
        if (indicators.Count == 1) // 단일 인디케이터
            indicators[0].DrawIndicator(player, mouse);
        else // 복합 인디케이터
            UpdateMultipleIndicators(player, mouse, indicators);
    }

    private void UpdateMonsterIndicators(List<SkillIndicator> indicators)
    {
        // 몬스터 인디케이터는 시간에 따른 연출 처리
        // TODO: 몬스터 인디케이터 구현 시 여기에 로직 추가
        foreach (SkillIndicator indicator in indicators)
        {
            // 몬스터 인디케이터는 시간에 따라 범위 표시
            // 예: 발동 시간에 가까워질수록 원형 범위 강조
            // indicator.UpdateMonsterIndicator();
        }
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
                SkillElement previousElement = indicators[i - 1].Element;
                Vector3 previousEndPoint = SkillIndicator.GetElementEndPoint(player, mouse, previousElement);
                indicator.DrawIndicator(previousEndPoint, mouse);
            }
        }
    }

    public void ShowIndicator(SkillKey skillKey, Vector3 start, bool isPlayerIndicator)
    {
        SkillData skillData = DataMgr.GetSkillData(skillKey);

        if (skillData.skillElements.Count > 1) // 복합 인디케이터
        {
            ShowMultipleIndicators(skillData, start, isPlayerIndicator);
        }

        else // 단일 인디케이터
        {
            SkillIndicator indicator = CreateIndicator(skillData.skillElements[0], isPlayerIndicator);
            indicator.DrawIndicator(start, Vector3.zero);
        }
    }

    private void ShowMultipleIndicators(SkillData skillData, Vector3 start, bool isPlayerIndicator)
    {
        for (int i = 0; i < skillData.skillElements.Count; i++)
        {
            SkillIndicator indicator = CreateIndicator(skillData.skillElements[i], isPlayerIndicator);

            if (i == 0)
            {
                indicator.DrawIndicator(start, Vector3.zero);
            }

            else
            {
                Vector3 previousEndPoint = SkillIndicator.GetElementEndPoint(start, Vector3.zero, skillData.skillElements[i - 1]);
                indicator.DrawIndicator(previousEndPoint, Vector3.zero);
            }
        }
    }

    public SkillIndicator CreateIndicator(SkillElement element, bool isPlayerIndicator)
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

    private void RemoveIndicatorByKey(SkillKey skillKey)
    {
        for (int i = activeIndicators.Count - 1; i >= 0; i--)
        {
            if (activeIndicators[i].Element.skillKey == skillKey)
            {
                RemoveIndicator(activeIndicators[i]);
                break;
            }
        }
    }

    public void RemoveIndicator(SkillIndicator indicator)
    {
        SkillKey skillKey = indicator.Element.skillKey;
        if (indicatorGroups.TryGetValue(skillKey, out List<SkillIndicator> group))
            group.Remove(indicator);

        activeIndicators.Remove(indicator);
        PushMesh(indicator.Element.indicatorType, indicator.Mesh);
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

    private SkillLauncher CreateSkillLauncher(SkillInstance inst, Vector3 startPos, Vector3 targetPos, Unit caster, Unit target = null)
    {
        Vector3 direction = (targetPos - startPos).normalized;

        if (inst.IsMultipleProjectile())
            return CreateMultipleLauncher(inst, startPos, direction, caster, target);

        return CreateSingleLauncher(inst, startPos, direction, caster, target);
    }

    private SkillLauncher CreateMultipleLauncher(SkillInstance inst, Vector3 startPos, Vector3 direction, Unit caster, Unit target)
    {
        int shot = inst.Values[0].ShotFinal.GetInt();
        float spreadAngle = GameValue.PROJECTILE_SPREAD_ANGLE;
        
        float angleStep = spreadAngle / (shot - 1);
        float startAngle = -spreadAngle * 0.5f;

        for (int i = 0; i < shot; i++)
        {
            float curAngle = startAngle + (angleStep * i);
            Vector3 rotatedDirection = Quaternion.Euler(0, curAngle, 0) * direction;
            CreateSingleLauncher(inst, startPos, rotatedDirection, caster, target);
        }

        caster.StartCooldown(inst.skillKey);
        return null;
    }

    private SkillLauncher CreateSingleLauncher(SkillInstance inst, Vector3 startPos, Vector3 direction, Unit caster, Unit target)
    {
        SkillLauncher launcher = PopSkillLauncher();
        SkillParticleController particle = PopParticle(inst.skillKey, launcher.transform);
        launcher.Init(inst, startPos, direction, particle, caster, target);
        caster.StartCooldown(inst.skillKey);
        activeLaunchers.Add(launcher);
        return launcher;
    }

    public void RemoveLauncher(SkillLauncher launcher)
    {
        launcher.Reset();
        PushLauncher(launcher);
        activeLaunchers.Remove(launcher);
    }

    public void ExecuteItemSkill(Unit player, CollectibleType type)
    {
        SkillLauncher launcher = PopSkillLauncher();
        launcher.Init(player, player.transform.position, Vector3.zero);
        launcher.CreateComponent(type);
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

    private Mesh PopMesh(SkillElement element)
    {
        if (meshPools[element.indicatorType].TryPop(out Mesh mesh))
            return mesh;

        return element.indicatorType switch
        {
            SkillIndicatorType.Line => SkillIndicator.CreateLineMesh(GameValue.PROJECTILE_MAX_LENGTH, GameValue.PROJECTILE_MAX_WIDTH),
            SkillIndicatorType.Sector => SkillIndicator.CreateSectorMesh(element.Angle, element.Radius),
            SkillIndicatorType.Circle => SkillIndicator.CreateCircleMesh(element.Radius),
            SkillIndicatorType.Rectangle => SkillIndicator.CreateLineMesh(GameValue.PROJECTILE_MAX_LENGTH, GameValue.PROJECTILE_MAX_WIDTH),
            _ => null,
        };
    }

    private void PushLauncher(SkillLauncher launcher)
    {
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

        return GameMgr.Instance.resourceMgr.GetSkillEffect(parent, skillKey);
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
