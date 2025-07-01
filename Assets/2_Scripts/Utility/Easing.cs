using UnityEngine;

public class Easing
{
    public static float GetCubicOut(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }
}
