using System;
using System.Collections.Generic;
using UnityEngine;


public class GameEvents : MonoBehaviour
{
    public static GameEvents Instance { get; private set; }

    // Player Events
    public event Action<float, float> OnPlayerHpChanged;
    public event Action<int> OnPlayerLevelChanged;
    public event Action<float> OnPlayerExpChanged;
    public event Action<int> OnPlayerLevelUp;
    public event Action<int> OnPlayerGoldChanged;

    // Skill Events
    public event Action<SkillKey> OnPlayerSkillAdded;
    public event Action<SkillKey> OnPlayerSkillCooldownEnded;
    public event Action<SkillKey, int> OnPlayerSkillLevelChanged;
    public event Action<SkillKey, float> OnPlayerSkillCooldownChanged;

    // Buff Events
    public event Action<BuffKey> OnBuffAdded;
    public event Action<BuffKey> OnBuffRemoved;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        else
        {
            Destroy(gameObject);
        }
    }

    // 플레이어 이벤트
    public void PlayerHpChanged(float prev, float cur)
    {
        OnPlayerHpChanged?.Invoke(prev, cur);
    }

    public void PlayerLevelChanged(int level)
    {
        OnPlayerLevelChanged?.Invoke(level);
    }

    public void PlayerExpChanged(float exp)
    {
        OnPlayerExpChanged?.Invoke(exp);
    }

    public void PlayerLevelUp(int levelUpCount)
    {
        OnPlayerLevelUp?.Invoke(levelUpCount);
    }

    public void PlayerGoldChanged(int gold)
    {
        OnPlayerGoldChanged?.Invoke(gold);
    }

    // 스킬
    public void PlayerSkillAdded(SkillKey skillKey)
    {
        OnPlayerSkillAdded?.Invoke(skillKey);
    }

    public void PlayerSkillCooldownEnded(SkillKey skillKey)
    {
        OnPlayerSkillCooldownEnded?.Invoke(skillKey);
    }

    public void PlayerSkillCooldownChanged(SkillKey skillKey, float cooldown)
    {
        OnPlayerSkillCooldownChanged?.Invoke(skillKey, cooldown);
    }

    public void SkillLevelChanged(SkillKey skillKey, int level)
    {
        OnPlayerSkillLevelChanged?.Invoke(skillKey, level);
    }
}