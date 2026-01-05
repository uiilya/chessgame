using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject gameOverPanel;
    public GameObject menuPanel;
    public TextMeshProUGUI winnerText;
    public TextMeshProUGUI turnIndicator;
    public TextMeshProUGUI phaseIndicator;

    public void SetTurnText(string text)
    {
        if (turnIndicator) turnIndicator.text = text;
    }

    public void SetPhaseText(string text)
    {
        if (phaseIndicator) phaseIndicator.text = text;
    }

    public void ShowVictory(string winner)
    {
        if (gameOverPanel) gameOverPanel.SetActive(true);
        // if (winnerText)
        // {
        //     if (winner == "white") winnerText.text = "VICTORY!";
        //     else winnerText.text = "DEFEAT";
        // }
        winnerText.text = winner;
    }

    public void OpenMenu()
    {
        if (menuPanel != null)
        {
            bool isActive = menuPanel.activeSelf;
            menuPanel.SetActive(!isActive);
        }
    }
}