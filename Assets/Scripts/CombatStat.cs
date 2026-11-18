using System;

public static class CombatStat
{
    public static float Round(float value) => (float)Math.Round(value, MidpointRounding.AwayFromZero);
}
