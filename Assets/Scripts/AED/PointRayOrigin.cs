using UnityEngine;

[DisallowMultipleComponent]
public sealed class PointRayOrigin : MonoBehaviour
{
    [Header("Ray")]
    [SerializeField] private Transform rayOrigin;
    [SerializeField] private float maxDistance = 10f;
    [SerializeField] private LayerMask hitMask = ~0;
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

    [Header("Hold to confirm")]
    [SerializeField] private float holdSeconds = 0.6f;

    [Header("Debug")]
    [SerializeField] private bool drawRay = true;
    [SerializeField] private bool log = false;

    private float _holdTimer;
    private PointFlagMoveTarget _current;

    private void Reset() => rayOrigin = transform;

    private void Awake()
    {
        if (rayOrigin == null)
            rayOrigin = transform;
    }

    private void Update()
    {
        if (rayOrigin == null) return;

        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        if (drawRay) Debug.DrawRay(ray.origin, ray.direction * maxDistance);

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, hitMask, triggerInteraction))
        {
            // IMPORTANT: support multiple PointFlagMoveTarget components on the same NPC.
            var targets = hit.collider.GetComponentsInParent<PointFlagMoveTarget>(true);

            PointFlagMoveTarget chosen = null;
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] != null && targets[i].CanInteract())
                {
                    chosen = targets[i];
                    break;
                }
            }

            if (chosen != null)
            {
                if (chosen == _current)
                    _holdTimer += Time.deltaTime;
                else
                {
                    _current = chosen;
                    _holdTimer = 0f;
                }

                if (_holdTimer >= holdSeconds)
                {
                    if (log)
                        Debug.Log($"[PointRayOrigin] Confirm -> {chosen.name} (component: {chosen.GetType().Name})", chosen);

                    _holdTimer = 0f;
                    chosen.Confirm();
                    _current = null; // require re-pointing
                }

                return;
            }
        }

        _current = null;
        _holdTimer = 0f;
    }
}
