using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public void PlayGame()
    {
        // Instead of loading scene directly, we initialize the roguelike run
        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.StartNewRun();
        }
        else
        {
            Debug.LogError("ProgressionManager is missing! Make sure it's in the scene or bootstrapped.");
            // Fallback for testing
            SceneManager.LoadScene("ChessScene");
        }
    }

    public void QuitGame()
    {
        Debug.Log("Quit!");
        Application.Quit();
    }
}