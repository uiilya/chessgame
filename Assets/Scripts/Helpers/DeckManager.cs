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

    // TESTING: Static lists to persist deck choices across scene reloads
    public static List<DeckEntry> savedPlayerDeck = null;
    public static List<DeckEntry> savedAIDeck = null;

    // Library
    public Dictionary<string, (int cost, string desc)> cardLibrary = new Dictionary<string, (int, string)>()
    {
        { "queen",  (5, "Powerful.") },
        { "rook",   (4, "Long range.") },
        { "bishop", (3, "Diagonal.") },
        { "knight", (3, "Jumper.") },
        { "pawn",   (1, "Basic infantry.") }
    };

    public void InitializeDecks()
    {
        whiteDeck.Clear(); blackDeck.Clear();
        whiteHand.Clear(); blackHand.Clear();

        if (savedPlayerDeck != null) playerDeckSetup = new List<DeckEntry>(savedPlayerDeck);
        if (savedAIDeck != null) aiDeckSetup = new List<DeckEntry>(savedAIDeck);

        if (playerDeckSetup.Count == 0) LoadDefaultSetup();
        if (aiDeckSetup.Count == 0) LoadDefaultAISetup();

        BuildDeck(whiteDeck, playerDeckSetup, "white");
        BuildDeck(blackDeck, aiDeckSetup, "black");

        Shuffle(whiteDeck);
        Shuffle(blackDeck);
    }

    void BuildDeck(List<Card> deckTarget, List<DeckEntry> setup, string owner)
    {
        foreach (var entry in setup)
        {
            if (cardLibrary.ContainsKey(entry.pieceType))
            {
                var stats = cardLibrary[entry.pieceType];
                AddCardToDeck(deckTarget, owner, entry.pieceType, entry.count, stats.cost, stats.desc);
            }
        }
    }

    void LoadDefaultSetup()
    {
        playerDeckSetup.Add(new DeckEntry { pieceType = "queen", count = 1 });
        playerDeckSetup.Add(new DeckEntry { pieceType = "rook", count = 2 });
        playerDeckSetup.Add(new DeckEntry { pieceType = "bishop", count = 2 });
        playerDeckSetup.Add(new DeckEntry { pieceType = "knight", count = 2 });
    }

    void LoadDefaultAISetup()
    {
        aiDeckSetup.Add(new DeckEntry { pieceType = "queen", count = 1 });
        aiDeckSetup.Add(new DeckEntry { pieceType = "rook", count = 2 });
        aiDeckSetup.Add(new DeckEntry { pieceType = "bishop", count = 2 });
        aiDeckSetup.Add(new DeckEntry { pieceType = "knight", count = 2 });
        aiDeckSetup.Add(new DeckEntry { pieceType = "pawn", count = 4 });
    }

    void AddCardToDeck(List<Card> deck, string owner, string type, int count, int cost, string desc)
    {
        for(int i=0; i<count; i++)
        {
            string name = char.ToUpper(type[0]) + type.Substring(1);
            deck.Add(new Card(name, type, owner, cost, desc));
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
        
        if (player == "black") RefreshAIHandVisuals();
    }

    public void RemoveCardFromHand(string player, int index)
    {
        if (player == "white") whiteHand.RemoveAt(index);
        else 
        {
            if (blackHand.Count > 0) blackHand.RemoveAt(0); 
            RefreshAIHandVisuals();
        }
    }

    void RefreshAIHandVisuals()
    {
        foreach (GameObject g in aiHandVisuals) Destroy(g);
        aiHandVisuals.Clear();

        if (cardBackPrefab == null) return;

        float cardSpacing = 1.0f; 
        float startY = 9f;      
        
        float totalWidth = (blackHand.Count - 1) * cardSpacing;
        float startX = 3.5f - (totalWidth / 2);

        for (int i = 0; i < blackHand.Count; i++)
        {
            float x = startX + (i * cardSpacing);
            float z = -2.0f - (i * 0.1f);

            GameObject card = Instantiate(cardBackPrefab, new Vector3(x, startY, z), Quaternion.identity);
            card.name = "AI_Card_" + i;
            
            // FIX: Remove CardObject script if present, otherwise it teleports to (0,0,0)
            CardObject co = card.GetComponent<CardObject>();
            if (co != null) Destroy(co);

            SpriteRenderer sr = card.GetComponent<SpriteRenderer>();
            if (sr == null) sr = card.GetComponentInChildren<SpriteRenderer>(); 
            
            if (sr != null)
            {
                sr.sortingOrder = 10 + i;
                SpriteRenderer[] children = card.GetComponentsInChildren<SpriteRenderer>();
                foreach(SpriteRenderer child in children)
                {
                    if (child.name.Contains("Border")) child.sortingOrder = 10 + i - 1; 
                    else child.sortingOrder = 10 + i; 
                }
            }

            card.transform.localScale = Vector3.one * 0.8f;
            aiHandVisuals.Add(card);
        }
    }

    // FIX: Calculate position mathematically to prevent (0,0) errors if visuals are missing
    public Vector3 GetTopAICardPosition()
    {
        float cardSpacing = 1.0f;
        float startY = 9.0f;
        
        // If hand is empty, return generic Top Center
        if (blackHand.Count == 0) return new Vector3(3.5f, startY, -2f);

        // Calculate where the FIRST card (index 0) *should* be
        float totalWidth = (blackHand.Count - 1) * cardSpacing;
        float startX = 3.5f - (totalWidth / 2);

        return new Vector3(startX, startY, -2.0f);
    }
}