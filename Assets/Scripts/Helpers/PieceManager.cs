using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PieceManager : MonoBehaviour
{
    [Header("Assets")]
    public GameObject chesspiecePrefab;
    
    // The Logical Grids
    private GameObject[,] positions = new GameObject[8, 8]; 
    private GameObject[,] pieces = new GameObject[8, 8];    

    [Header("Graveyard Settings")]
    public Transform whiteGraveyardAnchor; // Drag 'Anchor_Graveyard_BotRight' here (White's casualties)
    public Transform blackGraveyardAnchor; // Drag 'Anchor_Graveyard_TopLeft' here (Black's casualties)
    public float graveyardSpacing = 0.6f;  // Space between dead pieces
    public int graveyardColumns = 4;       // How many pieces per row before wrapping?
    public float graveyardScale = 0.6f;    // Size of dead pieces

    private List<GameObject> capturedWhite = new List<GameObject>();
    private List<GameObject> capturedBlack = new List<GameObject>();

    // ANIMATION SETTINGS
    private float moveDuration = 0.3f; 

    public void RegisterTile(int x, int y, GameObject tile)
    {
        positions[x, y] = tile;
    }

    public void CreatePiece(string name, int x, int y)
    {
        Vector3 spawnPos = new Vector3(x, y, -1.0f);
        GameObject obj = Instantiate(chesspiecePrefab, spawnPos, Quaternion.identity);
        ChessPiece cm = obj.GetComponent<ChessPiece>();
        obj.name = name; 
        
        cm.SetXBoard(x);
        cm.SetYBoard(y);
        cm.Activate(); 
        
        pieces[x, y] = obj;
    }

    public GameObject GetPosition(int x, int y)
    {
        if (!PositionOnBoard(x,y)) return null;
        return pieces[x, y];
    }
    
    public GameObject[,] GetAllPieces() { return pieces; } 

    public void SetPosition(int x, int y, GameObject obj)
    {
        pieces[x, y] = obj;
    }

    public bool PositionOnBoard(int x, int y)
    {
        if (x < 0 || y < 0 || x >= 8 || y >= 8) return false;
        return true;
    }

    public IEnumerator MovePieceLogic(GameObject piece, int x, int y)
    {
        ChessPiece cp = piece.GetComponent<ChessPiece>();
        
        SetPosition(cp.GetXBoard(), cp.GetYBoard(), null);
        cp.SetXBoard(x);
        cp.SetYBoard(y);
        SetPosition(x, y, piece);

        Vector3 startPos = piece.transform.position;
        Vector3 endPos = new Vector3(x, y, -1.0f); 
        
        float elapsedTime = 0;
        Vector3 flightPos; 

        while (elapsedTime < moveDuration)
        {
            float t = elapsedTime / moveDuration;
            t = t * t * (3f - 2f * t); 

            flightPos = Vector3.Lerp(startPos, endPos, t);
            piece.transform.position = flightPos;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        piece.transform.position = endPos;
    }

    public void CapturePiece(GameObject piece)
    {
        if (piece == null) return;
        ChessPiece cp = piece.GetComponent<ChessPiece>();
        bool isWhite = (cp.player == "white");

        // 1. Create the visual "corpse"
        GameObject deadPiece = new GameObject("Dead_" + piece.name);
        SpriteRenderer sr = deadPiece.AddComponent<SpriteRenderer>();
        
        SpriteRenderer originalSr = piece.GetComponentInChildren<SpriteRenderer>();
        if (originalSr != null) 
        {
            sr.sprite = originalSr.sprite;
            // Optionally flip black pieces if your art requires it
            sr.flipY = originalSr.flipY; 
        }
        
        sr.color = new Color(1f, 1f, 1f); 
        deadPiece.transform.localScale = Vector3.one * graveyardScale; 

        // 2. Determine Graveyard Position
        if (isWhite)
        {
            // Add to list first so we know the index (0, 1, 2...)
            capturedWhite.Add(deadPiece);
            PositionDeadPiece(deadPiece, capturedWhite.Count - 1, whiteGraveyardAnchor);
        }
        else
        {
            capturedBlack.Add(deadPiece);
            PositionDeadPiece(deadPiece, capturedBlack.Count - 1, blackGraveyardAnchor);
        }

        Destroy(piece);
    }

    // New Helper to arrange pieces in a grid
    private void PositionDeadPiece(GameObject obj, int index, Transform anchor)
    {
        if (anchor == null) {
            Debug.LogWarning("Graveyard Anchor not set in Inspector!");
            return;
        }

        // Calculate Row and Column based on index
        // Example: if columns = 4
        // Index 0 -> Col 0, Row 0
        // Index 3 -> Col 3, Row 0
        // Index 4 -> Col 0, Row 1
        int row = index / graveyardColumns;
        int col = index % graveyardColumns;

        // Calculate Offset
        // We move RIGHT by column * spacing
        // We move DOWN by row * spacing
        float xOffset = col * graveyardSpacing;
        float yOffset = row * -graveyardSpacing; // Negative to go down

        Vector3 finalPos = anchor.position + new Vector3(xOffset, yOffset, 0);
        obj.transform.position = finalPos;
    }
}