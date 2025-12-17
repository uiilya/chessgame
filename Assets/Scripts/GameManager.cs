using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    // References we need to drag in via the Inspector
    [Header("Art Assets")]
    public GameObject tilePrefab; 
    public GameObject chesspiece; // <--- ADD THIS: Reference to the single "Base Piece" prefab
    public GameObject movePlatePrefab; // <--- ADD THIS

    [Header("UI Elements")]
    public GameObject gameOverPanel;
    public Text winnerText;
    public GameObject startButton;
    public Text turnIndicator;

    // Internal Variables
    private GameObject[,] positions = new GameObject[8, 8]; // The "Logical" Grid for TILES
    private GameObject[,] pieces = new GameObject[8, 8]; // <--- ADD THIS: The "Logical" Grid for PIECES
    // private GameObject[] playerBlack = new GameObject[16];
    // private GameObject[] playerWhite = new GameObject[16];

    private string currentPlayer = "white";
    private bool gameOver = false;

    // <--- NEW: State Machine
    private bool isSetupPhase = true; 
    private string pieceToPlaceName = null;

    // Unity calls this function automatically when the game starts
    void Start()
    {
        GenerateBoard();
        
        // 1. Spawn Kings
        CreatePiece("white_king", 4, 0);
        CreatePiece("black_king", 4, 7);

        // 2. Spawn Bank
        SpawnBank();
        
        // 3. FIX: Center the Camera Automatically
        // The board is 0-7. Center is 3.5. 
        // We set Z to -10 so the camera can see the board.
        Camera.main.transform.position = new Vector3(3.5f, 3.5f, -10);
        
        // Optional: Force the Camera Size to be wide enough to see the banks
        Camera.main.orthographicSize = 6.0f; // Adjust this number if your banks are still cut off

        if(turnIndicator) turnIndicator.text = "Setup Phase";
    }

    void Update()
    {
        if (gameOver == true) return;

        if (isSetupPhase)
        {
            UpdateSetupPhase();
        }
        else
        {
            UpdateGameplayPhase();
        }

        // 0 = Left Click
        // if (Input.GetMouseButtonDown(0))
        // {
        //     // Raycast Logic
        //     // 1. Get the mouse position in World Space
        //     Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            
        //     // 2. Set the Z to a standard value (2D doesn't care about depth for raycasts usually, but good practice)
        //     Vector2 mousePos2D = new Vector2(mousePos.x, mousePos.y);
            
        //     // 3. Fire the laser!
        //     RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);

        //     if (hit.collider != null)
        //     {
        //         // We hit something!
        //         if (hit.collider.GetComponent<ChessPiece>() != null)
        //         {
        //            // For now, let's just log it to prove it works
        //            Debug.Log("I clicked: " + hit.collider.name);
        //            // When we click a piece, we will eventually ask it to show its moves
        //            hit.collider.GetComponent<ChessPiece>().InitiateMovePlates();
        //         }
        //     }
        // }
    }

    // =========================================================
    //                PHASE 1: SETUP LOGIC
    // =========================================================

    void SpawnBank()
    {
        string[] bankPieces = { "queen", "bishop", "knight", "rook", "pawn" };

        // We want to center the bank vertically.
        // The board center is 3.5. 
        // Let's start spawning from y = 1 and go up.
        
        for (int i = 0; i < bankPieces.Length; i++)
        {
            // Left Side (White)
            // x = -2.5 ensures it is clearly off the board (assuming tiles are 1 unit wide)
            // y = 1.0f + (i * 1.2f) gives us nice vertical spacing starting from near the bottom
            SpawnBankIcon("white_" + bankPieces[i], -2.5f, 1.0f + (i * 1.2f));
        }

        for (int i = 0; i < bankPieces.Length; i++)
        {
            // Right Side (Black)
            // x = 9.5
            SpawnBankIcon("black_" + bankPieces[i], 9.5f, 1.0f + (i * 1.2f));
        }
    }

    void SpawnBankIcon(string name, float x, float y)
    {
        GameObject obj = Instantiate(chesspiece, new Vector3(x, y, -1), Quaternion.identity);
        
        // 1. Set Name for Sprite Lookup
        obj.name = name; 
        
        // 2. Set Tag so we can delete it later
        // ERROR PREVENTION: If you haven't made the tag in Editor, this line will crash.
        // Ensure you added the "Bank" tag in the Inspector!
        obj.tag = "Bank"; 

        ChessPiece cp = obj.GetComponent<ChessPiece>();
        cp.Activate();
        
        // Strip Chess Logic
        Destroy(cp); 
        Destroy(obj.GetComponent<BoxCollider2D>()); 
        
        // Add Bank Logic
        obj.AddComponent<BoxCollider2D>(); 
        BankPiece bp = obj.AddComponent<BankPiece>();
        bp.pieceName = name; 
    }

    public void SelectPieceToPlace(string name)
    {
        pieceToPlaceName = name;
    }

    void UpdateSetupPhase()
    {
        if (Input.GetMouseButtonDown(0) && pieceToPlaceName != null)
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            // Round to nearest integer to snap to grid
            int x = Mathf.RoundToInt(mousePos.x);
            int y = Mathf.RoundToInt(mousePos.y);

            if (CanPlacePiece(x, y))
            {
                CreatePiece(pieceToPlaceName, x, y);
            }
        }
    }

    // public void SelectPieceToPlace(GameObject prefab)
    // {
    //     // Called by BankPiece script
    //     pieceToPlaceName = name;
    //     Debug.Log("Selected to place: " + pieceToPlaceName);
    // }

    bool CanPlacePiece(int x, int y)
    {
        // 1. Is it on the board?
        if (!PositionOnBoard(x, y)) return false;

        // 2. Is the tile empty?
        if (GetPosition(x, y) != null) return false;

        // 3. ROW RESTRICTION RULE
        // White can only place in rows 0, 1, 2
        // Black can only place in rows 5, 6, 7
        string colorToPlace = pieceToPlaceName.Split('_')[0]; 

        if (colorToPlace == "white" && y > 2) return false;
        if (colorToPlace == "black" && y < 5) return false;

        return true;
    }

    public void StartGame()
    {
        isSetupPhase = false;
        startButton.SetActive(false);
        
        // This is the line that was crashing before!
        // It tries to find objects with tag "Bank".
        GameObject[] bankObjs = GameObject.FindGameObjectsWithTag("Bank"); 
        
        foreach(GameObject g in bankObjs)
        {
            Destroy(g);
        }
        
        currentPlayer = "white";
        if(turnIndicator) turnIndicator.text = "Turn: White";
    }

    // =========================================================
    //                PHASE 2: GAMEPLAY LOGIC
    // =========================================================

    void UpdateGameplayPhase()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 mousePos2D = new Vector2(mousePos.x, mousePos.y);
            RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);

            if (hit.collider != null)
            {
                if (hit.collider.GetComponent<ChessPiece>() != null)
                {
                    // TURN CHECK: Ensures you can only move your own pieces
                    if (hit.collider.GetComponent<ChessPiece>().player == currentPlayer)
                    {
                        hit.collider.GetComponent<ChessPiece>().InitiateMovePlates();
                    }
                }
            }
        }
    }

    public void NextTurn()
    {
        // Simple Toggle
        if (currentPlayer == "white")
        {
            currentPlayer = "black";
        }
        else
        {
            currentPlayer = "white";
        }
        
        if(turnIndicator) turnIndicator.text = "Turn: " + char.ToUpper(currentPlayer[0]) + currentPlayer.Substring(1);
    }

    // =========================================================
    //                HELPER FUNCTIONS
    // =========================================================

    // void SpawnBank()
    // {
    //     // Helper to spawn a clickable bank icon
    //     void SpawnIcon(string name, int x, int y)
    //     {
    //         // Use the standard ChessPiece prefab, but we will attach a BankPiece script to it dynamically
    //         // OR simpler: Instantiate the BasePiece, set its sprite, and add BankPiece component
    //         GameObject obj = Instantiate(chesspiece, new Vector3(x, y, -1), Quaternion.identity);
    //         obj.name = name;
            
    //         // Setup the look
    //         ChessPiece cp = obj.GetComponent<ChessPiece>();
    //         cp.Activate(); // Load the sprite
    //         Destroy(cp); // Remove the Chess logic! It's just an icon now.
    //         Destroy(obj.GetComponent<BoxCollider2D>()); // Remove old collider

    //         // Add new logic
    //         obj.AddComponent<BoxCollider2D>(); // Add fresh collider
    //         BankPiece bp = obj.AddComponent<BankPiece>();
            
    //         // We need a "Real" prefab to give the BankPiece. 
    //         // For this simple tutorial, we will hack it:
    //         // We create a temporary prefab in memory to hold the data
    //         GameObject dummy = new GameObject(name);
    //         dummy.AddComponent<ChessPiece>().player = name.StartsWith("white") ? "white" : "black";
    //         bp.prefabToSpawn = dummy; 
    //         // Note: In a real project, you'd drag real prefabs into a list.
    //     }

    //     // Spawn White Bank (Left side: x = -2)
    //     SpawnIcon("white_queen", -2, 0);
    //     SpawnIcon("white_rook", -2, 1);
    //     SpawnIcon("white_bishop", -2, 2);
    //     SpawnIcon("white_knight", -2, 3);
    //     SpawnIcon("white_pawn", -2, 4);

    //     // Spawn Black Bank (Right side: x = 9)
    //     SpawnIcon("black_queen", 9, 7);
    //     SpawnIcon("black_rook", 9, 6);
    //     SpawnIcon("black_bishop", 9, 5);
    //     SpawnIcon("black_knight", 9, 4);
    //     SpawnIcon("black_pawn", 9, 3);
    // }

    public void CreatePiece(string name, int x, int y)
    {
        GameObject obj = Instantiate(chesspiece, new Vector3(0,0,-1), Quaternion.identity);
        ChessPiece cm = obj.GetComponent<ChessPiece>();
        obj.name = name; 
        cm.SetXBoard(x);
        cm.SetYBoard(y);
        cm.Activate();
        pieces[x, y] = obj;
    }

    public void GenerateBoard() {
        for (int x = 0; x < 8; x++) {
            for (int y = 0; y < 8; y++) {
                GameObject newTile = Instantiate(tilePrefab, new Vector3(x, y, 0), Quaternion.identity);
                newTile.name = $"Tile {x},{y}";
                if ((x + y) % 2 != 0) newTile.GetComponent<SpriteRenderer>().color = new Color(0.6f, 0.6f, 0.6f);
                positions[x, y] = newTile;
            }
        }
    }

    // Function to draw the 8x8 Grid
    // private void GenerateBoard()
    // {
    //     // 8 columns (x), 8 rows (y)
    //     for (int x = 0; x < 8; x++)
    //     {
    //         for (int y = 0; y < 8; y++)
    //         {
    //             // 1. Create the tile in the game world
    //             // We offset by x and y so they don't stack on top of each other
    //             GameObject newTile = Instantiate(tilePrefab, new Vector3(x, y, 0), Quaternion.identity);
                
    //             // 2. Name it nicely in the Hierarchy (e.g., "Tile 0,0")
    //             newTile.name = $"Tile {x},{y}";

    //             // 3. Coloring logic:
    //             // If x and y are both Even or both Odd, it's a dark square (or light, depending on preference).
    //             // A common trick is (x + y) % 2.
    //             bool isOffset = (x + y) % 2 != 0;
                
    //             // Let's assume the default sprite is White. We only color the offset ones.
    //             if (isOffset) 
    //             {
    //                 // Tint the sprite darker
    //                 newTile.GetComponent<SpriteRenderer>().color = new Color(0.6f, 0.6f, 0.6f); 
    //             }

    //             // 4. (Important) Store this tile in our "Logical" grid
    //             // This allows us to access "A4" later just by asking for positions[0, 3]
    //             positions[x, y] = newTile;
    //         }
    //     }

    //     // Optional: Move the camera to the center of the board
    //     Camera.main.transform.position = new Vector3(3.5f, 3.5f, -10);
    // }
    
    // public void CreatePiece(string name, int x, int y)
    // {
    //     // 1. Instantiate the generic prefab
    //     GameObject obj = Instantiate(chesspiece, new Vector3(0,0,-1), Quaternion.identity);
        
    //     // 2. Access the script attached to it
    //     ChessPiece cm = obj.GetComponent<ChessPiece>();
        
    //     // 3. Set the name. This is crucial because Activate() uses the name to pick the sprite!
    //     obj.name = name; 
        
    //     // 4. Set logical coordinates
    //     cm.SetXBoard(x);
    //     cm.SetYBoard(y);
        
    //     // 5. Trigger the setup (loads sprite, snaps to grid)
    //     cm.Activate();
        
    //     // 6. Add to our Logic Grid so we know a piece is there
    //     pieces[x, y] = obj;
    // }

    // Helper to get a piece at a coordinate (returns null if empty or out of bounds)
    public GameObject GetPosition(int x, int y) {
        if (!PositionOnBoard(x,y)) return null;
        return pieces[x, y];
    }

    // Helper to update the grid when a piece moves
    public void SetPosition(int x, int y, GameObject obj)
    {
        pieces[x, y] = obj;
    }
    
    // helper to clean up valid coordinates
    public bool PositionOnBoard(int x, int y) {
        if (x < 0 || y < 0 || x >= 8 || y >= 8) return false;
        return true;
    }

    public void MovePiece(GameObject piece, int x, int y)
    {
        ChessPiece cp = piece.GetComponent<ChessPiece>();
        
        // 1. Clear Old Slot
        SetPosition(cp.GetXBoard(), cp.GetYBoard(), null);
        
        // 2. Move
        cp.SetXBoard(x);
        cp.SetYBoard(y);
        cp.SetCoords(); 
        
        // 3. Update New Slot
        SetPosition(x, y, piece);

        // 4. SWITCH TURNS!
        NextTurn();
    }

    public void Winner(string playerWinner)
    {
        gameOver = true;
        gameOverPanel.SetActive(true);
        winnerText.text = playerWinner + " wins!";
    }
}