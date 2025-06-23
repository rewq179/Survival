using UnityEngine;

public class Unit : MonoBehaviour
{
    private StatModule statModule = new();
    private BehaviourModule behaviourModule = new();
    private CombatModule combatModule = new();
    private PlayerSaveData playerSaveData = new();

    // StatModule
    public float MaxHp => statModule.MaxHp;

    // BehaviourModule

    // CombatModule
    public float CurHp => combatModule.CurHp;
    public void TakeDamage(int damage) => combatModule.TakeDamage(damage);

    // PlayerSaveData
    public int Level => playerSaveData.level;
    public float CurExp => playerSaveData.exp;
    public float MaxExp => playerSaveData.GetRequiredExp(playerSaveData.level);
}
