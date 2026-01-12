using UnityEngine;

public sealed class PointAtRequester : MonoBehaviour
{
    [Header("Ray")]
    [SerializeField] private Transform rayOrigin;
    [SerializeField] private float maxDistance = 10f;
    [SerializeField] private LayerMask hitMask = ~0;

    [Header("Hold to confirm")]
    [SerializeField] private float holdSeconds = 0.6f;

    [Header("Debug")]
    [SerializeField] private bool drawRay = true;

    private float _holdTimer;
    private RequestAedTarget _currentTarget;

    private void Reset()
    {
        rayOrigin = transform;
    }

    private void Update()
    {
        if (rayOrigin == null) return;

        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);

        if (drawRay)
            Debug.DrawRay(ray.origin, ray.direction * maxDistance);

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, hitMask, QueryTriggerInteraction.Ignore))
        {
            var target = hit.collider.GetComponentInParent<RequestAedTarget>();

            if (target != null)
            {
                // Same target -> accumulate hold time
                if (target == _currentTarget)
                {
                    _holdTimer += Time.deltaTime;
                }
                else
                {
                    _currentTarget = target;
                    _holdTimer = 0f;
                }

                // Confirm
                if (_holdTimer >= holdSeconds)
                {
                    _holdTimer = 0f;
                    _currentTarget.ConfirmRequest();
                    _currentTarget = null; // require re-pointing
                }

                return;
            }
        }

        // Not hitting a valid target
        _currentTarget = null;
        _holdTimer = 0f;
    }
}