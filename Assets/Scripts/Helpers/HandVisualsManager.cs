using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HandVisualsManager : MonoBehaviour
{
    [Header("References")]
    public AudioManager audioManager;
    public DeckManager deckManager; // Assigned by GameManager

    [Header("Deck Visuals")]
    public Transform playerDeckOrigin;
    public Transform aiDeckOrigin;
    public int maxVisualDeckSize = 10;
    public Vector3 deckStackOffset = new Vector3(0f, 0.02f, -0.02f); // Y lifts up, Z comes forward

    private List<GameObject> playerDeckVisuals = new List<GameObject>();
    private List<GameObject> aiDeckVisuals = new List<GameObject>();

    [Header("Prefabs")]
    public GameObject cardPrefab;
    public GameObject cardBackPrefab;

    [Header("Settings")]
    public float cardSpacing = 1.2f;
    public float drawInterval = 0.2f;

    // Track instantiated card objects
    private List<GameObject> activeHandCards = new List<GameObject>();
    private List<GameObject> activeAIHandCards = new List<GameObject>();

    [Header("White Piece Icons")]
    public Sprite icon_king_white;
    public Sprite icon_queen_white;
    public Sprite icon_rook_white;
    public Sprite icon_bishop_white;
    public Sprite icon_knight_white;
    public Sprite icon_pawn_white;

    [Header("Black Piece Icons")]
    public Sprite icon_king_black;
    public Sprite icon_queen_black;
    public Sprite icon_rook_black;
    public Sprite icon_bishop_black;
    public Sprite icon_knight_black;
    public Sprite icon_pawn_black;

    // --- DECK INITIALIZATION ---
    public void InitializeDeckVisuals(int playerCount, int aiCount)
    {
        UpdateDeckVisuals("white", playerCount);
        UpdateDeckVisuals("black", aiCount);
    }

    // Helper to rebuild or adjust the static stack
    void UpdateDeckVisuals(string owner, int realCount)
    {
        List<GameObject> stack = (owner == "white") ? playerDeckVisuals : aiDeckVisuals;
        Transform origin = (owner == "white") ? playerDeckOrigin : aiDeckOrigin;

        if (origin == null) return;

        // Cleanup nulls first
        for (int i = stack.Count - 1; i >= 0; i--)
        {
            if (stack[i] == null) stack.RemoveAt(i);
        }

        int targetCount = Mathf.Min(realCount, maxVisualDeckSize);

        // Add missing visuals
        while (stack.Count < targetCount)
        {
            GameObject card = Instantiate(cardBackPrefab, origin);
            
            // FIX: Ensure Deck Visuals are NOT tagged 'Bank' so they don't get destroyed by RefreshHand
            card.tag = "Untagged"; 
            
            // Disable scripts
            if(card.GetComponent<CardObject>()) card.GetComponent<CardObject>().enabled = false;
            
            stack.Add(card);
        }

        // Remove excess visuals
        while (stack.Count > targetCount)
        {
            int topIndex = stack.Count - 1;
            if(stack[topIndex] != null) Destroy(stack[topIndex]);
            stack.RemoveAt(topIndex);
        }

        // Reposition everything perfectly
        for (int i = 0; i < stack.Count; i++)
        {
            if (stack[i] == null) continue;
            stack[i].transform.position = origin.position + (deckStackOffset * i);
            stack[i].transform.rotation = origin.rotation;
            stack[i].name = $"{owner}_Deck_{i}";
        }
    }

    // --- HAND REFRESH LOGIC ---
    public void RefreshPlayerHand(List<Card> hand)
    {
        // 1. Clear managed list
        foreach (GameObject g in activeHandCards) 
        {
            if(g != null) Destroy(g);
        }
        activeHandCards.Clear();
        
        // 2. Clear legacy tags (BUT ONLY "Bank" ones, assuming Deck visuals are Untagged now)
        GameObject[] existing = GameObject.FindGameObjectsWithTag("Bank");
        foreach (GameObject g in existing) 
        {
            // Extra safety: Check if this is accidentally one of our deck visuals
            if (!playerDeckVisuals.Contains(g) && !aiDeckVisuals.Contains(g))
            {
                Destroy(g);
            }
        }

        if (hand.Count == 0) return;

        List<Vector3> positions = CalculateHandPositions(hand.Count);
        for (int i = 0; i < hand.Count; i++)
        {
            SpawnCardObject(hand[i], positions[i], i, 10 + (i * 10));
        }
    }

    public void RefreshAIHand(int cardCount)
    {
        ClearList(activeAIHandCards);
        if (cardCount == 0) return;

        List<Vector3> positions = CalculateAIHandPositions(cardCount);
        for (int i = 0; i < cardCount; i++)
        {
            GameObject card = Instantiate(cardBackPrefab, positions[i], Quaternion.identity);
            card.name = "AI_Card_" + i;
            activeAIHandCards.Add(card);
        }
    }

    void ClearList(List<GameObject> list)
    {
        foreach (GameObject g in list) if(g != null) Destroy(g);
        list.Clear();
    }

    // --- ANIMATIONS ---

    public IEnumerator AnimateDraw(List<Card> handData, int amountToDraw)
    {
        int totalCards = handData.Count;
        int startIndex = totalCards - amountToDraw;

        List<Vector3> finalPositions = CalculateHandPositions(totalCards);

        // Slide existing cards
        for (int i = 0; i < activeHandCards.Count; i++)
        {
            if (activeHandCards[i] != null)
            {
                StartCoroutine(MoveCardToPosition(activeHandCards[i], finalPositions[i], 0.25f));
                CardObject co = activeHandCards[i].GetComponent<CardObject>();
                if (co != null) co.handIndex = i; 
            }
        }

        // Draw new cards
        for (int k = 0; k < amountToDraw; k++)
        {
            int handIndex = startIndex + k;
            Card cardData = handData[handIndex];
            Vector3 targetPos = finalPositions[handIndex];

            // --- DECK VISUAL LOGIC ---
            int cardsRemainingToDraw = amountToDraw - k; 
            int currentVirtualDeckCount = deckManager.whiteDeck.Count + cardsRemainingToDraw;

            Vector3 spawnPos;
            
            // FIX: Check for nulls in the list before accessing
            if (playerDeckVisuals.Count > 0)
            {
                int topIndex = playerDeckVisuals.Count - 1;
                
                // Safety: If object was destroyed externally, remove from list and fallback
                if (playerDeckVisuals[topIndex] == null)
                {
                    playerDeckVisuals.RemoveAt(topIndex);
                    spawnPos = (playerDeckOrigin != null) ? playerDeckOrigin.position : new Vector3(-8, -2, 0);
                }
                else
                {
                    spawnPos = playerDeckVisuals[topIndex].transform.position;

                    if (currentVirtualDeckCount <= maxVisualDeckSize)
                    {
                        Destroy(playerDeckVisuals[topIndex]);
                        playerDeckVisuals.RemoveAt(topIndex);
                    }
                }
            }
            else
            {
                spawnPos = (playerDeckOrigin != null) ? playerDeckOrigin.position : new Vector3(-8, -2, 0);
            }

            if (audioManager) audioManager.PlayDeckDeal();

            GameObject flyingCard = Instantiate(cardBackPrefab, spawnPos + new Vector3(0, 0, -0.1f), Quaternion.identity);
            
            yield return StartCoroutine(FlyAndFlip(flyingCard, targetPos, cardData, handIndex));

            if (k < amountToDraw - 1) yield return new WaitForSeconds(drawInterval);
        }
    }

    public IEnumerator AnimateAIDraw(int totalCards, int amountToDraw)
    {
        int startIndex = totalCards - amountToDraw;
        List<Vector3> finalPositions = CalculateAIHandPositions(totalCards);

        // Slide existing
        for (int i = 0; i < activeAIHandCards.Count; i++)
        {
            if (activeAIHandCards[i] != null)
            {
                StartCoroutine(MoveCardToPosition(activeAIHandCards[i], finalPositions[i], 0.25f));
            }
        }

        for (int k = 0; k < amountToDraw; k++)
        {
            int handIndex = startIndex + k;
            Vector3 targetPos = finalPositions[handIndex];

            // --- AI DECK VISUAL LOGIC ---
            int cardsRemainingToDraw = amountToDraw - k;
            int currentVirtualDeckCount = deckManager.blackDeck.Count + cardsRemainingToDraw;
            Vector3 spawnPos;

            if (aiDeckVisuals.Count > 0)
            {
                int topIndex = aiDeckVisuals.Count - 1;
                
                // Safety Check
                if (aiDeckVisuals[topIndex] == null)
                {
                    aiDeckVisuals.RemoveAt(topIndex);
                    spawnPos = (aiDeckOrigin != null) ? aiDeckOrigin.position : new Vector3(-8, 9, 0);
                }
                else
                {
                    spawnPos = aiDeckVisuals[topIndex].transform.position;

                    if (currentVirtualDeckCount <= maxVisualDeckSize)
                    {
                        Destroy(aiDeckVisuals[topIndex]);
                        aiDeckVisuals.RemoveAt(topIndex);
                    }
                }
            }
            else
            {
                spawnPos = (aiDeckOrigin != null) ? aiDeckOrigin.position : new Vector3(-8, 9, 0);
            }

            if (audioManager) audioManager.PlayDeckDeal();

            GameObject flyingCard = Instantiate(cardBackPrefab, spawnPos + new Vector3(0, 0, -0.1f), Quaternion.identity);
            flyingCard.name = "AI_Card_" + handIndex;

            yield return StartCoroutine(MoveCardToPosition(flyingCard, targetPos, 0.3f));
            
            activeAIHandCards.Add(flyingCard);

            if (k < amountToDraw - 1) yield return new WaitForSeconds(drawInterval);
        }
    }

    // --- UTILITIES ---

    public void RemoveAIHandCard(int index)
    {
        if (index >= 0 && index < activeAIHandCards.Count)
        {
            GameObject card = activeAIHandCards[index];
            activeAIHandCards.RemoveAt(index);
            if (card != null) Destroy(card);

            if (activeAIHandCards.Count > 0)
            {
                List<Vector3> newPositions = CalculateAIHandPositions(activeAIHandCards.Count);
                for (int i = 0; i < activeAIHandCards.Count; i++)
                {
                    if (activeAIHandCards[i] != null)
                        StartCoroutine(MoveCardToPosition(activeAIHandCards[i], newPositions[i], 0.25f));
                }
            }
        }
    }

    public Vector3 GetAIHandPosition(int index)
    {
        if (index >= 0 && index < activeAIHandCards.Count && activeAIHandCards[index] != null)
            return activeAIHandCards[index].transform.position;
        return new Vector3(0, 9f, 0);
    }

    private List<Vector3> CalculateHandPositions(int count)
    {
        List<Vector3> positions = new List<Vector3>();
        float startY = -2.5f;
        float totalWidth = (count - 1) * cardSpacing;
        float startX = 3.5f - (totalWidth / 2);

        for (int i = 0; i < count; i++)
        {
            float x = startX + (i * cardSpacing);
            float z = -2.0f - (i * 0.1f);
            positions.Add(new Vector3(x, startY, z));
        }
        return positions;
    }

    private List<Vector3> CalculateAIHandPositions(int count)
    {
        List<Vector3> positions = new List<Vector3>();
        float startY = 9.0f;
        float spacing = 1.0f;
        float totalWidth = (count - 1) * spacing;
        float startX = 3.5f - (totalWidth / 2);

        for (int i = 0; i < count; i++)
        {
            float x = startX + (i * spacing);
            float z = -2.0f - (i * 0.1f);
            positions.Add(new Vector3(x, startY, z));
        }
        return positions;
    }

    private IEnumerator FlyAndFlip(GameObject cardObj, Vector3 targetPos, Card data, int index)
    {
        float duration = 0.4f; 
        float elapsed = 0f;
        Vector3 startPos = cardObj.transform.position;
        Quaternion targetRot = Quaternion.Euler(0, 0, Random.Range(-3f, 3f)); 

        while (elapsed < duration)
        {
            if (cardObj == null) yield break;
            float t = elapsed / duration;
            t = t * t * (3f - 2f * t); 
            cardObj.transform.position = Vector3.Lerp(startPos, targetPos, t);
            float ySpin = Mathf.Lerp(0, 360, t); 
            cardObj.transform.rotation = Quaternion.Euler(0, ySpin, 0) * targetRot;
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (cardObj != null) cardObj.transform.position = targetPos;

        // Flip Logic
        float flipDuration = 0.15f;
        elapsed = 0f;
        Quaternion startFlipRot = cardObj.transform.rotation;
        Quaternion edgeRot = Quaternion.Euler(0, 90, 0);

        while (elapsed < flipDuration)
        {
            if (cardObj == null) yield break;
            cardObj.transform.rotation = Quaternion.Lerp(startFlipRot, edgeRot, elapsed / flipDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        GameObject newCard = Instantiate(cardPrefab, cardObj.transform);
        newCard.name = "Card_" + data.cardName;
        newCard.tag = "Bank";
        newCard.transform.localPosition = Vector3.zero;
        newCard.transform.localRotation = Quaternion.identity; 
        newCard.transform.localScale = Vector3.one;            

        CardObject co = newCard.GetComponent<CardObject>();
        co.SetOriginalPos(targetPos);
        co.Setup(data, index, 10 + (index * 10));
        
        if (co != null) co.enabled = false;

        newCard.transform.SetParent(null);
        Destroy(cardObj);
        activeHandCards.Add(newCard);

        elapsed = 0f;
        Quaternion faceUpRot = Quaternion.identity;

        while (elapsed < flipDuration)
        {
            if (newCard == null) yield break;
            newCard.transform.rotation = Quaternion.Lerp(edgeRot, faceUpRot, elapsed / flipDuration);
            newCard.transform.position = targetPos;
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (newCard != null)
        {
            newCard.transform.rotation = faceUpRot;
            newCard.transform.position = targetPos;
            if (co != null) co.enabled = true;
        }
    }

    private IEnumerator MoveCardToPosition(GameObject card, Vector3 targetPos, float duration)
    {
        Vector3 startPos = card.transform.position;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (card == null) yield break;
            float t = elapsed / duration;
            t = Mathf.Sin(t * Mathf.PI * 0.5f);
            card.transform.position = Vector3.Lerp(startPos, targetPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        if(card != null) 
        {
            card.transform.position = targetPos;
            CardObject co = card.GetComponent<CardObject>();
            if (co != null) co.SetOriginalPos(targetPos);
        }
    }

    public IEnumerator AnimateAIDeploy(Vector3 startPos, Vector3 targetPos, string pieceType)
    {
        GameObject visualObj = Instantiate(cardBackPrefab, startPos, Quaternion.identity);
        if(visualObj.GetComponent<CardObject>()) Destroy(visualObj.GetComponent<CardObject>());
        
        yield return new WaitForSeconds(0.1f);

        Sprite pieceSprite = GetIcon(pieceType, "black");

        if (pieceSprite != null)
        {
            foreach (Transform child in visualObj.transform) Destroy(child.gameObject);
            SpriteRenderer sr = visualObj.GetComponent<SpriteRenderer>();
            if (sr == null) sr = visualObj.AddComponent<SpriteRenderer>();

            sr.sprite = pieceSprite;
            visualObj.transform.localScale = Vector3.one;
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 100;
            sr.color = Color.white;
        }

        yield return new WaitForSeconds(0.4f);

        float duration = 0.4f;
        float elapsed = 0f;
        Vector3 morphPos = visualObj.transform.position;

        while (elapsed < duration)
        {
            if (visualObj == null) break;
            float t = elapsed / duration;
            t = t * t * (3f - 2f * t); 
            visualObj.transform.position = Vector3.Lerp(morphPos, new Vector3(targetPos.x, targetPos.y, -3.0f), t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (visualObj != null) Destroy(visualObj);
    }

    public Sprite GetIcon(string type, string color)
    {
        if (color == "white")
        {
            switch (type)
            {
                case "king": return icon_king_white;
                case "queen": return icon_queen_white;
                case "rook": return icon_rook_white;
                case "bishop": return icon_bishop_white;
                case "knight": return icon_knight_white;
                case "pawn": return icon_pawn_white;
            }
        }
        else
        {
            switch (type)
            {
                case "king": return icon_king_black;
                case "queen": return icon_queen_black;
                case "rook": return icon_rook_black;
                case "bishop": return icon_bishop_black;
                case "knight": return icon_knight_black;
                case "pawn": return icon_pawn_black;
            }
        }
        return null;
    }

    GameObject SpawnCardObject(Card data, Vector3 pos, int index, int sortOrder)
    {
        GameObject obj = Instantiate(cardPrefab, pos, Quaternion.identity);
        obj.name = "Card_" + data.cardName;
        obj.tag = "Bank";

        CardObject co = obj.GetComponent<CardObject>();
        co.SetOriginalPos(pos);
        co.Setup(data, index, sortOrder);

        activeHandCards.Add(obj);
        return obj;
    }
}