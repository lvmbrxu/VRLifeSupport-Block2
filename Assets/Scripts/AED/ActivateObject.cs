using UnityEngine;

[DisallowMultipleComponent]
public sealed class ActivateObjectOnFlag : MonoBehaviour
{
    [Header("Scenario")]
    [SerializeField] private ScenarioDirector scenario;
    [SerializeField] private string requiredFlag = "AedArrived";

    [Header("What to activate")]
    [SerializeField] private GameObject targetObject;

    [Header("Options")]
    [SerializeField] private bool onlyOnce = true;
    [SerializeField] private bool log;

    private bool _done;

    private void Awake()
    {
        if (scenario == null) scenario = FindFirstObjectByType<ScenarioDirector>();
        Apply();
    }

    private void OnEnable()
    {
        if (scenario != null) scenario.FlagsChanged += Apply;
        Apply();
    }

    private void OnDisable()
    {
        if (scenario != null) scenario.FlagsChanged -= Apply;
    }

    private void Apply()
    {
        if (_done && onlyOnce) return;
        if (scenario == null || targetObject == null) return;

        if (scenario.HasFlag(requiredFlag))
        {
            targetObject.SetActive(true);
            if (log) Debug.Log($"[ActivateObjectOnFlag] Activated {targetObject.name} due to {requiredFlag}", this);

            if (onlyOnce) _done = true;
        }
    }
}