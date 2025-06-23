using UnityEngine;

public class CombatModule
{
    private float curHp;
    private bool isDead;

    public float CurHp => curHp;
    public bool IsDead => isDead;

    public void Reset()
    {
        isDead = false;
    }

    public void TakeDamage(int damage)
    {
        curHp = Mathf.Clamp01(curHp - damage);
        if (curHp <= 0)
            OnDead();
    }

    public void OnDead()
    {
        isDead = true;
        // 죽음 로직
    }
}
