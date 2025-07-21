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

    public void Reset()
    {
        level = 1;
        exp = 0f;
        gold = 0;
        
        OnLevelChanged = null;
        OnExpChanged = null;
        OnGoldChanged = null;
        unit = null;
    }

    public void Init(Unit unit)
    {
        this.unit = unit;
    }

    public int AddExp(float amount)
    {
        int levelUpCount = 0;
        exp += amount * (1 + unit.GetFinalStat(StatType.ExpGain));

        while (true)
        {
            float requiredExp = GameValue.GetRequiredExp(level);
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
        gold += (int)(amount * (1 + unit.GetFinalStat(StatType.GoldGain)));
        OnGoldChanged?.Invoke(gold);
    }
}