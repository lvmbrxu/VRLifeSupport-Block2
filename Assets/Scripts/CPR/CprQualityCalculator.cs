using UnityEngine;

/// <summary>
/// Calculates CPR quality scores based on BPM and depth accuracy
/// </summary>
public static class CprQualityCalculator
{
    public enum QualityGrade
    {
        Excellent,
        Good,
        Fair,
        Poor,
        Critical
    }

    public struct QualityResult
    {
        public float Score;
        public QualityGrade Grade;
        public Color Color;
        public string GradeText;
    }

    public static QualityResult CalculateQuality(float avgBpm, float avgDepth, float idealBpm, float idealDepth)
    {
        // --- Tunable ranges (meters for depth) ---
        // BPM
        const float bpmPerfectMin = 90f;
        const float bpmPerfectMax = 110f;
        const float bpmOkMin = 80f;
        const float bpmOkMax = 120f;

        // Depth (meters) ~ 4.5–6.0cm perfect, 3.5–6.5cm ok
        const float depthPerfectMin = 0.045f;
        const float depthPerfectMax = 0.060f;
        const float depthOkMin = 0.035f;
        const float depthOkMax = 0.065f;

        float bpmScore = ScoreBand(avgBpm, bpmPerfectMin, bpmPerfectMax, bpmOkMin, bpmOkMax);
        float depthScore = ScoreBand(avgDepth, depthPerfectMin, depthPerfectMax, depthOkMin, depthOkMax);

        // Weight depth slightly more (depth is harder and more important in VR training)
        float qualityScore = (bpmScore * 0.45f) + (depthScore * 0.55f);

        QualityResult result = new QualityResult { Score = qualityScore };

        if (qualityScore >= 90f)
        {
            result.Grade = QualityGrade.Excellent;
            result.GradeText = "EXCELLENT";
            result.Color = new Color(0.2f, 1f, 0.2f);
        }
        else if (qualityScore >= 75f)
        {
            result.Grade = QualityGrade.Good;
            result.GradeText = "GOOD";
            result.Color = Color.green;
        }
        else if (qualityScore >= 60f)
        {
            result.Grade = QualityGrade.Fair;
            result.GradeText = "FAIR";
            result.Color = Color.yellow;
        }
        else if (qualityScore >= 40f)
        {
            result.Grade = QualityGrade.Poor;
            result.GradeText = "POOR";
            result.Color = new Color(1f, 0.5f, 0f);
        }
        else
        {
            result.Grade = QualityGrade.Critical;
            result.GradeText = "CRITICAL";
            result.Color = Color.red;
        }

        return result;
    }

    // Returns 100 inside perfect band,
    // 75 inside OK band,
    // and falls off outside OK band.
    private static float ScoreBand(float value, float perfectMin, float perfectMax, float okMin, float okMax)
    {
        if (value <= 0f) return 0f;

        if (value >= perfectMin && value <= perfectMax)
            return 100f;

        if (value >= okMin && value <= okMax)
            return 75f;

        // Outside ok band: fall off linearly
        float dist;
        if (value < okMin) dist = okMin - value;
        else dist = value - okMax;

        // The farther away, the worse. Tune multiplier to taste:
        // 0.01 outside => -25 points; 0.02 => -50 points.
        float score = 75f - (dist * 2500f);
        return Mathf.Clamp(score, 0f, 75f);
    }
}
