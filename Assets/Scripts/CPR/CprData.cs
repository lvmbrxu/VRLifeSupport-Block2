using UnityEngine;

public sealed class CprData
{
    public int CompressionCount { get; private set; }
    public float CurrentBpm { get; private set; }
    public float SmoothedBpm { get; private set; }

    private float _sumDepth;
    private int _depthCount;

    private float _sumBpm;
    private int _bpmCount;

    private readonly float[] _bpmWindow;
    private int _bpmWindowCount;
    private int _bpmWindowIndex;
    private float _bpmWindowSum;

    private float _sessionStartTime;
    private bool _sessionStarted;

    public CprData(int movingAverageWindow = 5)
    {
        movingAverageWindow = Mathf.Clamp(movingAverageWindow, 1, 50);
        _bpmWindow = new float[movingAverageWindow];
    }

    public void RecordCompression(float depth, float bpm)
    {
        if (!_sessionStarted)
        {
            _sessionStarted = true;
            _sessionStartTime = Time.time;
        }

        CompressionCount++;

        _sumDepth += depth;
        _depthCount++;

        if (bpm > 0f)
        {
            CurrentBpm = bpm;

            _sumBpm += bpm;
            _bpmCount++;

            AddToMovingAverage(bpm);
        }
    }

    private void AddToMovingAverage(float bpm)
    {
        if (_bpmWindowCount < _bpmWindow.Length)
        {
            _bpmWindow[_bpmWindowIndex] = bpm;
            _bpmWindowSum += bpm;
            _bpmWindowCount++;
        }
        else
        {
            // Replace oldest
            _bpmWindowSum -= _bpmWindow[_bpmWindowIndex];
            _bpmWindow[_bpmWindowIndex] = bpm;
            _bpmWindowSum += bpm;
        }

        _bpmWindowIndex = (_bpmWindowIndex + 1) % _bpmWindow.Length;
        SmoothedBpm = _bpmWindowSum / Mathf.Max(1, _bpmWindowCount);
    }

    public float GetAverageBpm()
    {
        return _bpmCount > 0 ? (_sumBpm / _bpmCount) : 0f;
    }

    public float GetAverageDepth()
    {
        return _depthCount > 0 ? (_sumDepth / _depthCount) : 0f;
    }

    public float GetActualBpmOverTime()
    {
        if (!_sessionStarted || CompressionCount == 0) return 0f;

        float elapsedMinutes = (Time.time - _sessionStartTime) / 60f;
        if (elapsedMinutes <= 0f) return 0f;

        return CompressionCount / elapsedMinutes;
    }

    public float GetSessionDurationMinutes()
    {
        if (!_sessionStarted) return 0f;
        return (Time.time - _sessionStartTime) / 60f;
    }
}
