using UnityEngine;

public class GameValue
{
    public static readonly LayerMask UNIT_LAYERS = LayerMask.GetMask("Unit");

    // 스킬
    public const float PROJECTILE_MAX_LENGTH = 15f;
    public const float PROJECTILE_MAX_LENGTH_POW = PROJECTILE_MAX_LENGTH * PROJECTILE_MAX_LENGTH;
    public const float PROJECTILE_MAX_WIDTH = 0.4f;
    public const int MAX_ACTIVE_SKILL_LEVEL = 3;
    public const int MAX_PASSIVE_SKILL_LEVEL = 3;
    public const int MAX_SUB_SKILL_LEVEL = 3;
}
