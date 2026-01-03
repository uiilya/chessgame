using UnityEngine;

[System.Serializable]
public class Card
{
    public string cardName;
    [TextArea] public string description; // Adds a bigger box in Inspector for text
    public int cost;
    public string pieceType;
    public string owner;

    // --- NEW: The specific art for the Card UI ---
    public Sprite cardArt; 

    // Updated Constructor
    public Card(string name, string type, string owner, int cost, string desc, Sprite art = null)
    {
        this.cardName = name;
        this.pieceType = type;
        this.owner = owner;
        this.cost = cost;
        this.description = desc;
        this.cardArt = art;
    }
}