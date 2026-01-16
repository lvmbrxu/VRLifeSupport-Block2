using UnityEngine;

[DisallowMultipleComponent]
public sealed class PointRayOrigin : MonoBehaviour
{
    [Header("Ray")]
    [SerializeField] private Transform rayOrigin;
    [SerializeField] private float maxDistance = 10f;
    [SerializeField] private LayerMask hitMask = ~0;

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

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, hitMask, QueryTriggerInteraction.Ignore))
        {
            var target = hit.collider.GetComponentInParent<PointFlagMoveTarget>();

            if (target != null && target.CanInteract())
            {
                if (target == _current)
                    _holdTimer += Time.deltaTime;
                else
                {
                    _current = target;
                    _holdTimer = 0f;
                }

                if (_holdTimer >= holdSeconds)
                {
                    if (log) Debug.Log($"[PointRayOrigin] Confirm -> {target.name}", target);

                    _holdTimer = 0f;
                    target.Confirm();
                    _current = null; // require re-pointing
                }

                return;
            }
        }

        _current = null;
        _holdTimer = 0f;
    }
}