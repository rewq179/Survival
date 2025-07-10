using UnityEngine;

public class DescMgr
{
    public static string GetSubSkillDescription(string desc, float value)
    {
        if (desc.Contains("{P0}"))
            desc = desc.Replace("{P0}", (value * 100).ToString("F1") + "%");

        else if (desc.Contains("{F0}"))
            desc = desc.Replace("{F0}", value.ToString("F1"));

        return desc;
    }
}