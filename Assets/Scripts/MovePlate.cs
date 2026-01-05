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
        if (controller == null) return;

        GameManager gm = controller.GetComponent<GameManager>();

        // REMOVED: Manual CapturePiece call. 
        // REMOVED: Manual PlayMoveSound call.
        // REASON: GameManager.MoveSequence() handles both capturing and sound logic centrally.
        
        // 1. Initiate the Move
        // The GameManager will detect if there is a piece at (matrixX, matrixY) and capture it automatically.
        gm.MovePiece(reference, matrixX, matrixY);
        
        // 2. Cleanup
        // We destroy the plates immediately so the UI is clean while the piece moves.
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