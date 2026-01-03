using UnityEngine;
using UnityEngine.SceneManagement; // IMPORTANT: This namespace is required to switch scenes

public class MainMenuController : MonoBehaviour
{
    // Call this function to load the chess scene
    public void PlayGame()
    {
        // Replace "ChessSceneName" with the EXACT name of your chess scene
        SceneManager.LoadScene("ChessScene");
    }

    // Optional: Call this to close the game
    public void QuitGame()
    {
        Debug.Log("Quit!"); // Prints to console because Application.Quit() doesn't work in the editor
        Application.Quit();
    }
}