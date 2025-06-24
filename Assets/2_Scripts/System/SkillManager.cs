using UnityEngine;
using System;
using System.Collections.Generic;

public class SkillManager : MonoBehaviour
{
    private List<int> skillIds = new();
    private List<int> activeCooldownSkills = new();
    private Dictionary<int, float> previousCooldowns = new();
    private Dictionary<int, float> currentCooldowns = new();
    private Dictionary<int, float> skillCooldownTimes = new();

    // 스킬 인디케이터
    [SerializeField] private GameObject indicatorPrefab;
    [SerializeField] private Material indicatorMaterial;
    private List<SkillIndicator> activeIndicators = new();
    private Stack<SkillIndicator> indicatorPool = new();

    // 인디케이터 상태 관리
    private bool isIndicatorActive = false;
    private int currentIndicatorSkillId = -1;
    private SkillIndicator currentIndicator = null;
    private PlayerController playerController;

    // 스킬 발동 중복 방지
    private bool isSkillActivating = false;

    public event Action<int, float> OnSkillCooldownChanged;
    public event Action<List<int>> OnSkillListChanged;
    public event Action<int> OnSkillCooldownEnded;
    public event Action<string> OnSkillActivated;

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

    private void UpdateIndicator()
    {
        if (isIndicatorActive && currentIndicator != null)
        {
            Vector3 mouseWorldPos = playerController.GetMouseWorldPosition();
            currentIndicator.DrawIndicator(mouseWorldPos);
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
            if (isIndicatorActive)
            {
                CancelIndicator();
                return;
            }

            // 인디케이터 표시 (플레이어 위치에서 시작)
            CreateIndicator(skillId, playerController.transform.position);
            isIndicatorActive = true;
            currentIndicatorSkillId = skillId;
            currentIndicator = activeIndicators[activeIndicators.Count - 1];
        }
    }

    public void ActivateSkill()
    {
        if (!isIndicatorActive || currentIndicatorSkillId == -1 || isSkillActivating)
            return;

        isSkillActivating = true;

        // 스킬 발동
        SkillData skillData = DataManager.GetSkillData(currentIndicatorSkillId);
        OnSkillActivated?.Invoke($"{skillData.name} 발동!!");

        // 쿨다운 시작
        currentCooldowns[currentIndicatorSkillId] = skillCooldownTimes[currentIndicatorSkillId];
        if (!activeCooldownSkills.Contains(currentIndicatorSkillId))
            activeCooldownSkills.Add(currentIndicatorSkillId);

        // 인디케이터 제거
        CancelIndicator();

        // 다음 프레임에서 플래그 리셋
        StartCoroutine(ResetSkillActivatingFlag());
    }

    private System.Collections.IEnumerator ResetSkillActivatingFlag()
    {
        yield return new WaitForEndOfFrame();
        isSkillActivating = false;
    }

    private void CancelIndicator()
    {
        if (currentIndicator != null)
        {
            RemoveIndicator(currentIndicator);
            currentIndicator = null;
        }

        isIndicatorActive = false;
        currentIndicatorSkillId = -1;
    }

    #region Indicator

    public void CreateIndicator(int skillId, Vector3 position)
    {
        SkillData skillData = DataManager.GetSkillData(skillId);
        SkillIndicator indicator = PopIndicator();
        indicator.Init(skillData.indicatorType, skillData.length, skillData.angle, skillData.width, indicatorMaterial);
        indicator.DrawIndicator(position);
        activeIndicators.Add(indicator);
    }

    public void RemoveIndicator(SkillIndicator indicator)
    {
        if (activeIndicators.Contains(indicator))
        {
            activeIndicators.Remove(indicator);
            PushIndicator(indicator);
        }
    }

    public void ClearAllIndicators()
    {
        foreach (var indicator in activeIndicators)
        {
            PushIndicator(indicator);
        }
        activeIndicators.Clear();
    }

    private SkillIndicator PopIndicator()
    {
        if (indicatorPool.Count > 0)
        {
            SkillIndicator indicator = indicatorPool.Pop();
            indicator.gameObject.SetActive(true);
            return indicator;
        }
        else
        {
            SkillIndicator indicator = Instantiate(indicatorPrefab, transform).GetComponent<SkillIndicator>();
            return indicator;
        }
    }

    private void PushIndicator(SkillIndicator indicator)
    {
        indicator.Reset();
        indicatorPool.Push(indicator);
    }

    #endregion
}
