using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class ResultsFlow : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CprSystem cprSystem;

    [Header("Scene Names")]
    [SerializeField] private string resultsSceneName = "ResultsRoom";
    [SerializeField] private string gameplaySceneName = "Gameplay";

    [Header("Debug")]
    [SerializeField] private bool log = true;

    private void Awake()
    {
        // Ensure store exists
        if (RunResultsStore.Instance == null)
        {
            var storeGo = new GameObject("RunResultsStore");
            storeGo.AddComponent<RunResultsStore>();
        }

        if (cprSystem == null)
            cprSystem = FindFirstObjectByType<CprSystem>();
    }

    // Hook this to your timeline end (after AED arrived + 20 sec)
    public void GoToResults()
    {
        var snap = new CprResultsSnapshot();

        if (cprSystem != null)
        {
            var q = cprSystem.GetQualityResult();
            snap.compressions = cprSystem.CompressionCount;
            snap.avgBpm = cprSystem.AvgBpm;
            snap.avgDepthMeters = cprSystem.AvgDepthMeters;
            snap.score = q.Score;
            snap.gradeText = q.GradeText;
        }

        RunResultsStore.Instance.Save(snap);

        if (log)
            Debug.Log($"[ResultsFlow] Saved: {snap.compressions} comps, {snap.avgBpm:F0} BPM, {snap.AvgDepthCm:F1} cm, {snap.gradeText} {snap.score:F0}%");

        SceneManager.LoadScene(resultsSceneName);
    }

    public void RestartGameplay()
    {
        if (RunResultsStore.Instance != null)
            RunResultsStore.Instance.Clear();

        SceneManager.LoadScene(gameplaySceneName);
    }
}