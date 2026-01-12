using System;

public static class ScenarioSignals
{
    public static event Action<ScenarioFlag> OnFlagRaised;

    public static void Raise(ScenarioFlag flag)
    {
        if (flag == ScenarioFlag.None) return;
        OnFlagRaised?.Invoke(flag);
    }
}