using UnityEngine;

public sealed class ScenarioFlagRaiser : MonoBehaviour
{
    [SerializeField] private ScenarioFlag flag;

    public void RaiseFlag()
    {
        ScenarioSignals.Raise(flag);
    }
}