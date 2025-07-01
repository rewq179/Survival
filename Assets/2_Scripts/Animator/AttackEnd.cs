using UnityEngine;

public class AttackEnd : StateMachineBehaviour
{
    private Unit unit;

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (unit == null)
            unit = animator.GetComponent<Unit>();

        unit.OnAttackAnimationEnd();
    }
}
