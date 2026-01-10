using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
public sealed class CprUIDisplay : MonoBehaviour
{
    [Header("UI References (assign these)")]
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private TextMeshProUGUI qualityText;
    [SerializeField] private TextMeshProUGUI actualBpmText;

    [HideInInspector] public float IdealBpm = 100f;

    private void Awake()
    {
        // Fail fast (AAA): don't auto-create UI at runtime.
        if (statsText == null || qualityText == null || actualBpmText == null)
        {
            Debug.LogError($"{nameof(CprUIDisplay)}: Missing TMP references. Assign them in the inspector.", this);
            enabled = false;
        }
    }

    public void UpdateProgressDisplay(int current, int target)
    {
        if (!enabled) return;
        statsText.text = $"Compressions: {current}/{target}";
        statsText.color = Color.white;

        qualityText.text = "Quality: --";
        qualityText.color = Color.white;
    }

    public void UpdateStatsDisplay(float avgBpm, float avgDepth)
    {
        if (!enabled) return;

        if (avgBpm > 0f && avgDepth > 0f)
            statsText.text = $"Avg: {avgBpm:F0} BPM | {(avgDepth * 100f):F1} cm";
        else
            statsText.text = "Avg: -- BPM | -- cm";
    }

    public void UpdateActualBpmDisplay(float actualBpm, float sessionMinutes)
    {
        if (!enabled) return;

        if (actualBpm > 0f)
        {
            actualBpmText.text = $"Rate: {actualBpm:F1} BPM ({sessionMinutes:F1} min)";

            if (actualBpm >= 90f && actualBpm <= 110f)
                actualBpmText.color = new Color(0.2f, 1f, 0.2f);
            else if (actualBpm < 80f || actualBpm > 120f)
                actualBpmText.color = new Color(1f, 0.3f, 0.3f);
            else
                actualBpmText.color = new Color(1f, 1f, 0.3f);
        }
        else
        {
            actualBpmText.text = "Rate: -- BPM";
            actualBpmText.color = new Color(0.5f, 0.8f, 1f);
        }
    }

    public void UpdateQualityDisplay(CprQualityCalculator.QualityResult quality)
    {
        if (!enabled) return;
        qualityText.text = $"{quality.GradeText} ({quality.Score:F0}%)";
        qualityText.color = quality.Color;
    }
}
