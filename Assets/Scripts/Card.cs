using UnityEngine;

[System.Serializable] // Allows us to see it in Inspector if needed
public class Card
{
    public string cardName;     // "White Rook"
    public string description;  // "Moves in straight lines."
    public int cost;            // 1 (Placeholder for resource system)
    public string pieceType;    // "rook", "bishop", etc.
    public string owner;        // "white" or "black"

    // Constructor for quick creation
    public Card(string name, string type, string owner, int cost, string desc)
    {
        this.cardName = name;
        this.pieceType = type;
        this.owner = owner;
        this.cost = cost;
        this.description = desc;
    }
}