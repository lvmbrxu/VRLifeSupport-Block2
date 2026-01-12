using UnityEngine;

[DisallowMultipleComponent]
public sealed class ScenarioActionRouter : MonoBehaviour
{
    [SerializeField] private ScenarioProgress progress;
    [SerializeField] private PushAwayCubes pushAwayCubes;

    [Header("Debug")]
    [SerializeField] private bool logBlockedActions = true;

    private void Awake()
    {
        if (progress == null) progress = FindFirstObjectByType<ScenarioProgress>();
    }

    // Hook this to your gesture event
    public void TryMakeSpace()
    {
        if (progress != null && !progress.CanDo(ScenarioAction.MakeSpaceGesture))
        {
            if (logBlockedActions)
                Debug.Log($"[Scenario] MakeSpace blocked (current step: {progress.CurrentStepName})", this);
            return;
        }

        pushAwayCubes?.MakeSpace();

        // Optional: mark as done
        ScenarioSignals.Raise(ScenarioFlag.MadeSpaceGesture);
    }
}