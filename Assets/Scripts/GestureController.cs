using UnityEngine;

public class MonitorGestureController : MonoBehaviour
{
    [Header("Assign")]
    public Transform rightHand;
    public Transform monitorQuad;
    public Transform playerCamera;

    [Header("Move Settings")]
    public float moveDistance = 50f;
    public Vector3 moveDirection = Vector3.forward;
    public Space moveSpace = Space.World;

    [Header("Detection Settings")]
    [Range(0f, 1f)]
    public float lookDotThreshold = 0.8f;
    [Range(0f, 1f)]
    public float pointDotThreshold = 0.8f;
    public float maxPointDistance = 5f;

    private bool _wasActive = false;

    void Update()
    {
        if (rightHand == null || monitorQuad == null || playerCamera == null)
            return;

        bool looking  = IsLookingAtMonitor();
        bool pointing = IsPointingAtMonitor();
        bool activeNow = looking && pointing;

        if (activeNow && !_wasActive)
            MoveMonitor();

        _wasActive = activeNow;
    }

    bool IsLookingAtMonitor()
    {
        Vector3 camToMonitor = (monitorQuad.position - playerCamera.position).normalized;
        float dot = Vector3.Dot(playerCamera.forward, camToMonitor);
        return dot >= lookDotThreshold;
    }

    bool IsPointingAtMonitor()
    {
        Vector3 handToMonitor = monitorQuad.position - rightHand.position;
        float distance = handToMonitor.magnitude;
        if (distance > maxPointDistance) return false;

        handToMonitor.Normalize();
        float dot = Vector3.Dot(rightHand.forward, handToMonitor);
        return dot >= pointDotThreshold;
    }

    void MoveMonitor()
    {
        if (monitorQuad == null) return;

        Vector3 dir = moveDirection.normalized;
        if (dir.sqrMagnitude < 0.0001f) return;

        if (moveSpace == Space.Self)
            dir = monitorQuad.TransformDirection(dir);

        monitorQuad.position += dir * moveDistance;
        Debug.Log("Monitor moved");
    }
}