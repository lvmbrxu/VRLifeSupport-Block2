using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
public sealed class CprUIDisplay : MonoBehaviour
{
    [Header("Preferred: Assign these (AAA)")]
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private TextMeshProUGUI qualityText;
    [SerializeField] private TextMeshProUGUI actualBpmText;

    [Header("Optional: Auto-create on monitor if missing")]
    [SerializeField] private bool autoCreateOnMonitor = true;
    [SerializeField] private Transform monitorRoot; // your monitorQuad / screen transform
    [SerializeField] private Vector3 canvasLocalOffset = new Vector3(0f, 0f, -0.001f);
    [SerializeField] private float canvasLocalScale = 0.001f;

    [HideInInspector] public float IdealBpm = 100f;

    private void Awake()
    {
        if ((statsText == null || qualityText == null || actualBpmText == null) && autoCreateOnMonitor)
        {
            if (monitorRoot == null) monitorRoot = transform;
            CreateMonitorUI();
        }

        // Still fail if missing (so you know immediately)
        if (statsText == null || qualityText == null || actualBpmText == null)
        {
            Debug.LogError($"{nameof(CprUIDisplay)}: Missing TMP references. Assign them in the inspector (or set monitorRoot + autoCreateOnMonitor).", this);
            enabled = false;
        }
    }

    private void CreateMonitorUI()
    {
        // Create a world-space canvas under the monitor
        var canvasGO = new GameObject("MonitorUI");
        canvasGO.transform.SetParent(monitorRoot, false);
        canvasGO.transform.localPosition = canvasLocalOffset;
        canvasGO.transform.localRotation = Quaternion.identity;
        canvasGO.transform.localScale = Vector3.one * canvasLocalScale;

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        // Create 3 texts
        actualBpmText = CreateText(canvasGO.transform, "RateText", new Vector3(0f, 120f, 0f), 60, new Color(0.5f, 0.8f, 1f));
        statsText     = CreateText(canvasGO.transform, "StatsText", new Vector3(0f, 0f, 0f),   75, new Color(0.8f, 0.8f, 0.8f));
        qualityText   = CreateText(canvasGO.transform, "QualityText", new Vector3(0f, -120f, 0f), 75, Color.white);
    }

    private TextMeshProUGUI CreateText(Transform parent, string name, Vector3 localPos, float fontSize, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.text = "--";

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(900f, 180f);

        return tmp;
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
