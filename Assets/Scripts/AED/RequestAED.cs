using UnityEngine;

public sealed class RequestAedTarget : MonoBehaviour
{
    [Header("Scenario")]
    [SerializeField] private ScenarioDirector scenario;
    [SerializeField] private string requestFlag = "RequestedAED";

    [Header("Runner (optional)")]
    [SerializeField] private AedRunnerCube runner;

    [Header("One-shot")]
    [SerializeField] private bool onlyOnce = true;

    private bool _done;

    public void ConfirmRequest()
    {
        if (onlyOnce && _done) return;
        _done = true;

        if (scenario == null)
        {
            Debug.LogError("[RequestAedTarget] Missing ScenarioDirector reference.", this);
            return;
        }

        // Raise the scenario progress flag
        scenario.RaiseFlag(requestFlag);

        // Make the cube "go get the AED"
        if (runner != null)
            runner.GoGetAed();
    }
}