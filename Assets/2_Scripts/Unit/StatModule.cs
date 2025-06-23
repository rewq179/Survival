using UnityEngine;

public class StatModule
{
    private float maxHp;
    private float attack;
    private float moveSpd;
    private float cooldown;
    private float critChance;
    private float critMulti;

    public float MaxHp => maxHp;
    public float Attack => attack;
    public float MoveSpd => moveSpd;
    public float Cooldown => cooldown;
    public float CritChance => critChance;
    public float CritMulti => critMulti;

    public void Init(int level)
    {
        maxHp = 20 + level * 10;
        attack = 1 + level * 1;
        moveSpd = 3;
        critChance = 0f;
        critMulti = 1f;
    }
}
