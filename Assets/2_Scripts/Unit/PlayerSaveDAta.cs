using UnityEngine;

public class PlayerSaveData
{
    public int level;
    public float exp;
    public int gold;

    public float GetRequiredExp(int level)
    {
        // 기본 공식: 100 * (현재 레벨 + 1) * 1.5
        return 100 * level * 1.5f;
    }

    public int AddExp(float amount)
    {
        int levelUpCount = 0;
        exp += amount;

        while (true)
        {
            float requiredExp = GetRequiredExp(level + 1);
            if (exp < requiredExp)
                break;

            exp -= requiredExp;
            level++;
            levelUpCount++;
        }

        return levelUpCount;
    }
}
