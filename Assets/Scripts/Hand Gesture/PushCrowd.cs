using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public sealed class SendCrowdToExit : MonoBehaviour
{
    [Header("Origin (what area we clear)")]
    [Tooltip("Usually victim/chest center. If null, uses this transform.")]
    [SerializeField] private Transform origin;

    [Header("Who is affected")]
    [Tooltip("Put crowd NPC roots on a dedicated layer (recommended).")]
    [SerializeField] private LayerMask crowdLayers;

    [SerializeField, Min(0.1f)] private float radius = 2.5f;

    [Header("Exit")]
    [Tooltip("Where NPCs should go when cleared. MUST be on the NavMesh if using agents.")]
    [SerializeField] private Transform exitPoint;

    [Header("Optional: Scenario flag")]
    [SerializeField] private ScenarioDirector scenario;
    [SerializeField] private bool raiseCrowdClearedFlag = true;
    [SerializeField] private string crowdClearedFlag = "CrowdCleared";

    [Header("Fallback if no NavMeshAgent")]
    [SerializeField] private float fallbackMoveDistance = 3.0f;

    [Header("Debug")]
    [SerializeField] private bool log = false;

    // Non-alloc overlap (avoid garbage)
    private readonly Collider[] _hits = new Collider[64];

    private void Awake()
    {
        if (scenario == null)
            scenario = FindFirstObjectByType<ScenarioDirector>();
    }

    public void MakeSpace()
    {
        Transform o = origin != null ? origin : transform;
        Vector3 center = o.position;

        int count = Physics.OverlapSphereNonAlloc(center, radius, _hits, crowdLayers, QueryTriggerInteraction.Ignore);

        if (exitPoint == null)
        {
            Debug.LogError("[SendCrowdToExit] exitPoint is NULL. Assign an exit point.", this);
            return;
        }

        for (int i = 0; i < count; i++)
        {
            Collider c = _hits[i];
            if (c == null) continue;

            // Find a sensible root (NPC parent)
            Transform root = c.attachedRigidbody != null ? c.attachedRigidbody.transform : c.transform;

            // Prefer NavMeshAgent if present anywhere on the NPC
            NavMeshAgent agent = root.GetComponentInParent<NavMeshAgent>();
            if (agent == null) agent = root.GetComponentInChildren<NavMeshAgent>();

            if (agent != null && agent.enabled)
            {
                // Let navmesh drive movement (don’t fight with physics)
                Rigidbody rb = root.GetComponentInChildren<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                agent.isStopped = false;
                agent.ResetPath();
                agent.SetDestination(exitPoint.position);

                if (log) Debug.Log($"[SendCrowdToExit] Sent {root.name} to exit.", root);
            }
            else
            {
                // Fallback: move away from the origin
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
