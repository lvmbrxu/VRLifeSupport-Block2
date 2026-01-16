using UnityEngine;

[DisallowMultipleComponent]
public sealed class CprEcgDisplay : MonoBehaviour
{
    [Header("Placement (Monitor)")]
    [SerializeField] private Transform monitorRoot;
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 0f, -0.01f);
    [SerializeField] private bool autoCreateLineRenderer = true;

    [Header("Line Renderer")]
    [SerializeField] private LineRenderer ecgLine;

    [Tooltip("Assign a material in Inspector. If null, uses a shared runtime fallback (no leaks).")]
    [SerializeField] private Material lineMaterial;

    [SerializeField, Min(0.0001f)] private float lineWidth = 0.004f;

    [Header("Wave Settings")]
    [SerializeField, Min(16)] private int maxPoints = 500;
    [SerializeField, Min(0.0001f)] private float ecgScale = 0.2f;
    [SerializeField, Min(0.1f)] private float baseScrollSpeed = 2f;
    [SerializeField] private float xWidth = 0.8f;

    [HideInInspector] public float IdealDepth = 0.05f;
    [HideInInspector] public float IdealBpm = 100f;

    private float[] _values;
    private int _writeIndex;

    private Vector3[] _positions;
    private float[] _x;

    private float[] _spikeQueue;
    private int _spikeHead;
    private int _spikeCount;

    private float _currentScrollSpeed;
    private bool _ready;

    private static Material s_fallbackLineMat;

    private void Awake()
    {
        EnsureLineRenderer();
        Allocate();
        ResetBaseline();

        _currentScrollSpeed = baseScrollSpeed;
        _ready = true;

        ecgLine.positionCount = maxPoints;
        ecgLine.SetPositions(_positions);
    }

    private void EnsureLineRenderer()
    {
        if (ecgLine != null)
        {
            ConfigureLineRenderer(ecgLine);
            return;
        }

        if (!autoCreateLineRenderer)
        {
            Debug.LogError($"{nameof(CprEcgDisplay)}: Missing LineRenderer reference.", this);
            enabled = false;
            return;
        }

        Transform parent = monitorRoot != null ? monitorRoot : transform;

        var go = new GameObject("ECG_Line");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localOffset;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        ecgLine = go.AddComponent<LineRenderer>();
        ConfigureLineRenderer(ecgLine);
    }

    private void ConfigureLineRenderer(LineRenderer lr)
    {
        lr.useWorldSpace = false;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;

        // IMPORTANT: do NOT new Material every time (leaks/duplicates)
        if (lineMaterial != null)
        {
            lr.sharedMaterial = lineMaterial;
        }
        else
        {
            if (s_fallbackLineMat == null)
                s_fallbackLineMat = new Material(Shader.Find("Sprites/Default"));
            lr.sharedMaterial = s_fallbackLineMat;
        }

        lr.startColor = new Color(0.2f, 1f, 0.3f);
        lr.endColor = new Color(0.2f, 1f, 0.3f);

        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.numCornerVertices = 0;
        lr.numCapVertices = 0;
        lr.alignment = LineAlignment.TransformZ;
    }

    private void Allocate()
    {
        _values = new float[maxPoints];
        _positions = new Vector3[maxPoints];
        _x = new float[maxPoints];

        float half = xWidth * 0.5f;
        for (int i = 0; i < maxPoints; i++)
        {
            float t = (float)i / (maxPoints - 1);
            _x[i] = Mathf.Lerp(-half, half, t);
            _positions[i] = new Vector3(_x[i], 0f, 0f);
        }

        _spikeQueue = new float[maxPoints * 4];
        _spikeHead = 0;
        _spikeCount = 0;
        _writeIndex = 0;
    }

    public void ResetBaseline()
    {
        for (int i = 0; i < _values.Length; i++)
            _values[i] = 0f;

        _writeIndex = 0;
        _spikeHead = 0;
        _spikeCount = 0;

        if (ecgLine != null && _positions != null)
        {
            for (int i = 0; i < _positions.Length; i++)
                _positions[i].y = 0f;

            ecgLine.positionCount = maxPoints;
            ecgLine.SetPositions(_positions);
        }
    }

    public void TriggerHeartbeat(float depth, float speed)
    {
        _currentScrollSpeed = baseScrollSpeed * Mathf.Clamp(speed, 0.5f, 2f);
        QueueHeartbeatSpike(depth, speed);
    }

    public void UpdateDisplay(float dt)
    {
        if (!_ready || ecgLine == null) return;

        if (_spikeCount == 0)
            _currentScrollSpeed = Mathf.Lerp(_currentScrollSpeed, baseScrollSpeed, dt * 2f);

        int pointsToAdd = Mathf.Max(1, Mathf.RoundToInt(_currentScrollSpeed));
        for (int i = 0; i < pointsToAdd; i++)
        {
            float v = (_spikeCount > 0) ? DequeueSpike() : 0f;
            WriteValue(v);
        }

        UpdateLine();
    }

    private void WriteValue(float v)
    {
        _values[_writeIndex] = v;
        _writeIndex = (_writeIndex + 1) % _values.Length;
    }

    private void UpdateLine()
    {
        for (int i = 0; i < maxPoints; i++)
        {
            int src = (_writeIndex + i) % maxPoints;
            _positions[i].y = _values[src] * ecgScale;
        }

        ecgLine.SetPositions(_positions);
    }

    private void EnqueueSpike(float v)
    {
        if (_spikeCount == _spikeQueue.Length)
        {
            _spikeHead = (_spikeHead + 1) % _spikeQueue.Length;
            _spikeCount--;
        }

        int tail = (_spikeHead + _spikeCount) % _spikeQueue.Length;
        _spikeQueue[tail] = v;
        _spikeCount++;
    }

    private float DequeueSpike()
    {
        float v = _spikeQueue[_spikeHead];
        _spikeHead = (_spikeHead + 1) % _spikeQueue.Length;
        _spikeCount--;
        return v;
    }

    private void QueueHeartbeatSpike(float depth, float speed)
    {
        float depthIntensity = IdealDepth > 0f ? (depth / IdealDepth) : 1f;
        float speedMultiplier = Mathf.Clamp(speed, 0.5f, 1.5f);

        float pWaveHeight = Mathf.Lerp(0.03f, 0.06f, depthIntensity) * speedMultiplier;
        float rWaveHeight = Mathf.Lerp(0.5f, 1.5f, depthIntensity) * speedMultiplier;
        float tWaveHeight = Mathf.Lerp(0.15f, 0.25f, depthIntensity) * speedMultiplier;

        int w = Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(3f, 1f, speedMultiplier - 0.5f)));

        for (int i = 0; i < 2 * w; i++) EnqueueSpike(pWaveHeight * 0.7f);
        for (int i = 0; i < 2 * w; i++) EnqueueSpike(pWaveHeight);
        for (int i = 0; i < 2 * w; i++) EnqueueSpike(pWaveHeight * 0.7f);

        for (int i = 0; i < 3 * w; i++) EnqueueSpike(0f);

        for (int i = 0; i < 1 * w; i++) EnqueueSpike(-0.08f * depthIntensity);

        EnqueueSpike(0.1f * rWaveHeight);
        EnqueueSpike(0.3f * rWaveHeight);
        EnqueueSpike(0.6f * rWaveHeight);
        EnqueueSpike(0.9f * rWaveHeight);
        EnqueueSpike(1.2f * rWaveHeight);
        EnqueueSpike(1.0f * rWaveHeight);
        EnqueueSpike(0.7f * rWaveHeight);
        EnqueueSpike(0.4f * rWaveHeight);
        EnqueueSpike(0.1f * rWaveHeight);

        for (int i = 0; i < 2 * w; i++) EnqueueSpike(-0.12f * depthIntensity);

        for (int i = 0; i < 4 * w; i++) EnqueueSpike(0f);

        EnqueueSpike(0.06f * tWaveHeight);
        EnqueueSpike(0.12f * tWaveHeight);
        EnqueueSpike(0.18f * tWaveHeight);
        EnqueueSpike(0.22f * tWaveHeight);
        EnqueueSpike(0.24f * tWaveHeight);
        EnqueueSpike(0.22f * tWaveHeight);
        EnqueueSpike(0.18f * tWaveHeight);
        EnqueueSpike(0.12f * tWaveHeight);
        EnqueueSpike(0.06f * tWaveHeight);

        for (int i = 0; i < 6 * w; i++) EnqueueSpike(0f);
    }
}
