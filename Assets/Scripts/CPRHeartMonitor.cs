using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class CprHeartMonitor : MonoBehaviour
{
    [Header("ASSIGN THESE")]
    public Transform leftHand;
    public Transform rightHand;
    public Transform chestPlate;
    public Transform monitorQuad;
    
    [Header("BPM Display")]
    public TextMeshProUGUI bpmText;
    
    [Header("Settings")]
    public float handOverlapDistance = 0.15f;
    public float idealDepth = 0.05f;
    public float idealBpm = 100f;
    public int compressionsBeforeBpm = 20;
    
    [Header("Chest Physics")]
    public float chestStiffness = 200f;
    public float chestDamping = 10f;
    public float maxCompression = 0.06f;
    
    [Header("ECG Display")]
    public LineRenderer ecgLine;
    public int maxPoints = 500;
    public float ecgScale = 0.2f;
    
    private readonly Queue<float> _ecgValues = new Queue<float>();
    private bool _previouslyOverlapping;
    private bool _previouslyInCube;
    
    private Vector3 _chestOriginalPos;
    private float _currentChestCompression;
    private float _chestVelocity;
    
    private float _startHandY;
    private float _maxDepthReached;
    private bool _trackingPress;
    
    private float _lastCompressionTime = -999f;
    private float _currentBpm;
    private int _compressionCount;
    
    private bool _spikeQueued;
    private float _spikeQueueTime;
    private float _queuedDepth;
    private float _queuedSpeed;

    void Start()
    {
        if (chestPlate != null)
        {
            _chestOriginalPos = chestPlate.position;
        }
        
        if (ecgLine == null)
        {
            GameObject lineObj = new GameObject("ECG_Line");
            lineObj.transform.SetParent(monitorQuad);
            lineObj.transform.localPosition = Vector3.zero;
            lineObj.transform.localRotation = Quaternion.identity;
            
            ecgLine = lineObj.AddComponent<LineRenderer>();
            ecgLine.startWidth = 0.008f;
            ecgLine.endWidth = 0.008f;
            ecgLine.material = new Material(Shader.Find("Sprites/Default"));
            ecgLine.startColor = Color.green;
            ecgLine.endColor = Color.green;
            ecgLine.useWorldSpace = false;
        }
        
        if (bpmText == null && monitorQuad != null)
        {
            Canvas canvas = monitorQuad.GetComponentInChildren<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("BPM_Canvas");
                canvasObj.transform.SetParent(monitorQuad);
                canvasObj.transform.localPosition = new Vector3(0, 0.15f, -0.01f);
                canvasObj.transform.localRotation = Quaternion.identity;
                canvasObj.transform.localScale = Vector3.one * 0.001f;
                
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                
                GameObject textObj = new GameObject("BPM_Text");
                textObj.transform.SetParent(canvasObj.transform);
                textObj.transform.localPosition = Vector3.zero;
                textObj.transform.localRotation = Quaternion.identity;
                textObj.transform.localScale = Vector3.one;
                
                bpmText = textObj.AddComponent<TextMeshProUGUI>();
                bpmText.fontSize = 100;
                bpmText.color = Color.green;
                bpmText.alignment = TextAlignmentOptions.Center;
                bpmText.text = "-- BPM";
                
                RectTransform rect = textObj.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(400, 150);
            }
        }
        
        for (int i = 0; i < maxPoints; i++)
        {
            _ecgValues.Enqueue(0f);
        }
    }

    void Update()
    {
        bool handsOverlap = CheckHandsOverlap();
        
        if (handsOverlap && !_previouslyOverlapping)
        {
            Debug.Log("CPR POSE READY");
        }
        _previouslyOverlapping = handsOverlap;
        
        if (handsOverlap && chestPlate != null)
        {
            Vector3 handsCenter = (leftHand.position + rightHand.position) / 2f;
            Bounds chestBounds = new Bounds(chestPlate.position, chestPlate.localScale);
            bool handsInCube = chestBounds.Contains(handsCenter);
            
            if (handsInCube)
            {
                if (!_trackingPress)
                {
                    _startHandY = handsCenter.y;
                    _trackingPress = true;
                    _maxDepthReached = 0f;
                }
                
                float currentDepth = _startHandY - handsCenter.y;
                if (currentDepth > _maxDepthReached)
                {
                    _maxDepthReached = currentDepth;
                }
                
                if (currentDepth > 0)
                {
                    float targetCompression = Mathf.Clamp(currentDepth, 0f, maxCompression);
                    float compressionDiff = targetCompression - _currentChestCompression;
                    _chestVelocity += compressionDiff * chestStiffness * Time.deltaTime;
                }
                
                _previouslyInCube = true;
            }
            else
            {
                if (_previouslyInCube && _trackingPress && _maxDepthReached > 0.01f)
                {
                    _compressionCount++;
                    
                    float timeSinceLast = Time.time - _lastCompressionTime;
                    if (timeSinceLast > 0.1f && _compressionCount >= compressionsBeforeBpm)
                    {
                        _currentBpm = 60f / timeSinceLast;
                    }
                    
                    float compressionSpeed = _currentBpm / idealBpm;
                    
                    _spikeQueued = true;
                    _spikeQueueTime = Time.time + 0.15f;
                    _queuedDepth = _maxDepthReached;
                    _queuedSpeed = compressionSpeed;
                    
                    _lastCompressionTime = Time.time;
                    
                    if (_compressionCount >= compressionsBeforeBpm)
                    {
                        UpdateBpmDisplay();
                    }
                    else
                    {
                        if (bpmText != null)
                        {
                            bpmText.text = $"{_compressionCount}/{compressionsBeforeBpm}";
                            bpmText.color = Color.white;
                        }
                    }
                    
                    string depthQuality = _maxDepthReached < 0.04f ? "SHALLOW" : 
                                         _maxDepthReached > 0.06f ? "TOO DEEP" : "GOOD";
                    string speedQuality = _currentBpm < 80f ? "TOO SLOW" : 
                                         _currentBpm > 120f ? "TOO FAST" : "GOOD";
                    
                    Debug.Log($"Compression #{_compressionCount} | Depth: {(_maxDepthReached*100):F1}cm ({depthQuality}) | BPM: {_currentBpm:F0} ({speedQuality})");
                }
                
                _trackingPress = false;
                _maxDepthReached = 0f;
                _previouslyInCube = false;
            }
        }
        else
        {
            _previouslyInCube = false;
            _trackingPress = false;
        }
        
        if (_spikeQueued && Time.time >= _spikeQueueTime)
        {
            CreateHeartbeatSpike(_queuedDepth, _queuedSpeed);
            _spikeQueued = false;
        }
        
        UpdateChestPhysics();
        UpdateEcgLine();
    }

    void UpdateBpmDisplay()
    {
        if (bpmText == null) return;
        
        bpmText.text = $"{_currentBpm:F0} BPM";
        
        if (_currentBpm >= 90f && _currentBpm <= 110f)
        {
            bpmText.color = Color.green;
        }
        else if (_currentBpm < 80f || _currentBpm > 120f)
        {
            bpmText.color = Color.red;
        }
        else
        {
            bpmText.color = Color.yellow;
        }
    }

    bool CheckHandsOverlap()
    {
        if (leftHand == null || rightHand == null) return false;
        float distance = Vector3.Distance(leftHand.position, rightHand.position);
        return distance < handOverlapDistance;
    }

    void UpdateChestPhysics()
    {
        if (chestPlate == null) return;
        
        float springForce = -_currentChestCompression * chestStiffness;
        _chestVelocity += springForce * Time.deltaTime;
        _chestVelocity *= (1f - chestDamping * Time.deltaTime);
        _currentChestCompression += _chestVelocity * Time.deltaTime;
        _currentChestCompression = Mathf.Clamp(_currentChestCompression, 0f, maxCompression);
        
        chestPlate.position = _chestOriginalPos - new Vector3(0, _currentChestCompression, 0);
    }

    void CreateHeartbeatSpike(float depth, float speed)
    {
        float depthIntensity = depth / idealDepth;
        float speedMultiplier = Mathf.Clamp(speed, 0.5f, 1.5f);
        
        float pWaveHeight = Mathf.Lerp(0.03f, 0.06f, depthIntensity) * speedMultiplier;
        float rWaveHeight = Mathf.Lerp(0.5f, 1.5f, depthIntensity) * speedMultiplier;
        float tWaveHeight = Mathf.Lerp(0.15f, 0.25f, depthIntensity) * speedMultiplier;
        
        int waveformSpeed = Mathf.RoundToInt(Mathf.Lerp(1f, 3f, speed));
        
        for (int i = 0; i < 3 * waveformSpeed; i++) AddToEcg(pWaveHeight * 0.7f);
        for (int i = 0; i < 3 * waveformSpeed; i++) AddToEcg(pWaveHeight);
        for (int i = 0; i < 3 * waveformSpeed; i++) AddToEcg(pWaveHeight * 0.7f);
        
        for (int i = 0; i < 5 * waveformSpeed; i++) AddToEcg(0f);
        
        for (int i = 0; i < 2; i++) AddToEcg(-0.08f * depthIntensity);
        
        AddToEcg(0.1f * rWaveHeight);
        AddToEcg(0.3f * rWaveHeight);
        AddToEcg(0.6f * rWaveHeight);
        AddToEcg(0.9f * rWaveHeight);
        AddToEcg(1.2f * rWaveHeight);
        AddToEcg(1.0f * rWaveHeight);
        AddToEcg(0.7f * rWaveHeight);
        AddToEcg(0.4f * rWaveHeight);
        AddToEcg(0.1f * rWaveHeight);
        
        for (int i = 0; i < 3; i++) AddToEcg(-0.12f * depthIntensity);
        for (int i = 0; i < 2; i++) AddToEcg(-0.08f * depthIntensity);
        
        for (int i = 0; i < 8 * waveformSpeed; i++) AddToEcg(0f);
        
        AddToEcg(0.06f * tWaveHeight);
        AddToEcg(0.12f * tWaveHeight);
        AddToEcg(0.18f * tWaveHeight);
        AddToEcg(0.22f * tWaveHeight);
        AddToEcg(0.24f * tWaveHeight);
        AddToEcg(0.22f * tWaveHeight);
        AddToEcg(0.18f * tWaveHeight);
        AddToEcg(0.12f * tWaveHeight);
        AddToEcg(0.06f * tWaveHeight);
        
        for (int i = 0; i < 10 * waveformSpeed; i++) AddToEcg(0f);
    }

    void AddToEcg(float value)
    {
        if (_ecgValues.Count >= maxPoints)
        {
            _ecgValues.Dequeue();
        }
        _ecgValues.Enqueue(value);
    }

    void UpdateEcgLine()
    {
        AddToEcg(0f);
        
        ecgLine.positionCount = _ecgValues.Count;
        int index = 0;
        foreach (float value in _ecgValues)
        {
            float x = ((float)index / maxPoints) * 0.8f - 0.4f;
            float y = value * ecgScale;
            ecgLine.SetPosition(index, new Vector3(x, y, -0.01f));
            index++;
        }
    }

    void OnDrawGizmos()
    {
        if (chestPlate != null)
        {
            Gizmos.color = _previouslyInCube ? Color.red : Color.yellow;
            Gizmos.DrawWireCube(chestPlate.position, chestPlate.localScale);
        }
        
        if (leftHand != null && rightHand != null)
        {
            Vector3 center = (leftHand.position + rightHand.position) / 2f;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(center, 0.03f);
        }
    }
}