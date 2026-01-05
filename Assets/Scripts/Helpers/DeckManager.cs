using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct DeckEntry
{
    public string pieceType; 
    public int count;        
}

public class DeckManager : MonoBehaviour
{
    public List<Card> whiteDeck = new List<Card>();
    public List<Card> blackDeck = new List<Card>();
    public List<Card> whiteHand = new List<Card>();
    public List<Card> blackHand = new List<Card>();

    [Header("Deck Configuration")]
    public List<DeckEntry> playerDeckSetup = new List<DeckEntry>();
    public List<DeckEntry> aiDeckSetup = new List<DeckEntry>();

    [Header("AI Visuals")]
    public GameObject cardBackPrefab; 
    private List<GameObject> aiHandVisuals = new List<GameObject>();

    [Header("Card Art Assets")]
    public Sprite art_King; // NEW: Added King Art slot
    public Sprite art_Queen;
    public Sprite art_Rook;
    public Sprite art_Bishop;
    public Sprite art_Knight;
    public Sprite art_Pawn;

    public void InitializeDecks()
    {
        whiteDeck.Clear(); blackDeck.Clear();
        whiteHand.Clear(); blackHand.Clear();

        if (ProgressionManager.Instance != null)
        {
            playerDeckSetup = new List<DeckEntry>(ProgressionManager.Instance.playerDeck);
            aiDeckSetup = ProgressionManager.Instance.GetEnemyDeckForCurrentLevel();
        }
        else
        {
            // Debugging Fallback
            if (playerDeckSetup.Count == 0) LoadDefaultSetup();
            if (aiDeckSetup.Count == 0) LoadDefaultAISetup();
        }

        BuildDeck(whiteDeck, playerDeckSetup, "white");
        BuildDeck(blackDeck, aiDeckSetup, "black");

        Shuffle(whiteDeck);
        Shuffle(blackDeck);
    }

    void BuildDeck(List<Card> deckTarget, List<DeckEntry> setup, string owner)
    {
        foreach (var entry in setup)
        {
            string typeKey = entry.pieceType.ToLower();
            
            int cost = 1;
            string desc = "Unit";

            if (ProgressionManager.Instance != null && ProgressionManager.Instance.cardLibrary.ContainsKey(typeKey))
            {
                var stats = ProgressionManager.Instance.cardLibrary[typeKey];
                cost = stats.cost;
                desc = stats.desc;
            }

            Sprite specificArt = GetCardArt(typeKey);
            AddCardToDeck(deckTarget, owner, typeKey, entry.count, cost, desc, specificArt);
        }
    }

    private Sprite GetCardArt(string type)
    {
        switch (type.ToLower())
        {
            case "king": return art_King; // Added Case
            case "queen": return art_Queen;
            case "rook": return art_Rook;
            case "bishop": return art_Bishop;
            case "knight": return art_Knight;
            case "pawn": return art_Pawn;
            default: return null;
        }
    }

    void LoadDefaultSetup()
    {
        playerDeckSetup.Add(new DeckEntry { pieceType = "king", count = 1 }); // Default needs King now
        playerDeckSetup.Add(new DeckEntry { pieceType = "queen", count = 1 });
        playerDeckSetup.Add(new DeckEntry { pieceType = "rook", count = 2 });
    }

    void LoadDefaultAISetup()
    {
        aiDeckSetup.Add(new DeckEntry { pieceType = "pawn", count = 5 });
    }

    void AddCardToDeck(List<Card> deck, string owner, string type, int count, int cost, string desc, Sprite art)
    {
        // REMOVED logic that prevented King from being added to deck
        for(int i=0; i<count; i++)
        {
            string name = char.ToUpper(type[0]) + type.Substring(1);
            deck.Add(new Card(name, type, owner, cost, desc, art));
        }
    }

    void Shuffle(List<Card> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            Card temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    public void DrawCard(string player, int amount)
    {
        List<Card> deck = (player == "white") ? whiteDeck : blackDeck;
        List<Card> hand = (player == "white") ? whiteHand : blackHand;

        for (int i = 0; i < amount; i++)
        {
            if (deck.Count > 0)
            {
                hand.Add(deck[0]);
                deck.RemoveAt(0);
            }
        }
    }

    // --- NEW: Helper to force a specific card draw (like the King) ---
    public void DrawSpecificCard(string player, string type)
    {
        List<Card> deck = (player == "white") ? whiteDeck : blackDeck;
        List<Card> hand = (player == "white") ? whiteHand : blackHand;

        Card target = deck.Find(x => x.pieceType.ToLower() == type.ToLower());
        
        if (target != null)
        {
            deck.Remove(target);
            hand.Add(target);
        }
    }

    public void RemoveCardFromHand(string player, int index)
    {
        if (player == "white") whiteHand.RemoveAt(index);
        else 
        {
            if (blackHand.Count > 0) blackHand.RemoveAt(0); 
        }
    }
}