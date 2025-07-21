using UnityEngine;

public class GameValue
{
    public const string SHEET_ADDRESS = "15RA8mCckMbGIXxx5Ukfmtih0TxDTEGkXiAVyrHf8Blw";
    public static readonly LayerMask UNIT_LAYERS = LayerMask.GetMask("Unit");

    public static float GetRequiredExp(int level)
    {
        if (level <= 5)
            return 3 + 5 * level;

        else if (level <= 10)
            return 8 + 10 * level;

        else
            return 20 + 12 * level;
    }

    // 스킬
    public const float PROJECTILE_MAX_LENGTH = 15f;
    public const float PROJECTILE_MAX_LENGTH_POW = PROJECTILE_MAX_LENGTH * PROJECTILE_MAX_LENGTH;
    public const float PROJECTILE_MAX_WIDTH = 0.4f;
    public const float PROJECTILE_SPREAD_ANGLE = 15f;

    public const int MAX_ACTIVE_SKILL_LEVEL = 3;
    public const int MAX_PASSIVE_SKILL_LEVEL = 3;
    public const int MAX_SUB_SKILL_LEVEL = 3;
}
