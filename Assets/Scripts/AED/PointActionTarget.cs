using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public sealed class PointFlagMoveTarget : MonoBehaviour
{
    [Header("Scenario")]
    [SerializeField] private ScenarioDirector scenario;

    [Tooltip("Must be TRUE to allow interaction. Leave empty to allow anytime.")]
    [SerializeField] private string requiredFlag;

    [Tooltip("Raised when the player successfully points/holds on this target.")]
    [SerializeField] private string raiseFlag;

    [Tooltip("Delay before raising raiseFlag (sync dog leaving / scenario progress).")]
    [SerializeField] private float raiseFlagDelaySeconds = 0f;

    [Header("Behavior")]
    [SerializeField] private bool onlyOnce = true;

    [Header("Movement (Optional)")]
    [Tooltip("If null, will try to find a NavMeshAgent in this object or children.")]
    [SerializeField] private NavMeshAgent agent;

    [Tooltip("If assigned, agent will move here after the delay.")]
    [SerializeField] private Transform moveTo;

    [Tooltip("Wait this long after confirm before starting movement (for VO line time).")]
    [SerializeField] private float moveStartDelaySeconds = 1.5f;

    [Tooltip("If true, we warp the agent onto the NavMesh if it's slightly off.")]
    [SerializeField] private bool warpToNavMeshIfNeeded = true;

    [Tooltip("When close enough to moveTo, optionally disable the whole NPC.")]
    [SerializeField] private bool disableAfterArrive = false;

    [SerializeField] private float arriveDistance = 0.7f;

    [Header("Audio (Optional)")]
    [Tooltip("If null, will try to find an AudioSource in this object or children.")]
    [SerializeField] private AudioSource audioSource;

    [SerializeField] private AudioClip confirmClip;

    [Tooltip("Delay before playing confirmClip after confirm.")]
    [SerializeField] private float audioDelaySeconds = 0.2f;

    [Header("Debug")]
    [SerializeField] private bool log = false;

    private bool _done;
    private Coroutine _routine;

    private void Awake()
    {
        if (scenario == null)
            scenario = FindFirstObjectByType<ScenarioDirector>();

        if (agent == null)
            agent = GetComponentInChildren<NavMeshAgent>(true);

        if (audioSource == null)
            audioSource = GetComponentInChildren<AudioSource>(true);

        if (audioSource != null)
            audioSource.playOnAwake = false;
    }

    public bool CanInteract()
    {
        if (_done && onlyOnce) return false;
        if (scenario == null) return false;

        if (string.IsNullOrWhiteSpace(requiredFlag))
            return true;

        return scenario.HasFlag(requiredFlag.Trim());
    }

    public void Confirm()
    {
        if (!CanInteract())
        {
            if (log) Debug.Log($"[PointFlagMoveTarget] Blocked. Missing required flag: {requiredFlag}", this);
            return;
        }

        if (_done && onlyOnce) return;
        _done = true;

        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(ConfirmRoutine());
    }

    private IEnumerator ConfirmRoutine()
    {
        // Play VO after delay
        if (audioSource != null && confirmClip != null)
        {
            if (audioDelaySeconds > 0f)
                yield return new WaitForSeconds(audioDelaySeconds);

            audioSource.PlayOneShot(confirmClip);
        }

        // Raise the scenario flag after its own delay (sync external systems)
        if (!string.IsNullOrWhiteSpace(raiseFlag) && scenario != null)
        {
            if (raiseFlagDelaySeconds > 0f)
                yield return new WaitForSeconds(raiseFlagDelaySeconds);

            scenario.RaiseFlag(raiseFlag.Trim());
            if (log) Debug.Log($"[PointFlagMoveTarget] Raised flag: {raiseFlag} (after {raiseFlagDelaySeconds:F2}s)", this);
        }

        // Wait before this NPC starts moving (optional)
        if (moveStartDelaySeconds > 0f)
            yield return new WaitForSeconds(moveStartDelaySeconds);

        // Move after wait
        if (agent != null && agent.enabled && moveTo != null)
        {
            EnsureOnNavMesh();

            agent.isStopped = false;
            agent.ResetPath();

            bool ok = agent.SetDestination(moveTo.position);
            if (log) Debug.Log($"[PointFlagMoveTarget] Move start -> {moveTo.name}, ok={ok}, onNavMesh={agent.isOnNavMesh}", this);

            if (disableAfterArrive)
                StartCoroutine(DisableWhenArrived());
        }

        _routine = null;
    }

    private void EnsureOnNavMesh()
    {
        if (!warpToNavMeshIfNeeded || agent == null) return;
        if (agent.isOnNavMesh) return;

        if (NavMesh.SamplePosition(agent.transform.position, out var hit, 2f, NavMesh.AllAreas))
            agent.Warp(hit.position);
    }

    private IEnumerator DisableWhenArrived()
    {
        while (agent != null && agent.enabled)
        {
            if (!agent.pathPending)
            {
                float stop = Mathf.Max(arriveDistance, agent.stoppingDistance + 0.05f);
                if (agent.remainingDistance <= stop)
                    break;
            }
            yield return null;
        }

        gameObject.SetActive(false);
    }
}
