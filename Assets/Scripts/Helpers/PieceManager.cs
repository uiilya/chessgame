using UnityEngine;
using System.Collections; // Required for Coroutines
using System.Collections.Generic;

public class PieceManager : MonoBehaviour
{
    [Header("Assets")]
    public GameObject chesspiecePrefab;
    
    // The Logical Grids
    private GameObject[,] positions = new GameObject[8, 8]; 
    private GameObject[,] pieces = new GameObject[8, 8];    

    private List<GameObject> capturedWhite = new List<GameObject>();
    private List<GameObject> capturedBlack = new List<GameObject>();

    // ANIMATION SETTINGS
    private float moveDuration = 0.3f; // Seconds

    public void RegisterTile(int x, int y, GameObject tile)
    {
        positions[x, y] = tile;
    }

    public void CreatePiece(string name, int x, int y)
    {
        // FIX: Calculate the world position based on grid X,Y immediately
        // This prevents the piece from spawning at 0,0 and snapping later
        Vector3 spawnPos = new Vector3(x, y, -1.0f);

        GameObject obj = Instantiate(chesspiecePrefab, spawnPos, Quaternion.identity);
        ChessPiece cm = obj.GetComponent<ChessPiece>();
        obj.name = name; 
        
        cm.SetXBoard(x);
        cm.SetYBoard(y);
        cm.Activate(); // This will confirm the coords, but it's already in the right spot visually
        
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

    // UPDATED: Now returns an IEnumerator so GameManager can wait for it
    public IEnumerator MovePieceLogic(GameObject piece, int x, int y)
    {
        ChessPiece cp = piece.GetComponent<ChessPiece>();
        
        // 1. Update Logic Grid Immediately (So data is correct)
        SetPosition(cp.GetXBoard(), cp.GetYBoard(), null);
        cp.SetXBoard(x);
        cp.SetYBoard(y);
        SetPosition(x, y, piece);

        // 2. Animate Visuals
        Vector3 startPos = piece.transform.position;
        // Target Z is -1.0f (Standard piece depth)
        Vector3 endPos = new Vector3(x, y, -1.0f); 
        
        float elapsedTime = 0;
        
        // Lift piece slightly while moving
        Vector3 flightPos; 

        while (elapsedTime < moveDuration)
        {
            float t = elapsedTime / moveDuration;
            // Optional: Add an "Arc" or "Ease Out"
            t = t * t * (3f - 2f * t); // SmoothStep easing

            flightPos = Vector3.Lerp(startPos, endPos, t);
            // flightPos.z = -2.0f; // Optional: Lift up

            piece.transform.position = flightPos;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 3. Snap to final position
        piece.transform.position = endPos;
    }

    public void CapturePiece(GameObject piece)
    {
        if (piece == null) return;
        ChessPiece cp = piece.GetComponent<ChessPiece>();
        bool isWhite = (cp.player == "white");

        GameObject deadPiece = new GameObject("Dead_" + piece.name);
        SpriteRenderer sr = deadPiece.AddComponent<SpriteRenderer>();
        
        // FIX: We now look for the sprite in the CHILDREN, because the visuals were moved to a child object
        SpriteRenderer originalSr = piece.GetComponentInChildren<SpriteRenderer>();
        if (originalSr != null) 
        {
            sr.sprite = originalSr.sprite;
        }
        
        sr.color = new Color(1f, 1f, 1f, 0.6f); 
        deadPiece.transform.localScale = Vector3.one * 0.7f; 

        if (isWhite)
        {
            capturedWhite.Add(deadPiece);
            float yPos = 7f - (capturedWhite.Count * 0.7f);
            deadPiece.transform.position = new Vector3(-2.0f, yPos, 0);
        }
        else
        {
            capturedBlack.Add(deadPiece);
            float yPos = 7f - (capturedBlack.Count * 0.7f);
            deadPiece.transform.position = new Vector3(9.0f, yPos, 0);
        }

        Destroy(piece);
    }
}