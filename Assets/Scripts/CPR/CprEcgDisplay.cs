// ==================== CprEcgDisplay.cs ====================
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages ECG waveform visualization (scrolls right to left)
/// </summary>
public class CprEcgDisplay : MonoBehaviour
{
    [Header("ECG Settings")]
    public LineRenderer ecgLine;
    public Transform monitorQuad;
    public int maxPoints = 500;
    public float ecgScale = 0.2f;
    public float baseScrollSpeed = 2f; // Base points added per frame
    
    [Header("Ideal Values for Scaling")]
    public float idealDepth = 0.05f;
    public float idealBpm = 100f;
    
    private Queue<float> ecgValues = new Queue<float>();
    private Queue<float> spikeQueue = new Queue<float>();
    private float currentScrollSpeed;
    
    public void Initialize()
    {
        if (ecgLine == null && monitorQuad != null)
        {
            GameObject lineObj = new GameObject("ECG_Line");
            lineObj.transform.SetParent(monitorQuad);
            lineObj.transform.localPosition = Vector3.zero;
            lineObj.transform.localRotation = Quaternion.identity;
            
            ecgLine = lineObj.AddComponent<LineRenderer>();
            ecgLine.startWidth = 0.004f;
            ecgLine.endWidth = 0.004f;
            ecgLine.material = new Material(Shader.Find("Sprites/Default"));
            ecgLine.startColor = new Color(0.2f, 1f, 0.3f);
            ecgLine.endColor = new Color(0.2f, 1f, 0.3f);
            ecgLine.useWorldSpace = false;
            ecgLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ecgLine.receiveShadows = false;
        }
        
        currentScrollSpeed = baseScrollSpeed;
        
        // Fill with baseline values
        for (int i = 0; i < maxPoints; i++)
        {
            ecgValues.Enqueue(0f);
        }
    }
    
    public void TriggerHeartbeat(float depth, float speed)
    {
        // Adjust scroll speed based on compression rate
        // Faster compressions = faster scroll
        currentScrollSpeed = baseScrollSpeed * Mathf.Clamp(speed, 0.5f, 2f);
        
        // Queue the spike to be added immediately on the right side
        QueueHeartbeatSpike(depth, speed);
    }
    
    public void UpdateDisplay()
    {
        // Smoothly interpolate scroll speed back to base when not actively spiking
        if (spikeQueue.Count == 0)
        {
            currentScrollSpeed = Mathf.Lerp(currentScrollSpeed, baseScrollSpeed, Time.deltaTime * 2f);
        }
        
        // Add spike values if we're in the middle of a heartbeat
        if (spikeQueue.Count > 0)
        {
            // Add points based on current scroll speed
            int pointsToAdd = Mathf.Max(1, Mathf.RoundToInt(currentScrollSpeed));
            for (int i = 0; i < pointsToAdd && spikeQueue.Count > 0; i++)
            {
                AddToEcg(spikeQueue.Dequeue());
            }
        }
        else
        {
            // Add baseline when not spiking (still scrolls)
            int baselinePoints = Mathf.Max(1, Mathf.RoundToInt(currentScrollSpeed * 0.5f));
            for (int i = 0; i < baselinePoints; i++)
            {
                AddToEcg(0f);
            }
        }
        
        UpdateEcgLine();
    }
    
    private void QueueHeartbeatSpike(float depth, float speed)
    {
        float depthIntensity = depth / idealDepth;
        float speedMultiplier = Mathf.Clamp(speed, 0.5f, 1.5f);
        
        float pWaveHeight = Mathf.Lerp(0.03f, 0.06f, depthIntensity) * speedMultiplier;
        float rWaveHeight = Mathf.Lerp(0.5f, 1.5f, depthIntensity) * speedMultiplier;
        float tWaveHeight = Mathf.Lerp(0.15f, 0.25f, depthIntensity) * speedMultiplier;
        
        // Adjust waveform length based on speed (faster = shorter/compressed)
        int waveformPoints = Mathf.RoundToInt(Mathf.Lerp(3f, 1f, speedMultiplier - 0.5f));
        waveformPoints = Mathf.Max(1, waveformPoints);
        
        // P wave
        for (int i = 0; i < 2 * waveformPoints; i++) spikeQueue.Enqueue(pWaveHeight * 0.7f);
        for (int i = 0; i < 2 * waveformPoints; i++) spikeQueue.Enqueue(pWaveHeight);
        for (int i = 0; i < 2 * waveformPoints; i++) spikeQueue.Enqueue(pWaveHeight * 0.7f);
        
        // PR segment
        for (int i = 0; i < 3 * waveformPoints; i++) spikeQueue.Enqueue(0f);
        
        // Q wave
        for (int i = 0; i < 1 * waveformPoints; i++) spikeQueue.Enqueue(-0.08f * depthIntensity);
        
        // R wave (sharp peak) - always sharp regardless of speed
        spikeQueue.Enqueue(0.1f * rWaveHeight);
        spikeQueue.Enqueue(0.4f * rWaveHeight);
        spikeQueue.Enqueue(0.8f * rWaveHeight);
        spikeQueue.Enqueue(1.2f * rWaveHeight);
        spikeQueue.Enqueue(1.0f * rWaveHeight);
        spikeQueue.Enqueue(0.6f * rWaveHeight);
        spikeQueue.Enqueue(0.2f * rWaveHeight);
        
        // S wave
        for (int i = 0; i < 2 * waveformPoints; i++) spikeQueue.Enqueue(-0.12f * depthIntensity);
        for (int i = 0; i < 1 * waveformPoints; i++) spikeQueue.Enqueue(-0.08f * depthIntensity);
        
        // ST segment
        for (int i = 0; i < 4 * waveformPoints; i++) spikeQueue.Enqueue(0f);
        
        // T wave
        spikeQueue.Enqueue(0.06f * tWaveHeight);
        spikeQueue.Enqueue(0.14f * tWaveHeight);
        spikeQueue.Enqueue(0.20f * tWaveHeight);
        spikeQueue.Enqueue(0.24f * tWaveHeight);
        spikeQueue.Enqueue(0.20f * tWaveHeight);
        spikeQueue.Enqueue(0.14f * tWaveHeight);
        spikeQueue.Enqueue(0.06f * tWaveHeight);
        
        // Baseline before next beat (shorter for faster rates)
        int baselineLength = Mathf.RoundToInt(Mathf.Lerp(15f, 5f, speedMultiplier - 0.5f));
        for (int i = 0; i < baselineLength; i++) spikeQueue.Enqueue(0f);
    }
    
    private void AddToEcg(float value)
    {
        if (ecgValues.Count >= maxPoints)
        {
            ecgValues.Dequeue();
        }
        ecgValues.Enqueue(value);
    }
    
    private void UpdateEcgLine()
    {
        if (ecgLine == null) return;
        
        ecgLine.positionCount = ecgValues.Count;
        int index = 0;
        
        // Render RIGHT to LEFT (newest on right, oldest on left)
        float[] valuesArray = ecgValues.ToArray();
        for (int i = valuesArray.Length - 1; i >= 0; i--)
        {
            // Map: rightmost point = newest value, leftmost = oldest
            float x = ((float)index / maxPoints) * 0.8f - 0.4f;
            float y = valuesArray[i] * ecgScale;
            ecgLine.SetPosition(index, new Vector3(x, y, -0.01f));
            index++;
        }
    }
}