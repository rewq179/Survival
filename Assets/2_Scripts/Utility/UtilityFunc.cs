using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using Unity.Burst.Intrinsics;


public static class VectorExtension
{
    public static Vector2 Bezier2D(Vector2 start, Vector2 end, float t)
    {
        Vector2 p1 = (start + end) / 2 + Vector2.up * 100f;
        return Bezier2D(start, p1, end, t);
    }

    public static Vector2 Bezier2D(Vector2 p0, Vector2 p1, Vector2 p2, float t)
    {
        float u = 1 - t;
        return u * u * p0 + 2 * u * t * p1 + t * t * p2;
    }
}

public static class RandomPickerByWeight
{
    public static T PickOne<T>(List<T> items, Func<T, float> weight)
    {
        float total = items.Sum(weight);
        float random = UnityEngine.Random.Range(0f, total);
        float val = 0f;

        foreach (T item in items)
        {
            val += weight(item);
            if (random <= val)
                return item;
        }

        Debug.LogError("확률에 맞는 아이템을 찾을 수 없습니다.");
        return default;
    }
}
