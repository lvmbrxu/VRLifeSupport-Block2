using UnityEngine;

[DisallowMultipleComponent]
public sealed class AedPadWire : MonoBehaviour
{
    [Header("Wire endpoints")]
    [Tooltip("Where the wire starts (AED port).")]
    [SerializeField] private Transform aedPort;

    [Tooltip("Where the wire attaches on the pad (child empty).")]
    [SerializeField] private Transform padWireAttach;

    [Header("Optional: socket target (victim)")]
    [Tooltip("If set, wire will visually go toward this socket when pad is placed.")]
    [SerializeField] private Transform victimSocketTarget;

    [Header("Line Renderer")]
    [SerializeField] private LineRenderer line;

    [Header("Look")]
    [SerializeField, Range(2, 32)] private int segments = 12;
    [SerializeField] private float baseSag = 0.03f;
    [SerializeField] private float sagPerMeter = 0.03f;

    private void Awake()
    {
        if (line == null) line = GetComponentInChildren<LineRenderer>(true);
        if (line == null)
        {
            Debug.LogError($"{nameof(AedPadWire)}: Missing LineRenderer.", this);
            enabled = false;
            return;
        }

        line.positionCount = Mathf.Max(2, segments);
        line.useWorldSpace = true;
        line.enabled = true;
    }

    private void LateUpdate()
    {
        if (aedPort == null || padWireAttach == null || line == null) return;

        Vector3 start = aedPort.position;

        // Wire end is always the pad attach (so it follows your hand)
        Vector3 end = padWireAttach.position;

        // Optional: if you want the cable to look like it “anchors” toward the placed socket,
        // you can change end to socket target instead. BUT simplest is: keep end = pad.
        // We’ll keep it simple: always end at the pad.
        DrawSagWire(start, end);
    }

    private void DrawSagWire(Vector3 a, Vector3 b)
    {
        float dist = Vector3.Distance(a, b);
        float sag = baseSag + dist * sagPerMeter;

        int count = Mathf.Max(2, segments);
        line.positionCount = count;

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / (count - 1);
            Vector3 p = Vector3.Lerp(a, b, t);

            float s = 4f * t * (1f - t); // parabola sag
            p += Vector3.down * (s * sag);

            line.SetPosition(i, p);
        }
    }

    // Call this when pad is placed in a socket (optional if you later want different behavior)
    public void SetVictimSocketTarget(Transform socket)
    {
        victimSocketTarget = socket;
    }
}
