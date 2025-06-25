using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;

public class SkillManager : MonoBehaviour
{
    private List<int> skillIds = new();

    // 쿨타임 관리
    private List<int> activeCooldownSkills = new();
    private Dictionary<int, float> previousCooldowns = new();
    private Dictionary<int, float> currentCooldowns = new();
    private Dictionary<int, float> skillCooldownTimes = new();

    // 스킬 인디케이터
    [SerializeField] private GameObject indicatorPrefab;
    [SerializeField] private Material indicatorMaterial;
    private List<SkillIndicator> activeIndicators = new();
    private Dictionary<int, List<SkillIndicator>> skillGroups = new();
    private Stack<SkillIndicator> indicatorPool = new();

    // 인디케이터 상태 관리
    private PlayerController playerController;

    // 스킬 발동 중복 방지
    private bool isSkillActivating = false;

    // 메시 풀 관리
    private Dictionary<SkillIndicatorType, Stack<Mesh>> meshPools = new();

    // 이벤트
    public event Action<int, float> OnSkillCooldownChanged;
    public event Action<List<int>> OnSkillListChanged;
    public event Action<int> OnSkillCooldownEnded;
    public event Action<string> OnSkillActivated;

    private void Awake()
    {
        InitializeMeshPools();
    }

    private void Update()
    {
        UpdateCooldowns();
        UpdateIndicator();
    }

    private void UpdateCooldowns()
    {
        for (int i = activeCooldownSkills.Count - 1; i >= 0; i--)
        {
            int skillId = activeCooldownSkills[i];
            float newCooldown = GetCurrentCooldown(skillId);

            if (Mathf.Abs(previousCooldowns[skillId] - newCooldown) > 0.01f)
            {
                previousCooldowns[skillId] = newCooldown;
                OnSkillCooldownChanged?.Invoke(skillId, newCooldown);
            }

            if (newCooldown <= 0f)
            {
                activeCooldownSkills.RemoveAt(i);
                OnSkillCooldownEnded?.Invoke(skillId);
            }
        }
    }

    public void Init(Unit unit)
    {
        InitializeSkills(unit.SkillIds);
        unit.OnSkillChanged += InitializeSkills;
        playerController = unit.GetComponentInChildren<PlayerController>();
    }

    public void InitializeSkills(List<int> newSkillIds)
    {
        skillIds.Clear();
        activeCooldownSkills.Clear();
        currentCooldowns.Clear();
        previousCooldowns.Clear();
        skillCooldownTimes.Clear();
        skillIds.AddRange(newSkillIds);

        foreach (int id in skillIds)
        {
            float cooldownTime = GetSkillCooldownTime(id);
            skillCooldownTimes[id] = cooldownTime;
            currentCooldowns[id] = 0f;
            previousCooldowns[id] = 0f;
        }

        OnSkillListChanged?.Invoke(skillIds);
    }

    public float GetCooldown(int skillId)
    {
        if (!currentCooldowns.ContainsKey(skillId) || !skillCooldownTimes.ContainsKey(skillId))
            return 1f;

        return currentCooldowns[skillId];
    }

    private float GetSkillCooldownTime(int skillId) => DataManager.GetSkillData(skillId).cooldown;
    private float GetCurrentCooldown(int skillId)
    {
        if (!currentCooldowns.ContainsKey(skillId))
            return 0f;

        if (currentCooldowns[skillId] > 0f)
        {
            currentCooldowns[skillId] -= Time.deltaTime;
            if (currentCooldowns[skillId] < 0f)
                currentCooldowns[skillId] = 0f;
        }

        return currentCooldowns[skillId];
    }

    public void UseSkill(int skillId)
    {
        if (currentCooldowns.ContainsKey(skillId) && currentCooldowns[skillId] <= 0f)
        {
            RemoveIndicatorsBySkillId(skillId);
            CreateIndicator(skillId, playerController.transform.position, true);
        }
    }

    public void ActivateSkill()
    {
        if (activeIndicators.Count == 0 || isSkillActivating)
            return;

        isSkillActivating = true;

        int skillId = -1;
        foreach (var indicator in activeIndicators)
        {
            if (indicator.isPlayerIndicator)
            {
                skillId = indicator.Element.skillId;
                break;
            }
        }

        if (skillId == -1)
        {
            isSkillActivating = false;
            return;
        }

        SkillData skillData = DataManager.GetSkillData(skillId);
        OnSkillActivated?.Invoke($"{skillData.name} 발동!!");

        currentCooldowns[skillId] = skillCooldownTimes[skillId];
        if (!activeCooldownSkills.Contains(skillId))
            activeCooldownSkills.Add(skillId);

        RemoveIndicatorsBySkillId(skillId);
        StartCoroutine(ResetSkillActivatingFlag());
    }

    private IEnumerator ResetSkillActivatingFlag()
    {
        yield return new WaitForEndOfFrame();
        isSkillActivating = false;
    }

    #region Indicator

    private void UpdateIndicator()
    {
        if (activeIndicators.Count > 0)
        {
            Vector3 playerPos = playerController.transform.position;
            Vector3 mousePos = playerController.GetMouseWorldPosition();

            foreach (var group in skillGroups)
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

    private void RemoveIndicatorsBySkillId(int skillId)
    {
        for (int i = activeIndicators.Count - 1; i >= 0; i--)
        {
            if (activeIndicators[i].Element.skillId == skillId)
            {
                RemoveActiveIndicator(i);
            }
        }
    }

    public void CreateIndicator(int skillId, Vector3 start, bool isPlayerIndicator)
    {
        SkillData skillData = DataManager.GetSkillData(skillId);

        if (skillData.elements.Count > 1) // 복합 인디케이터
        {
            CreateMultipleIndicators(skillData, start, isPlayerIndicator);
        }

        else // 단일 인디케이터
        {
            SkillIndicator indicator = CreateActiveIndicator(skillData.elements[0], isPlayerIndicator);
            indicator.DrawIndicator(start, Vector3.zero);
        }
    }

    private void CreateMultipleIndicators(SkillData skillData, Vector3 start, bool isPlayerIndicator)
    {
        for (int i = 0; i < skillData.elements.Count; i++)
        {
            SkillIndicator indicator = CreateActiveIndicator(skillData.elements[i], isPlayerIndicator);

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

    public SkillIndicator CreateActiveIndicator(IndicatorElement element, bool isPlayerIndicator)
    {
        SkillIndicator indicator = PopIndicator();
        activeIndicators.Add(indicator);

        int skillId = indicator.Element.skillId;
        if (skillGroups.TryGetValue(skillId, out List<SkillIndicator> group))
            group.Add(indicator);
        else
            skillGroups.Add(skillId, new List<SkillIndicator> { indicator });

        indicator.Init(element, indicatorMaterial, PopMesh(element), isPlayerIndicator);
        return indicator;
    }

    public void RemoveActiveIndicator(int index)
    {
        if (activeIndicators.Count > index)
        {
            SkillIndicator indicator = activeIndicators[index];
            activeIndicators.RemoveAt(index);

            PushMesh(indicator.Element.type, indicator.Mesh);
            PushIndicator(indicator);
        }
    }

    private SkillIndicator PopIndicator()
    {
        if (!indicatorPool.TryPop(out SkillIndicator indicator))
            indicator = Instantiate(indicatorPrefab, transform).GetComponent<SkillIndicator>();

        return indicator;
    }

    private void PushIndicator(SkillIndicator indicator)
    {
        indicator.Reset();
        indicatorPool.Push(indicator);
    }

    #endregion

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
        if (!meshPools[element.type].TryPop(out Mesh mesh))
            mesh = CreateMesh(element);

        return mesh;
    }

    private void PushMesh(SkillIndicatorType type, Mesh mesh)
    {
        meshPools[type].Push(mesh);
    }

    private Mesh CreateMesh(IndicatorElement element)
    {
        Mesh mesh = element.type switch
        {
            SkillIndicatorType.Line => SkillIndicator.CreateLineMesh(element.length, element.width),
            SkillIndicatorType.Sector => SkillIndicator.CreateSectorMesh(element.angle, element.radius),
            SkillIndicatorType.Circle => SkillIndicator.CreateCircleMesh(element.radius),
            _ => null,
        };

        return mesh;
    }

    #endregion
}
