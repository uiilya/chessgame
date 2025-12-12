using UnityEngine;

public class MovePlate : MonoBehaviour
{
    // Some reference to the game board controller
    public GameObject controller;

    // The piece that created this plate (the one we want to move)
    GameObject reference = null;

    // Board coordinates for this specific plate
    int matrixX;
    int matrixY;

    // Boolean: false = movement, true = attacking (taking a piece)
    public bool attack = false;

    public void Start()
    {
        // If it's an attack, change color to Red
        if (attack)
        {
            // Set to Red
            gameObject.GetComponent<SpriteRenderer>().color = new Color(1.0f, 0.0f, 0.0f, 0.5f);
        }
    }

    public void OnMouseUp()
    {
        controller = GameObject.FindGameObjectWithTag("GameController");

        // When clicked, we call the Move function on the GameManager
        // We pass the piece we want to move, and the coordinates of this plate
        if (attack)
        {
            GameObject cp = controller.GetComponent<GameManager>().GetPosition(matrixX, matrixY);
            
            // If we are attacking, we destroy the piece currently at that location
            Destroy(cp);
        }

        // Tell the game to move the piece to these coordinates
        controller.GetComponent<GameManager>().MovePiece(reference, matrixX, matrixY);
        
        // After moving, we don't need these highlights anymore
        // (We will add a function to destroy all plates later)
        reference.GetComponent<ChessPiece>().DestroyMovePlates();
    }

    public void SetCoords(int x, int y)
    {
        matrixX = x;
        matrixY = y;
    }

    public void SetReference(GameObject obj)
    {
        reference = obj;
    }

    public GameObject GetReference()
    {
        return reference;
    }
}