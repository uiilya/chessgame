using UnityEngine;

public class MovePlate : MonoBehaviour
{
    public GameObject controller;
    GameObject reference = null;
    int matrixX;
    int matrixY;
    public bool attack = false;

    public void Start()
    {
        // FIX: Force Hitbox to fill the entire 1x1 grid square
        // This makes selecting the move destination much easier.
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col == null) col = gameObject.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1.0f, 1.0f);
        col.offset = Vector2.zero;

        if (attack)
        {
            gameObject.GetComponent<SpriteRenderer>().color = new Color(1.0f, 0.0f, 0.0f, 0.5f);
        }
    }

    public void OnMouseUpReference()
    {
        controller = GameObject.FindGameObjectWithTag("GameController");
        GameManager gm = controller.GetComponent<GameManager>();

        // 1. Attack Logic
        if (attack)
        {
            GameObject cp = gm.GetPosition(matrixX, matrixY);
            
            // This calls CapturePiece -> which now plays PlayPieceImpact()
            gm.CapturePiece(cp);
        }
        else
        {
            // If we are NOT capturing, we are moving normally.
            // Play the "Swish" sound.
            gm.PlayMoveSound();
        }

        // 2. Move Logic
        gm.MovePiece(reference, matrixX, matrixY);
        
        // 3. Cleanup
        reference.GetComponent<ChessPiece>().DestroyMovePlates();
    }

    public void OnMouseUp()
    {
        OnMouseUpReference();
    }

    public void SetCoords(int x, int y) { matrixX = x; matrixY = y; }
    public void SetReference(GameObject obj) { reference = obj; }
    public GameObject GetReference() { return reference; }
}