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
        // Calculate BPM score (0-100)
        float bpmDeviation = Mathf.Abs(avgBpm - idealBpm);
        float bpmScore = Mathf.Clamp(100f - (bpmDeviation * 2f), 0f, 100f);
        
        // Calculate depth score (0-100)
        float depthDeviation = Mathf.Abs(avgDepth - idealDepth);
        float depthScore = Mathf.Clamp(100f - (depthDeviation * 1000f), 0f, 100f);
        
        // Combined quality score (weighted average)
        float qualityScore = (bpmScore + depthScore) / 2f;
        
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
}