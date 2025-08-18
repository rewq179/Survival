using System.Collections.Generic;
using UnityEngine;
using System;

public class PlayerSaveData
{
    private Unit unit;
    public int level;
    public float exp;
    public int gold;

    public void Reset()
    {
        level = 1;
        exp = 0f;
        gold = 0;
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

        GameEvents.Instance.PlayerExpChanged(exp);

        if (levelUpCount > 0)
        {
            GameEvents.Instance.PlayerLevelUp(levelUpCount);
            unit.UpdateHp();
            GameEvents.Instance.PlayerLevelChanged(level);
        }

        return levelUpCount;
    }

    public void AddGold(int amount)
    {
        gold += (int)(amount * (1 + unit.GetFinalStat(StatType.GoldGain)));
        GameEvents.Instance.PlayerGoldChanged(gold);
    }
}