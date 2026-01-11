using UnityEngine;

[DisallowMultipleComponent]
public sealed class PushAwayCubes : MonoBehaviour
{
    [Header("Origin (where you want space)")]
    [Tooltip("Usually the victim/chest center. If null, uses this transform.")]
    [SerializeField] private Transform origin;

    [Header("Which objects get pushed")]
    [Tooltip("Put your cubes on a dedicated layer (recommended).")]
    [SerializeField] private LayerMask pushableLayers;

    [SerializeField, Min(0.1f)] private float radius = 2.5f;

    [Header("Push Settings")]
    [SerializeField, Min(0f)] private float pushForce = 3.0f;
    [SerializeField, Min(0f)] private float upwardForce = 0.2f;

    [Tooltip("If true, pushes only on the horizontal plane (looks nicer).")]
    [SerializeField] private bool flattenY = true;

    [Tooltip("If a cube has no Rigidbody, we still move it a bit.")]
    [SerializeField, Min(0f)] private float fallbackMoveDistance = 0.7f;

    // Non-alloc overlap (avoid garbage)
    private readonly Collider[] _hits = new Collider[64];

    public void MakeSpace()
    {
        Transform o = origin != null ? origin : transform;
        Vector3 center = o.position;

        int count = Physics.OverlapSphereNonAlloc(center, radius, _hits, pushableLayers, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            Collider c = _hits[i];
            if (c == null) continue;

            Rigidbody rb = c.attachedRigidbody;

            Vector3 dir = (c.transform.position - center);
            if (flattenY) dir.y = 0f;

            if (dir.sqrMagnitude < 0.0001f)
                dir = o.forward;

            dir.Normalize();

            if (rb != null && !rb.isKinematic)
            {
                // Push with physics
                Vector3 force = dir * pushForce + Vector3.up * upwardForce;
                rb.AddForce(force, ForceMode.VelocityChange);
            }
            else
            {
                // Fallback: move transform a bit
                c.transform.position += dir * fallbackMoveDistance;
            }
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Transform o = origin != null ? origin : transform;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(o.position, radius);
    }
#endif
}
    