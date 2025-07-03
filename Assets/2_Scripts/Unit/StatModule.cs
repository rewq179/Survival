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

    public void Init(UnitData unitData)
    {
        maxHp = unitData.hp;
        attack = unitData.attack;
        moveSpd = unitData.moveSpd;
        cooldown = unitData.cooldown;
        critChance = unitData.critChance;
        critMulti = unitData.critMulti;
    }
}
