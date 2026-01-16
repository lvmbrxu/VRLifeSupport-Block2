using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class ScenarioTimeline : MonoBehaviour
{
    [Header("Scenario")]
    [SerializeField] private ScenarioDirector scenario;

    [Header("Flags")]
    [SerializeField] private string crowdClearedFlag = "CrowdCleared";
    [SerializeField] private string aedArrivedFlag = "AedArrived";

    [Header("Timing")]
    [Tooltip("How long after CrowdCleared until AED arrives.")]
    [SerializeField] private float aedReturnDelaySeconds = 10f;

    [Tooltip("How long after AED arrives until results/room transition.")]
    [SerializeField] private float endCountdownSeconds = 20f;

    [Header("Optional: Notify other systems")]
    [Tooltip("Called when we decide AED should come back (e.g., enable runner return).")]
    public UnityEvent onRequestAedReturn;

    [Tooltip("Called exactly when AED arrives (for UI, sounds, etc.).")]
    public UnityEvent onAedArrived;

    [Tooltip("Called when countdown finishes (teleport to results, load scene, etc.).")]
    public UnityEvent onEndScenario;

    [Header("Debug")]
    [SerializeField] private bool log = true;

    private Coroutine _routine;
    private bool _started;

    private void Awake()
    {
        if (scenario == null)
            scenario = FindFirstObjectByType<ScenarioDirector>();
    }

    private void OnEnable()
    {
        if (scenario != null) scenario.FlagsChanged += Evaluate;
        Evaluate();
    }

    private void OnDisable()
    {
        if (scenario != null) scenario.FlagsChanged -= Evaluate;
    }

    private void Evaluate()
    {
        if (_started) return;
        if (scenario == null) return;

        if (scenario.HasFlag(crowdClearedFlag))
        {
            _started = true;
            _routine = StartCoroutine(RunTimeline());
        }
    }

    private IEnumerator RunTimeline()
    {
        if (log) Debug.Log($"[ScenarioTimeline] Crowd cleared. AED returns in {aedReturnDelaySeconds:F1}s", this);

        onRequestAedReturn?.Invoke();

        if (aedReturnDelaySeconds > 0f)
            yield return new WaitForSeconds(aedReturnDelaySeconds);

        if (!scenario.HasFlag(aedArrivedFlag))
        {
            scenario.RaiseFlag(aedArrivedFlag);
            if (log) Debug.Log("[ScenarioTimeline] AED arrived flag raised.", this);
        }

        onAedArrived?.Invoke();

        if (log) Debug.Log($"[ScenarioTimeline] Ending scenario in {endCountdownSeconds:F1}s", this);

        if (endCountdownSeconds > 0f)
            yield return new WaitForSeconds(endCountdownSeconds);

        onEndScenario?.Invoke();
    }
}
