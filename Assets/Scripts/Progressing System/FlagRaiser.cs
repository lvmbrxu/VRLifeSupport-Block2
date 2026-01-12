using UnityEngine;

public sealed class ScenarioFlagRaiser : MonoBehaviour
{
    [SerializeField] private ScenarioDirector director;
    [SerializeField] private string flagToRaise;

    private void Reset()
    {
        director = FindFirstObjectByType<ScenarioDirector>();
    }

    public void Raise()
    {
        if (director == null)
        {
            director = FindFirstObjectByType<ScenarioDirector>();
            if (director == null)
            {
                Debug.LogError("ScenarioFlagRaiser: No ScenarioDirector found in scene.", this);
                return;
            }
        }

        director.RaiseFlag(flagToRaise);
    }
}