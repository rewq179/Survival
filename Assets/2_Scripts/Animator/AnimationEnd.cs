using UnityEngine;

public class AnimationEnd : StateMachineBehaviour
{
    public AnimEvent animEvent;
    private Unit unit;
    private bool isDeactivated;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (unit == null)
            unit = animator.GetComponent<Unit>();

        isDeactivated = false;
    }
    
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (isDeactivated || stateInfo.normalizedTime < 0.9f)
            return;

        unit.OnAnimationEnd(animEvent);
        isDeactivated = true;
    }
}
