using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;

[DisallowMultipleComponent]
public sealed class TwoHandStopPushGestureXRHands : MonoBehaviour
{
    [Header("References")]
    [Tooltip("XR Origin camera / head. If null, uses Camera.main.")]
    [SerializeField] private Transform head;

    [Header("Stop Pose Requirements")]
    [Tooltip("Both palms must face camera forward by at least this dot product.")]
    [Range(0f, 1f)] public float palmFacingForward = 0.6f;

    [Tooltip("Fingertips must be at least this far from palm center to count as 'open hand'.")]
    public float fingerOpenDistance = 0.06f;

    [Tooltip("Palms must be in front of head by this distance (meters).")]
    public float minPalmInFront = 0.20f;

    [Tooltip("Palms must be at least this far apart so it doesn't trigger with one hand.")]
    public float minHandsSeparation = 0.20f;

    [Tooltip("How long the 'stop pose' must be held before we allow pushing.")]
    public float armHoldSeconds = 0.15f;

    [Header("Push Detection")]
    [Tooltip("Both hands must move forward faster than this to trigger (m/s).")]
    public float pushSpeed = 0.6f;

    [Tooltip("Optional: allow trigger if the average forward speed is high enough.")]
    public bool allowAverageSpeedTrigger = true;

    [Tooltip("Cooldown to prevent repeated triggering.")]
    public float cooldownSeconds = 1.0f;

    [Header("Events")]
    public UnityEvent onStopAndPush;

    // Internals
    private XRHandSubsystem _subsystem;

    private Vector3 _prevLeftPalmPos;
    private Vector3 _prevRightPalmPos;
    private float _prevTime;

    private float _armTimer;
    private float _cooldown;

    private void Awake()
    {
        if (head == null && Camera.main != null)
            head = Camera.main.transform;
    }

    private void OnEnable()
    {
        _subsystem = GetHandSubsystem();
        _prevTime = Time.time;
        _armTimer = 0f;
        _cooldown = 0f;
    }

    private void Update()
    {
        if (_subsystem == null || head == null)
            return;

        if (_cooldown > 0f)
        {
            _cooldown -= Time.deltaTime;
            return;
        }

        XRHand left = _subsystem.leftHand;
        XRHand right = _subsystem.rightHand;

        if (!left.isTracked || !right.isTracked)
        {
            _armTimer = 0f;
            return;
        }

        if (!TryGetPalmPose(left, out Vector3 leftPalmPos, out Vector3 leftPalmNormal) ||
            !TryGetPalmPose(right, out Vector3 rightPalmPos, out Vector3 rightPalmNormal))
        {
            _armTimer = 0f;
            return;
        }

        // --- STOP pose checks ---
        bool leftOpen = IsOpenPalm(left, leftPalmPos);
        bool rightOpen = IsOpenPalm(right, rightPalmPos);

        bool leftFacing = Vector3.Dot(leftPalmNormal, head.forward) > palmFacingForward;
        bool rightFacing = Vector3.Dot(rightPalmNormal, head.forward) > palmFacingForward;

        // In front of head
        bool leftInFront = Vector3.Dot(leftPalmPos - head.position, head.forward) > minPalmInFront;
        bool rightInFront = Vector3.Dot(rightPalmPos - head.position, head.forward) > minPalmInFront;

        // Hands separated
        float separation = Vector3.Distance(leftPalmPos, rightPalmPos);
        bool separatedEnough = separation > minHandsSeparation;

        bool stopPose = leftOpen && rightOpen && leftFacing && rightFacing && leftInFront && rightInFront && separatedEnough;

        // --- Velocity for push detection ---
        float now = Time.time;
        float dt = Mathf.Max(0.0001f, now - _prevTime);

        Vector3 leftVel = (leftPalmPos - _prevLeftPalmPos) / dt;
        Vector3 rightVel = (rightPalmPos - _prevRightPalmPos) / dt;

        float leftForwardSpeed = Vector3.Dot(leftVel, head.forward);
        float rightForwardSpeed = Vector3.Dot(rightVel, head.forward);
        float avgForwardSpeed = (leftForwardSpeed + rightForwardSpeed) * 0.5f;

        _prevLeftPalmPos = leftPalmPos;
        _prevRightPalmPos = rightPalmPos;
        _prevTime = now;

        // Arm the gesture only after holding stop pose briefly (reduces false triggers)
        if (stopPose)
            _armTimer += Time.deltaTime;
        else
            _armTimer = 0f;

        bool armed = _armTimer >= armHoldSeconds;
        if (!armed)
            return;

        // Push trigger: both hands forward fast, OR average is fast (configurable)
        bool bothPush = leftForwardSpeed > pushSpeed && rightForwardSpeed > pushSpeed;
        bool avgPush = allowAverageSpeedTrigger && avgForwardSpeed > pushSpeed;

        if (bothPush || avgPush)
        {
            _cooldown = cooldownSeconds;
            _armTimer = 0f;
            onStopAndPush?.Invoke();
        }
    }

    private XRHandSubsystem GetHandSubsystem()
    {
        var settings = XRGeneralSettings.Instance;
        if (settings == null || settings.Manager == null) return null;

        var loader = settings.Manager.activeLoader;
        if (loader == null) return null;

        return loader.GetLoadedSubsystem<XRHandSubsystem>();
    }

    private bool TryGetPalmPose(XRHand hand, out Vector3 palmPos, out Vector3 palmNormal)
    {
        palmPos = default;
        palmNormal = default;

        if (!hand.GetJoint(XRHandJointID.Wrist).TryGetPose(out Pose wrist)) return false;
        if (!hand.GetJoint(XRHandJointID.IndexMetacarpal).TryGetPose(out Pose index)) return false;
        if (!hand.GetJoint(XRHandJointID.LittleMetacarpal).TryGetPose(out Pose little)) return false;

        palmPos = (wrist.position + index.position + little.position) / 3f;

        Vector3 a = index.position - wrist.position;
        Vector3 b = little.position - wrist.position;

        palmNormal = Vector3.Cross(a, b).normalized;

        // Make normal consistent: point roughly where the player looks
        if (Vector3.Dot(palmNormal, head.forward) < 0f)
            palmNormal = -palmNormal;

        return true;
    }

    private bool IsOpenPalm(XRHand hand, Vector3 palmPos)
    {
        // Simple “student style” check: fingertips far from palm => open.
        return TipFar(hand, XRHandJointID.ThumbTip, palmPos) &&
               TipFar(hand, XRHandJointID.IndexTip, palmPos) &&
               TipFar(hand, XRHandJointID.MiddleTip, palmPos) &&
               TipFar(hand, XRHandJointID.RingTip, palmPos) &&
               TipFar(hand, XRHandJointID.LittleTip, palmPos);
    }

    private bool TipFar(XRHand hand, XRHandJointID tip, Vector3 palmPos)
    {
        if (!hand.GetJoint(tip).TryGetPose(out Pose tipPose)) return false;
        return Vector3.Distance(tipPose.position, palmPos) > fingerOpenDistance;
    }
}
