using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class TutorialUIButtons : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string gameplaySceneName = "Gameplay";

    public void StartGame()
    {
        SceneManager.LoadScene(gameplaySceneName);
    }

    public void Quit()
    {
        Application.Quit();
    }
}