using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class ResultsRoomUI : MonoBehaviour
{
    [Header("Text (TMP)")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI detailsText;

    [Header("Scene Names")]
    [SerializeField] private string gameplaySceneName = "Gameplay";

    private void Start()
    {
        // Ensure store exists (in case someone enters ResultsRoom directly)
        if (RunResultsStore.Instance == null)
        {
            var storeGo = new GameObject("RunResultsStore");
            storeGo.AddComponent<RunResultsStore>();
        }

        if (!RunResultsStore.Instance.HasResults)
        {
            if (titleText != null) titleText.text = "No Results";
            if (detailsText != null) detailsText.text = "No CPR data was recorded.";
            return;
        }

        var r = RunResultsStore.Instance.Results;

        if (titleText != null)
            titleText.text = $"CPR PERFORMANCE: {r.gradeText} ({r.score:F0}%)";

        if (detailsText != null)
        {
            detailsText.text =
                $"Compressions: {r.compressions}\n" +
                $"Avg Rate: {r.avgBpm:F0} BPM\n" +
                $"Avg Depth: {r.AvgDepthCm:F1} cm\n";
        }
    }

    // Button: Restart
    public void Restart()
    {
        RunResultsStore.Instance.Clear();
        SceneManager.LoadScene(gameplaySceneName);
    }

    // Button: Quit
    public void Quit()
    {
        Application.Quit();
    }
}