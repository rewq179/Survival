using UnityEngine;
using System.Collections.Generic;

public class InstantAOELauncher : SkillLauncher
{
    public override SkillLauncherType Type => SkillLauncherType.InstantAOE;

    protected override void OnInitialize()
    {
        base.OnInitialize();

        List<Unit> hitTargets = GetHitTargets(range, isAffectCaster);
        foreach (Unit target in hitTargets)
        {
            OnHitTarget(target);
        }
    }

    protected override void UpdateMovement()
    {

    }
}