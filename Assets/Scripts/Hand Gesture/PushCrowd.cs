using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public sealed class SendCrowdToExit : MonoBehaviour
{
    [Header("Origin (what area we clear)")]
    [Tooltip("Usually the victim/chest center. If null, uses this transform.")]
    [SerializeField] private Transform origin;

    [Header("Who is affected")]
    [SerializeField] private LayerMask crowdLayers;
    [SerializeField, Min(0.1f)] private float radius = 2.5f;

    [Header("Exit")]
    [SerializeField] private Transform exitPoint;

    [Header("Scenario Gate")]
    [SerializeField] private ScenarioDirector scenario;

    [Tooltip("If set, MakeSpace will only work AFTER this flag is raised.")]
    [SerializeField] private string requiredFlag = "CrowdArrived";

    [Header("Optional: Scenario flag")]
    [SerializeField] private bool raiseCrowdClearedFlag = true;
    [SerializeField] private string crowdClearedFlag = "CrowdCleared";

    [Header("Fallback if no NavMeshAgent")]
    [SerializeField] private float fallbackMoveDistance = 1.5f;

    [Header("Debug")]
    [SerializeField] private bool log = false;

    private readonly Collider[] _hits = new Collider[64];

    private void Awake()
    {
        if (scenario == null)
            scenario = FindFirstObjectByType<ScenarioDirector>();
    }

    public void MakeSpace()
    {
        // ---- HARD GATE (fixes your issue) ----
        if (scenario != null && !string.IsNullOrWhiteSpace(requiredFlag))
        {
            if (!scenario.HasFlag(requiredFlag.Trim()))
            {
                if (log) Debug.Log($"[SendCrowdToExit] Blocked. Missing flag: {requiredFlag}", this);
                return;
            }
        }

        if (exitPoint == null)
        {
            Debug.LogError("[SendCrowdToExit] exitPoint is NULL. Assign an exit point.", this);
            return;
        }

        Transform o = origin != null ? origin : transform;
        Vector3 center = o.position;

        int count = Physics.OverlapSphereNonAlloc(center, radius, _hits, crowdLayers, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            Collider c = _hits[i];
            if (c == null) continue;

            Transform root = c.attachedRigidbody != null ? c.attachedRigidbody.transform : c.transform;

            NavMeshAgent agent = root.GetComponentInParent<NavMeshAgent>();
            if (agent == null) agent = root.GetComponentInChildren<NavMeshAgent>();

            if (agent != null && agent.enabled)
            {
                // Prevent physics from fighting the agent
                Rigidbody rb = root.GetComponentInChildren<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                if (!agent.isOnNavMesh && NavMesh.SamplePosition(agent.transform.position, out var hit, 2f, NavMesh.AllAreas))
                    agent.Warp(hit.position);

                agent.isStopped = false;
                agent.ResetPath();
                agent.SetDestination(exitPoint.position);
            }
            else
            {
                // fallback shove
                Vector3 dir = (root.position - center);
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.0001f) dir = o.forward;
                dir.Normalize();
                root.position += dir * fallbackMoveDistance;
            }
        }

        if (raiseCrowdClearedFlag && scenario != null)
            scenario.RaiseFlag(crowdClearedFlag);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Transform o = origin != null ? origin : transform;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(o.position, radius);

        if (exitPoint != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(exitPoint.position, 0.2f);
        }
    }
#endif
}
