using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro; // Using TextMeshPro namespace

public class DeckBuilderController : MonoBehaviour
{
    // CHANGED: We now use TMP_Text instead of TextMeshProUGUI.
    // This allows you to drag in EITHER a "TextMeshPro - Text (UI)" OR a "TextMeshPro - Text (3D)" component.
    
    [Header("Global UI")]
    public TMP_Text pointsText;
    public TMP_Text levelText;

    [Header("Card Count Texts")]
    public TMP_Text kingCountText;
    public TMP_Text queenCountText;
    public TMP_Text rookCountText;
    public TMP_Text bishopCountText;
    public TMP_Text knightCountText;
    public TMP_Text pawnCountText;

    [Header("Card Cost Texts")]
    public TMP_Text kingCostText;
    public TMP_Text queenCostText;
    public TMP_Text rookCostText;
    public TMP_Text bishopCostText;
    public TMP_Text knightCostText;
    public TMP_Text pawnCostText;

    void Start()
    {
        UpdateUI();
        UpdateCosts(); 
    }

    void UpdateUI()
    {
        if (ProgressionManager.Instance == null) return;

        // Update Header
        pointsText.text = "Points Remaining:" + ProgressionManager.Instance.currency;
        levelText.text = "Level " + ProgressionManager.Instance.currentLevel + " / " + ProgressionManager.Instance.maxLevels;

        // Update individual card counts
        UpdateCardCount("king", kingCountText);
        UpdateCardCount("queen", queenCountText);
        UpdateCardCount("rook", rookCountText);
        UpdateCardCount("bishop", bishopCountText);
        UpdateCardCount("knight", knightCountText);
        UpdateCardCount("pawn", pawnCountText);
    }

    void UpdateCosts()
    {
        if (ProgressionManager.Instance == null) return;

        UpdateCardCost("king", kingCostText);
        UpdateCardCost("queen", queenCostText);
        UpdateCardCost("rook", rookCostText);
        UpdateCardCost("bishop", bishopCostText);
        UpdateCardCost("knight", knightCostText);
        UpdateCardCost("pawn", pawnCostText);
    }

    // CHANGED: Parameter type is now TMP_Text to match the fields
    void UpdateCardCount(string type, TMP_Text textComponent)
    {
        if (textComponent == null) return;

        var entry = ProgressionManager.Instance.playerDeck.Find(x => x.pieceType == type);
        int count = (entry.pieceType == type) ? entry.count : 0;
        
        textComponent.text = "x" + count;
    }

    // CHANGED: Parameter type is now TMP_Text to match the fields
    void UpdateCardCost(string type, TMP_Text textComponent)
    {
        if (textComponent == null) return;

        if (ProgressionManager.Instance.cardLibrary.ContainsKey(type))
        {
            int cost = ProgressionManager.Instance.cardLibrary[type].cost;
            textComponent.text = "Cost:" + cost;
        }
    }

    // --- BUTTON ACTIONS ---

    public void BuyCard(string type)
    {
        if (ProgressionManager.Instance.TryBuyCard(type))
        {
            UpdateUI();
        }
        else
        {
            Debug.Log("Not enough points!");
        }
    }

    public void SellCard(string type)
    {
        if (ProgressionManager.Instance.TrySellCard(type))
        {
            UpdateUI();
        }
        else
        {
            Debug.Log("Don't have that card!");
        }
    }

    public void StartBattle()
    {
        // Vital Check: Player MUST have a King to play
        bool hasKing = ProgressionManager.Instance.playerDeck.Exists(x => x.pieceType.ToLower() == "king");
        
        if (!hasKing)
        {
            Debug.LogError("You need a King!");
            // Optional: Show a UI popup "You need a King!"
            return;
        }

        SceneManager.LoadScene("ChessScene");
    }

    public void ReturnToMenu() { SceneManager.LoadScene("Main Menu"); }

    public void QuitGame()
    {
        Debug.Log("Quit!");
        Application.Quit();
    }
}