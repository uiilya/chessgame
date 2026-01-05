using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class ProgressionManager : MonoBehaviour
{
    public static ProgressionManager Instance;

    [Header("Run State")]
    public int currentLevel = 1;
    public int currency = 0; // "Points"
    public List<DeckEntry> playerDeck = new List<DeckEntry>();
    
    // Config
    public int maxLevels = 3;
    public int initialCurrency = 5;

    // Central Database for Card Data (Cost, Description)
    public Dictionary<string, (int cost, string desc)> cardLibrary = new Dictionary<string, (int, string)>()
    {
        { "king",   (5, "Practically your life.") }, // Cost added
        { "queen",  (5, "Powerful.") },
        { "rook",   (4, "Long range.") },
        { "bishop", (3, "Diagonal.") },
        { "knight", (3, "Jumper.") },
        { "pawn",   (1, "Basic infantry.") }
    };

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- RUN CONTROL ---

    public void StartNewRun()
    {
        currentLevel = 1;
        currency = initialCurrency;
        playerDeck.Clear();

        // Basic Starter Deck (Now includes a King card)
        AddCardToDeck("king", 1); 
        AddCardToDeck("pawn", 4);
        AddCardToDeck("knight", 1);

        SceneManager.LoadScene("DeckBuilderScene");
    }

    public void CompleteLevel(int pointsEarned)
    {
        currency += pointsEarned;
        currentLevel++;

        if (currentLevel > maxLevels)
        {
            Debug.Log("YOU BEAT THE GAME!");
            SceneManager.LoadScene("Main Menu"); 
        }
        else
        {
            SceneManager.LoadScene("DeckBuilderScene");
        }
    }

    // --- DECK MANAGEMENT ---

    public void AddCardToDeck(string type, int count)
    {
        int index = playerDeck.FindIndex(x => x.pieceType == type);
        if (index != -1)
        {
            DeckEntry entry = playerDeck[index];
            entry.count += count;
            playerDeck[index] = entry;
        }
        else
        {
            playerDeck.Add(new DeckEntry { pieceType = type, count = count });
        }
    }

    public bool TryBuyCard(string type)
    {
        if (cardLibrary.ContainsKey(type))
        {
            int cost = cardLibrary[type].cost;
            if (currency >= cost)
            {
                currency -= cost;
                AddCardToDeck(type, 1);
                return true;
            }
        }
        return false;
    }

    public bool TrySellCard(string type)
    {
        // Removed the "Cannot sell King" check here. 
        // We allow selling the King as long as they buy one back before starting (checked in DeckBuilderController).
        
        int index = playerDeck.FindIndex(x => x.pieceType == type);
        if (index != -1 && cardLibrary.ContainsKey(type))
        {
            DeckEntry entry = playerDeck[index];
            
            // Refund cost
            currency += cardLibrary[type].cost;

            entry.count--;
            if (entry.count <= 0)
            {
                playerDeck.RemoveAt(index);
            }
            else
            {
                playerDeck[index] = entry;
            }
            return true;
        }
        return false;
    }

    // --- AI DIFFICULTY ---

    public List<DeckEntry> GetEnemyDeckForCurrentLevel()
    {
        List<DeckEntry> aiDeck = new List<DeckEntry>();
        
        // Note: AI Kings might still be pre-placed by GameManager, 
        // but if we want AI to deploy extra Kings, we add them here.
        // We do NOT add the main King here if GameManager spawns it.
        
        switch (currentLevel)
        {
            case 1: // Easy
                aiDeck.Add(new DeckEntry { pieceType = "king", count = 1 });
                aiDeck.Add(new DeckEntry { pieceType = "pawn", count = 5 });
                aiDeck.Add(new DeckEntry { pieceType = "knight", count = 1 });
                break;

            case 2: // Medium
                aiDeck.Add(new DeckEntry { pieceType = "pawn", count = 4 });
                aiDeck.Add(new DeckEntry { pieceType = "bishop", count = 2 });
                aiDeck.Add(new DeckEntry { pieceType = "rook", count = 1 });
                aiDeck.Add(new DeckEntry { pieceType = "queen", count = 1 });
                aiDeck.Add(new DeckEntry { pieceType = "king", count = 1 }); 
                break;

            case 3: // Hard
                aiDeck.Add(new DeckEntry { pieceType = "pawn", count = 4 });
                aiDeck.Add(new DeckEntry { pieceType = "bishop", count = 2 });
                aiDeck.Add(new DeckEntry { pieceType = "rook", count = 2 });
                aiDeck.Add(new DeckEntry { pieceType = "queen", count = 2 });
                aiDeck.Add(new DeckEntry { pieceType = "knight", count = 2 });
                aiDeck.Add(new DeckEntry { pieceType = "king", count = 2 });
                break;
                
            default: 
                aiDeck.Add(new DeckEntry { pieceType = "pawn", count = 5 });
                aiDeck.Add(new DeckEntry { pieceType = "queen", count = 2 });
                aiDeck.Add(new DeckEntry { pieceType = "rook", count = 2 });
                break;
        }

        return aiDeck;
    }
}