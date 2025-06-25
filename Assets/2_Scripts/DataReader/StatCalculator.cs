using UnityEngine;

public interface IStatCalulator
{
    public void Sum(int value, ref int result);
    public void Sum(float value, ref float result);
    public void Multi(float value, ref float result);
}

public class StatCalculator : IStatCalulator
{
    private static IStatCalulator calculator;
    public static IStatCalulator basic
    {
        get
        {
            if (calculator == null)
                calculator = new StatCalculator();
            return calculator;
        }
    }

    public void Sum(int value, ref int result)
    {
        result += value;
    }

    public void Sum(float value, ref float result)
    {
        result += value;
    }

    public void Multi(float value, ref float result)
    {
        result *= 1 + value;
    }
}