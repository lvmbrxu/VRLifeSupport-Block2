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

    [Header("Movement (Optional)")]
    [Tooltip("If null, will try to find a NavMeshAgent in this object or children.")]
    [SerializeField] private NavMeshAgent agent;

    [Tooltip("If assigned, agent will move here on confirm (exit / go get AED / etc.).")]
    [SerializeField] private Transform moveTo;

    [Tooltip("If true, we warp the agent onto the NavMesh if it's slightly off.")]
    [SerializeField] private bool warpToNavMeshIfNeeded = true;

    [Tooltip("When close enough to moveTo, optionally disable the whole NPC.")]
    [SerializeField] private bool disableAfterArrive = false;

    [SerializeField] private float arriveDistance = 0.7f;

    [Header("Behavior")]
    [SerializeField] private bool onlyOnce = true;

    [Header("Debug")]
    [SerializeField] private bool log = false;

    private bool _done;

    private void Awake()
    {
        if (scenario == null)
            scenario = FindFirstObjectByType<ScenarioDirector>();

        if (agent == null)
            agent = GetComponentInChildren<NavMeshAgent>(true);
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

        // Raise flag
        if (!string.IsNullOrWhiteSpace(raiseFlag))
        {
            scenario.RaiseFlag(raiseFlag.Trim());
            if (log) Debug.Log($"[PointFlagMoveTarget] Confirmed -> Raised flag: {raiseFlag}", this);
        }

        // Move agent (optional)
        if (agent != null && agent.enabled && moveTo != null)
        {
            EnsureOnNavMesh();

            agent.isStopped = false;
            agent.ResetPath();

            bool ok = agent.SetDestination(moveTo.position);
            if (log) Debug.Log($"[PointFlagMoveTarget] SetDestination({moveTo.name}) ok={ok}", this);

            if (disableAfterArrive)
                StartCoroutine(DisableWhenArrived());
        }
        else
        {
            if (log && moveTo != null && agent == null)
                Debug.LogWarning("[PointFlagMoveTarget] moveTo is set but no NavMeshAgent found.", this);
        }
    }

    private void EnsureOnNavMesh()
    {
        if (!warpToNavMeshIfNeeded || agent == null) return;
        if (agent.isOnNavMesh) return;

        if (NavMesh.SamplePosition(agent.transform.position, out var hit, 2f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
            if (log) Debug.Log("[PointFlagMoveTarget] Warped agent onto NavMesh.", this);
        }
        else
        {
            if (log) Debug.LogWarning("[PointFlagMoveTarget] Agent not on NavMesh and couldn't sample nearby.", this);
        }
    }

    private System.Collections.IEnumerator DisableWhenArrived()
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
