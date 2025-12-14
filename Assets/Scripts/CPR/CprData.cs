using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Stores all CPR compression data and statistics
/// </summary>
public class CprData
{
    public int CompressionCount { get; private set; }
    public float CurrentBpm { get; private set; }
    public float SmoothedBpm { get; private set; }
    
    private readonly List<float> allBpms = new List<float>();
    private readonly List<float> allDepths = new List<float>();
    private readonly Queue<float> bpmWindow;
    private readonly int windowSize;
    
    // For time-based BPM calculation
    private float sessionStartTime;
    private bool sessionStarted;
    
    public CprData(int movingAverageWindow = 5)
    {
        windowSize = movingAverageWindow;
        bpmWindow = new Queue<float>();
        sessionStarted = false;
    }
    
    public void RecordCompression(float depth, float bpm)
    {
        if (!sessionStarted)
        {
            sessionStartTime = Time.time;
            sessionStarted = true;
        }
        
        CompressionCount++;
        allDepths.Add(depth);
        
        if (bpm > 0)
        {
            CurrentBpm = bpm;
            allBpms.Add(bpm);
            UpdateMovingAverage(bpm);
        }
    }
    
    private void UpdateMovingAverage(float newBpm)
    {
        bpmWindow.Enqueue(newBpm);
        
        if (bpmWindow.Count > windowSize)
        {
            bpmWindow.Dequeue();
        }
        
        float sum = 0f;
        foreach (float bpm in bpmWindow)
        {
            sum += bpm;
        }
        SmoothedBpm = sum / bpmWindow.Count;
    }
    
    public float GetAverageBpm()
    {
        if (allBpms.Count == 0) return 0f;
        
        float sum = 0f;
        foreach (float bpm in allBpms)
        {
            sum += bpm;
        }
        return sum / allBpms.Count;
    }
    
    // Calculate ACTUAL BPM based on real time elapsed
    public float GetActualBpmOverTime()
    {
        if (!sessionStarted || CompressionCount == 0) return 0f;
        
        float elapsedMinutes = (Time.time - sessionStartTime) / 60f;
        if (elapsedMinutes <= 0) return 0f;
        
        return CompressionCount / elapsedMinutes;
    }
    
    public float GetAverageDepth()
    {
        if (allDepths.Count == 0) return 0f;
        
        float sum = 0f;
        foreach (float depth in allDepths)
        {
            sum += depth;
        }
        return sum / allDepths.Count;
    }
    
    public int GetBpmCount()
    {
        return allBpms.Count;
    }
    
    public int GetDepthCount()
    {
        return allDepths.Count;
    }
    
    public float GetSessionDurationMinutes()
    {
        if (!sessionStarted) return 0f;
        return (Time.time - sessionStartTime) / 60f;
    }
}