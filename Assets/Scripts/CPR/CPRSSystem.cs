using UnityEngine;

[DisallowMultipleComponent]
public sealed class CprSystem : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private CprHandController handController;
    [SerializeField] private CprChestController chestController;
    [SerializeField] private CprEcgDisplay ecgDisplay;
    [SerializeField] private CprUIDisplay uiDisplay;

    [Header("Tuning")]
    [SerializeField, Min(1)] private int compressionsBeforeBpm = 20;
    [SerializeField, Min(0.01f)] private float idealDepth = 0.05f;
    [SerializeField, Min(1f)] private float idealBpm = 100f;
    [SerializeField, Min(0.05f)] private float minSecondsBetweenCompressions = 0.10f;

    [Header("Smoothing / Stats")]
    [SerializeField, Range(1, 20)] private int bpmMovingAverageWindow = 5;
    [SerializeField, Min(0.05f)] private float uiRateUpdateInterval = 0.25f;

    private CprData _data;
    private float _lastCompressionTime = -999f;
    private float _nextRateUiTime;

    private Vector3 _handsCenter;
    private bool _cprActive;

    private void Awake()
    {
        // Safe wiring without Find()
        if (handController == null) handController = GetComponentInChildren<CprHandController>(true);
        if (chestController == null) chestController = GetComponentInChildren<CprChestController>(true);
        if (ecgDisplay == null) ecgDisplay = GetComponentInChildren<CprEcgDisplay>(true);
        if (uiDisplay == null) uiDisplay = GetComponentInChildren<CprUIDisplay>(true);

        _data = new CprData(bpmMovingAverageWindow);

        // Push ideals to dependent systems (keeps inspector cleaner)
        if (ecgDisplay != null)
        {
            ecgDisplay.IdealDepth = idealDepth;
            ecgDisplay.IdealBpm = idealBpm;
        }

        if (uiDisplay != null)
        {
            uiDisplay.IdealBpm = idealBpm;
        }
    }

    private void Start()
    {
        if (chestController != null)
            chestController.Initialize();
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        if (handController == null || chestController == null)
            return;

        handController.Tick(dt);

        _handsCenter = handController.HandsCenter;
        _cprActive = handController.IsCprActive;

        // Compression detection & input force accumulation
        if (chestController.UpdateCompression(_handsCenter, _cprActive, dt, out float depthReached))
        {
            OnCompressionCompleted(depthReached);
        }

        // UI rate display (actual BPM over elapsed time), throttled
        if (uiDisplay != null && Time.time >= _nextRateUiTime)
        {
            _nextRateUiTime = Time.time + uiRateUpdateInterval;
            uiDisplay.UpdateActualBpmDisplay(_data.GetActualBpmOverTime(), _data.GetSessionDurationMinutes());
        }
    }

    private void FixedUpdate()
    {
        if (chestController == null) return;

        chestController.UpdatePhysics(Time.fixedDeltaTime);

        // NEW: make CPR pose sink with the chest
        if (handController != null)
            handController.SetChestWorldOffset(chestController.ChestWorldOffset);
    }


    private void LateUpdate()
    {
        if (ecgDisplay == null) return;
        ecgDisplay.UpdateDisplay(Time.deltaTime);
    }

    private void OnCompressionCompleted(float depth)
    {
        float now = Time.time;
        float dt = now - _lastCompressionTime;
        _lastCompressionTime = now;

        // BPM only valid after some reps and with sane timing.
        float bpm = 0f;
        if (_data.CompressionCount >= compressionsBeforeBpm && dt >= minSecondsBetweenCompressions)
            bpm = 60f / dt;

        _data.RecordCompression(depth, bpm);

        // ECG pulse
        if (ecgDisplay != null)
        {
            float usedBpm = bpm > 0f ? bpm : (_data.SmoothedBpm > 0f ? _data.SmoothedBpm : idealBpm);
            float speed = usedBpm / Mathf.Max(idealBpm, 1f);
            ecgDisplay.TriggerHeartbeat(depth, speed);
        }

        // UI updates
        if (uiDisplay == null) return;

        if (_data.CompressionCount < compressionsBeforeBpm)
        {
            uiDisplay.UpdateProgressDisplay(_data.CompressionCount, compressionsBeforeBpm);
            return;
        }

        float avgBpm = _data.GetAverageBpm();
        float avgDepth = _data.GetAverageDepth();

        uiDisplay.UpdateStatsDisplay(avgBpm, avgDepth);

        var quality = CprQualityCalculator.CalculateQuality(avgBpm, avgDepth, idealBpm, idealDepth);
        uiDisplay.UpdateQualityDisplay(quality);
    }
}
