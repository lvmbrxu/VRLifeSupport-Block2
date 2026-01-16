using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public sealed class AedRunnerReturnController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform returnPoint;
    [SerializeField] private float arriveDistance = 0.7f;

    [Header("Scenario")]
    [SerializeField] private ScenarioDirector scenario;
    [SerializeField] private string aedArrivedFlag = "AedArrived";

    [Header("Arrival VO (optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip arrivedClip;
    [SerializeField] private float arrivedClipDelaySeconds = 0.2f;

    [Header("Debug")]
    [SerializeField] private bool log = false;

    private Coroutine _routine;
    private bool _arrivedOnce;

    private void Awake()
    {
        if (agent == null) agent = GetComponentInChildren<NavMeshAgent>(true);
        if (scenario == null) scenario = FindFirstObjectByType<ScenarioDirector>();
        if (audioSource == null) audioSource = GetComponentInChildren<AudioSource>(true);

        if (audioSource != null) audioSource.playOnAwake = false;
    }

    // Hook this to ScenarioTimeline.onRequestAedReturn
    public void ReturnWithAed()
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(ReturnRoutine());
    }

    private IEnumerator ReturnRoutine()
    {
        _arrivedOnce = false;

        if (agent == null || returnPoint == null)
        {
            if (log) Debug.LogWarning("[AedRunnerReturnController] Missing agent or returnPoint.", this);
            yield break;
        }

        if (!agent.isOnNavMesh && NavMesh.SamplePosition(agent.transform.position, out var hit, 2f, NavMesh.AllAreas))
            agent.Warp(hit.position);

        agent.isStopped = false;
        agent.ResetPath();
        agent.SetDestination(returnPoint.position);

        while (agent.pathPending) yield return null;

        while (agent.enabled && agent.remainingDistance > Mathf.Max(arriveDistance, agent.stoppingDistance + 0.05f))
            yield return null;

        if (_arrivedOnce) yield break;
        _arrivedOnce = true;

        if (log) Debug.Log("[AedRunnerReturnController] Arrived with AED.", this);

        // Play VO
        if (audioSource != null && arrivedClip != null)
        {
            if (arrivedClipDelaySeconds > 0f)
                yield return new WaitForSeconds(arrivedClipDelaySeconds);

            audioSource.PlayOneShot(arrivedClip);
        }

        // Raise flag AFTER the VO delay (so 'AED arrived' moment matches the line)
        if (scenario != null && !scenario.HasFlag(aedArrivedFlag))
            scenario.RaiseFlag(aedArrivedFlag);
    }
}
