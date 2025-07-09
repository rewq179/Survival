using System.Collections.Generic;
using UnityEngine;
using System;

public class PlayerSaveData
{
    private Unit unit;
    public int level;
    public float exp;
    public int gold;

    // 델리게이트/이벤트
    public event Action<int> OnLevelChanged;
    public event Action<float> OnExpChanged;
    public event Action<int> OnGoldChanged;

    public void Init(Unit unit)
    {
        this.unit = unit;
    }

    public float GetRequiredExp(int level)
    {
        // 기본 공식: 100 * (현재 레벨 + 1) * 1.5
        return 100 * (level + 1) * 1.5f;
    }

    public int AddExp(float amount)
    {
        int levelUpCount = 0;
        exp += amount;

        while (true)
        {
            float requiredExp = GetRequiredExp(level);
            if (exp < requiredExp)
                break;

            exp -= requiredExp;
            level++;
            levelUpCount++;
        }

        OnExpChanged?.Invoke(exp);
        
        if (levelUpCount > 0)
        {
            UIMgr.Instance.selectionPanel.AddLevelUpCount(levelUpCount);
            unit.UpdateHp();
            OnLevelChanged?.Invoke(level);
        }

        return levelUpCount;
    }

    public void AddGold(int amount)
    {
        gold += amount;
        OnGoldChanged?.Invoke(gold);
    }
}