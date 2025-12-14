// ==================== CprUIDisplay.cs ====================
using UnityEngine;
using TMPro;

/// <summary>
/// Manages all UI displays for CPR monitoring
/// </summary>
public class CprUIDisplay : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI statsText;
    public TextMeshProUGUI qualityText;
    public TextMeshProUGUI actualBpmText;
    public Transform monitorQuad;
    
    [Header("Ideal Values")]
    public float idealBpm = 100f;
    
    public void Initialize()
    {
        if (statsText == null && monitorQuad != null)
        {
            CreateUIElements();
        }
    }
    
    private void CreateUIElements()
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
            
            CreateStatsText(canvasObj.transform);
            CreateQualityText(canvasObj.transform);
            CreateActualBpmText(canvasObj.transform);
        }
    }
    
    private void CreateStatsText(Transform parent)
    {
        GameObject statsObj = new GameObject("Stats_Text");
        statsObj.transform.SetParent(parent);
        statsObj.transform.localPosition = new Vector3(0, 0, 0);
        statsObj.transform.localRotation = Quaternion.identity;
        statsObj.transform.localScale = Vector3.one;
        
        statsText = statsObj.AddComponent<TextMeshProUGUI>();
        statsText.fontSize = 80;
        statsText.color = new Color(0.7f, 0.7f, 0.7f);
        statsText.alignment = TextAlignmentOptions.Center;
        statsText.text = "Compressions: 0";
        
        RectTransform statsRect = statsObj.GetComponent<RectTransform>();
        statsRect.sizeDelta = new Vector2(600, 120);
    }
    
    private void CreateQualityText(Transform parent)
    {
        GameObject qualityObj = new GameObject("Quality_Text");
        qualityObj.transform.SetParent(parent);
        qualityObj.transform.localPosition = new Vector3(0, -100, 0);
        qualityObj.transform.localRotation = Quaternion.identity;
        qualityObj.transform.localScale = Vector3.one;
        
        qualityText = qualityObj.AddComponent<TextMeshProUGUI>();
        qualityText.fontSize = 70;
        qualityText.color = Color.white;
        qualityText.alignment = TextAlignmentOptions.Center;
        qualityText.text = "Quality: --";
        
        RectTransform qualityRect = qualityObj.GetComponent<RectTransform>();
        qualityRect.sizeDelta = new Vector2(600, 100);
    }
    
    private void CreateActualBpmText(Transform parent)
    {
        GameObject actualBpmObj = new GameObject("ActualBPM_Text");
        actualBpmObj.transform.SetParent(parent);
        actualBpmObj.transform.localPosition = new Vector3(0, -180, 0);
        actualBpmObj.transform.localRotation = Quaternion.identity;
        actualBpmObj.transform.localScale = Vector3.one;
        
        actualBpmText = actualBpmObj.AddComponent<TextMeshProUGUI>();
        actualBpmText.fontSize = 65;
        actualBpmText.color = new Color(0.5f, 0.8f, 1f);
        actualBpmText.alignment = TextAlignmentOptions.Center;
        actualBpmText.text = "Rate: -- BPM";
        
        RectTransform actualRect = actualBpmObj.GetComponent<RectTransform>();
        actualRect.sizeDelta = new Vector2(600, 90);
    }
    
    public void UpdateProgressDisplay(int current, int target)
    {
        if (statsText == null) return;
        statsText.text = $"Compressions: {current}/{target}";
        statsText.color = Color.white;
    }
    
    public void UpdateStatsDisplay(float avgBpm, float avgDepth)
    {
        if (statsText == null) return;
        
        if (avgBpm > 0 && avgDepth > 0)
        {
            statsText.text = $"Avg: {avgBpm:F0} BPM | {(avgDepth * 100):F1} cm";
        }
        else
        {
            statsText.text = "Avg: -- BPM | -- cm";
        }
    }
    
    public void UpdateActualBpmDisplay(float actualBpm, float sessionMinutes)
    {
        if (actualBpmText == null) return;
        
        if (actualBpm > 0)
        {
            actualBpmText.text = $"Rate: {actualBpm:F1} BPM ({sessionMinutes:F1} min)";
            
            // Color based on actual performance
            if (actualBpm >= 90f && actualBpm <= 110f)
            {
                actualBpmText.color = new Color(0.2f, 1f, 0.2f);
            }
            else if (actualBpm < 80f || actualBpm > 120f)
            {
                actualBpmText.color = new Color(1f, 0.3f, 0.3f);
            }
            else
            {
                actualBpmText.color = new Color(1f, 1f, 0.3f);
            }
        }
        else
        {
            actualBpmText.text = "Rate: -- BPM";
            actualBpmText.color = new Color(0.5f, 0.8f, 1f);
        }
    }
    
    public void UpdateQualityDisplay(CprQualityCalculator.QualityResult quality)
    {
        if (qualityText == null) return;
        qualityText.text = $"{quality.GradeText} ({quality.Score:F0}%)";
        qualityText.color = quality.Color;
    }
}