using UnityEngine;
using System;
using System.Collections.Generic;

public class SkillManager : MonoBehaviour
{
    private bool isInitialized = false;

    private List<int> skillIds = new();
    private List<int> activeCooldownSkills = new();
    private Dictionary<int, float> previousCooldowns = new();
    private Dictionary<int, float> currentCooldowns = new();
    private Dictionary<int, float> skillCooldownTimes = new();

    public event Action<int, float> OnSkillCooldownChanged;
    public event Action<List<int>> OnSkillListChanged;
    public event Action<int> OnSkillCooldownEnded;

    private void Update()
    {
        if (!isInitialized || activeCooldownSkills.Count == 0)
            return;

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
        isInitialized = true;
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

    public float GetCooldownRatio(int skillId)
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
            currentCooldowns[skillId] = skillCooldownTimes[skillId];

            if (!activeCooldownSkills.Contains(skillId))
                activeCooldownSkills.Add(skillId);

            // TODO:스킬 사용 로직 추가
        }
    }
}
