using UnityEngine;

[DisallowMultipleComponent]
public sealed class CprHandController : MonoBehaviour
{
    [Header("Hand References")]
    [SerializeField] private Transform leftHand;
    [SerializeField] private Transform rightHand;

    [Header("Hand Visuals")]
    [SerializeField] private GameObject leftHandVisual;
    [SerializeField] private GameObject rightHandVisual;
    [SerializeField] private GameObject cprHandsPose;

    [Header("Overlap Settings")]
    [SerializeField, Min(0.01f)] private float handOverlapDistance = 0.15f;

    [Header("CPR Zone")]
    [Tooltip("Trigger collider on the victim chest zone (Box/Capsule/etc).")]
    [SerializeField] private Collider cprZone;

    [Tooltip("Optional snap/lock point for CPR pose on the chest.")]
    [SerializeField] private Transform cprPoseAnchor;

    [SerializeField, Min(0.00001f)] private float insideEpsilon = 0.001f;

    [Header("Pose Feel")]
    [Tooltip("How snappy the CPR pose follows chest movement (higher = snappier).")]
    [SerializeField, Min(0f)] private float poseFollowChestSharpness = 35f;

    public Vector3 HandsCenter { get; private set; }
    public bool HandsOverlapped { get; private set; }
    public bool IsInCprZone { get; private set; }
    public bool IsCprActive { get; private set; }

    private Vector3 _lockedPos;
    private Quaternion _lockedRot;

    private Vector3 _chestWorldOffset;
    private Vector3 _poseVel;

    private float _overlapDistSqr;

    private void Awake()
    {
        _overlapDistSqr = handOverlapDistance * handOverlapDistance;
    }

    public void SetChestWorldOffset(Vector3 offset) => _chestWorldOffset = offset;

    public void Tick(float dt)
    {
        if (leftHand == null || rightHand == null)
        {
            HandsCenter = Vector3.zero;
            HandsOverlapped = false;
            IsInCprZone = false;
            SetCprActive(false);
            return;
        }

        HandsCenter = (leftHand.position + rightHand.position) * 0.5f;

        // PERF: no sqrt
        Vector3 d = leftHand.position - rightHand.position;
        HandsOverlapped = d.sqrMagnitude < _overlapDistSqr;

        IsInCprZone = (cprZone == null) ? true : IsPointInsideCollider(cprZone, HandsCenter);

        bool shouldBeActive = HandsOverlapped && IsInCprZone;

        if (shouldBeActive != IsCprActive)
            SetCprActive(shouldBeActive);

        if (IsCprActive)
            PinPose(dt);
    }

    private void SetCprActive(bool active)
    {
        if (IsCprActive == active) return;
        IsCprActive = active;

        if (active)
        {
            // Lock pose once on entry
            if (cprPoseAnchor != null)
            {
                _lockedPos = cprPoseAnchor.position;
                _lockedRot = cprPoseAnchor.rotation;
            }
            else
            {
                _lockedPos = HandsCenter;
                _lockedRot = leftHand.rotation;
            }

            if (leftHandVisual != null) leftHandVisual.SetActive(false);
            if (rightHandVisual != null) rightHandVisual.SetActive(false);

            if (cprHandsPose != null)
            {
                cprHandsPose.SetActive(true);
                cprHandsPose.transform.SetPositionAndRotation(_lockedPos, _lockedRot);
                _poseVel = Vector3.zero;
            }
        }
        else
        {
            if (leftHandVisual != null) leftHandVisual.SetActive(true);
            if (rightHandVisual != null) rightHandVisual.SetActive(true);
            if (cprHandsPose != null) cprHandsPose.SetActive(false);
        }
    }

    private void PinPose(float dt)
    {
        if (cprHandsPose == null || !cprHandsPose.activeSelf)
            return;

        Vector3 targetPos = _lockedPos + _chestWorldOffset;
        Quaternion targetRot = _lockedRot;

        if (poseFollowChestSharpness > 0f)
        {
            float smoothTime = 1f / poseFollowChestSharpness;
            Vector3 newPos = Vector3.SmoothDamp(cprHandsPose.transform.position, targetPos, ref _poseVel, smoothTime, Mathf.Infinity, dt);
            cprHandsPose.transform.SetPositionAndRotation(newPos, targetRot);
        }
        else
        {
            cprHandsPose.transform.SetPositionAndRotation(targetPos, targetRot);
        }
    }

    private bool IsPointInsideCollider(Collider col, Vector3 worldPoint)
    {
        Vector3 closest = col.ClosestPoint(worldPoint);
        return (closest - worldPoint).sqrMagnitude <= insideEpsilon * insideEpsilon;
    }
}
