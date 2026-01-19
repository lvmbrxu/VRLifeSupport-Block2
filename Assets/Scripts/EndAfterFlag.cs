using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class EndAfterFlag : MonoBehaviour
{
    [Header("Scenario")]
    [SerializeField] private ScenarioDirector scenario;
    [SerializeField] private string endFlag = "AedShockPressed";

    [Header("Timing")]
    [SerializeField] private float delaySeconds = 30f;

    [Header("What happens at the end")]
    [SerializeField] private UnityEvent onEnd;

    [Header("Debug")]
    [SerializeField] private bool log;

    private bool _started;
    private Coroutine _routine;

    private void Awake()
    {
        if (scenario == null) scenario = FindFirstObjectByType<ScenarioDirector>();
    }

    private void OnEnable()
    {
        if (scenario != null) scenario.FlagsChanged += Evaluate;
        Evaluate();
    }

    private void OnDisable()
    {
        if (scenario != null) scenario.FlagsChanged -= Evaluate;
        if (_routine != null) StopCoroutine(_routine);
    }

    private void Evaluate()
    {
        if (_started) return;
        if (scenario == null) return;

        if (scenario.HasFlag(endFlag))
        {
            _started = true;
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(EndRoutine());
        }
    }

    private IEnumerator EndRoutine()
    {
        if (log) Debug.Log($"[EndAfterFlag] Ending in {delaySeconds}s because {endFlag} is ON", this);
        yield return new WaitForSeconds(delaySeconds);
        onEnd?.Invoke();
    }
}