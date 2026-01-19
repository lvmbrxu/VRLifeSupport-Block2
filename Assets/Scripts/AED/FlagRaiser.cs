using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnableWhenFlagRaised : MonoBehaviour
{
    [Header("Scenario")]
    [SerializeField] private ScenarioDirector scenario;
    [SerializeField] private string requiredFlag = "AedArrived";

    [Header("Enable these when flag is ON")]
    [SerializeField] private Behaviour[] enableBehaviours;
    [SerializeField] private GameObject[] enableObjects;

    [Header("Debug")]
    [SerializeField] private bool log;

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
        bool on = scenario != null && scenario.HasFlag(requiredFlag);

        if (enableBehaviours != null)
            for (int i = 0; i < enableBehaviours.Length; i++)
                if (enableBehaviours[i] != null) enableBehaviours[i].enabled = on;

        if (enableObjects != null)
            for (int i = 0; i < enableObjects.Length; i++)
                if (enableObjects[i] != null) enableObjects[i].SetActive(on);

        if (log) Debug.Log($"[EnableWhenFlagRaised] {requiredFlag}={(on ? "ON" : "OFF")} on {name}", this);
    }
}