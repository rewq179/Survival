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
    private Dictionary<SkillKey, Stack<SkillEffectController>> effectPools = new();
    private Dictionary<SkillComponentType, Stack<SkillComponent>> componentPools = new();

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
        int count = skillData.skillElements.Count;
        for (int i = 0; i < count; i++)
        {
            if (skillData.skillElements[i].indicatorType == SkillIndicatorType.None)
                continue;

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
        RegisterActiveIndicator(indicator);
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
        UnregisterActiveIndicator(indicator);
        PushMesh(indicator.Element.indicatorType, indicator.Mesh);
        PushIndicator(indicator);
    }

    private void RegisterActiveIndicator(SkillIndicator indicator)
    {
        SkillKey key = indicator.Element.skillKey;
        if (!indicatorGroups.ContainsKey(key))
            indicatorGroups.Add(key, new List<SkillIndicator>());

        indicatorGroups[key].Add(indicator);
        activeIndicators.Add(indicator);
    }

    private void UnregisterActiveIndicator(SkillIndicator indicator)
    {
        SkillKey key = indicator.Element.skillKey;
        if (indicatorGroups.ContainsKey(key))
            indicatorGroups[key].Remove(indicator);

        activeIndicators.Remove(indicator);
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
        launcher.Init(inst, startPos, direction, caster, target);
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
        launcher.CreateComponentByCollectible(type);
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

    public void PushSkillObject(SkillKey skillKey, SkillEffectController effect)
    {
        if (!effectPools.ContainsKey(skillKey))
            effectPools[skillKey] = new Stack<SkillEffectController>();

        effect.Reset();
        effect.transform.SetParent(transform, false);
        effectPools[skillKey].Push(effect);
    }

    public SkillEffectController PopSkillObject(SkillKey key, Transform parent)
    {
        if (effectPools.TryGetValue(key, out Stack<SkillEffectController> pools) && pools.Count > 0)
        {
            SkillEffectController effect = pools.Pop();
            effect.Reset();
            effect.transform.SetParent(parent, false);
            return effect;
        }

        return GameMgr.Instance.resourceMgr.GetSkillEffect(parent, key);
    }

    public void PushComponent(SkillComponent component)
    {
        SkillComponentType type = component.Type;
        if (!componentPools.ContainsKey(type))
            componentPools[type] = new Stack<SkillComponent>();

        component.Reset();
        componentPools[type].Push(component);
    }

    public SkillComponent PopComponent(SkillComponentType type)
    {
        if (componentPools.TryGetValue(type, out Stack<SkillComponent> pools) && pools.Count > 0)
            return pools.Pop();

        return type switch
        {
            SkillComponentType.Projectile => new Attack_ProjectileComponent(),
            SkillComponentType.InstantAOE => new Attack_AOEComponent(),
            SkillComponentType.PeriodicAOE => new Attack_PeriodicAOEComponent(),
            SkillComponentType.InstantAttack => new Attack_InstantComponent(),
            SkillComponentType.Beam => new Attack_BeamComponent(),
            SkillComponentType.Linear => new Movement_LinearComponent(),
            SkillComponentType.Leap => new Movement_LeapComponent(),
            SkillComponentType.Gravity => new Effect_GravityComponent(),
            SkillComponentType.Freeze => new Effect_FreezeComponent(),
            SkillComponentType.Explosion => new Effect_ExplosionComponent(),
            _ => null,
        };
    }

    #endregion
}
