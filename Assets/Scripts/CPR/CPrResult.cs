using System;
using UnityEngine;

[Serializable]
public struct CprResultsSnapshot
{
    public int compressions;
    public float avgBpm;
    public float avgDepthMeters;

    public float score;
    public string gradeText;

    public float AvgDepthCm => avgDepthMeters * 100f;
}