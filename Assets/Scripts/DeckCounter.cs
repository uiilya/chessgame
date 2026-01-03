using UnityEngine;
using TMPro;

public class DeckCounter : MonoBehaviour
{
    [Header("Settings")]
    public string owner = "white"; // Set to "white" for Player, "black" for AI
    public string prefix = "";     // Optional: "Cards: "
    
    [Header("References")]
    public DeckManager deckManager;

    private TextMeshPro textMesh;

    void Start()
    {
        textMesh = GetComponent<TextMeshPro>();
        
        // Auto-find DeckManager if not assigned
        if (deckManager == null) 
            deckManager = FindFirstObjectByType<DeckManager>();
    }

    void Update()
    {
        if (deckManager != null && textMesh != null)
        {
            int count = (owner == "white") ? deckManager.whiteDeck.Count : deckManager.blackDeck.Count;
            textMesh.text = prefix + count.ToString();
        }
    }
}