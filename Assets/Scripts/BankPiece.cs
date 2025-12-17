using UnityEngine;

public class BankPiece : MonoBehaviour
{
    // The name of the piece this button represents (e.g., "white_rook")
    public string pieceName; 
    public GameObject controller;
    
    void Start()
    {
        controller = GameObject.FindGameObjectWithTag("GameController");
    }

    public void OnMouseUp()
    {
        // Tell the Game Manager: "The player is currently holding this piece name"
        controller.GetComponent<GameManager>().SelectPieceToPlace(pieceName);
        
        // Visual Feedback: Let's log it so we know it worked
        Debug.Log("Selected: " + pieceName);
    }
}