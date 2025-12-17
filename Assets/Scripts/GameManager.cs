using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    // References we need to drag in via the Inspector
    [Header("Art Assets")]
    public GameObject tilePrefab; 
    public GameObject chesspiece; // <--- ADD THIS: Reference to the single "Base Piece" prefab
    public GameObject movePlatePrefab; // <--- ADD THIS


    // Internal Variables
    private GameObject[,] positions = new GameObject[8, 8]; // The "Logical" Grid for TILES
    private GameObject[,] pieces = new GameObject[8, 8]; // <--- ADD THIS: The "Logical" Grid for PIECES
    private GameObject[] playerBlack = new GameObject[16];
    private GameObject[] playerWhite = new GameObject[16];

    private string currentPlayer = "white";
    private bool gameOver = false;
    
    [Header("AI Settings")]
    public StockfishManager stockfishManager;
    public BoardStateConverter boardConverter;
    private bool waitingForAI = false;

    // Unity calls this function automatically when the game starts
    void Start()
    {
        // Initialize board state converter
        boardConverter = new BoardStateConverter();
        
        GenerateBoard();
        
        // <--- ADD THIS BLOCK: Spawn White Pieces
        CreatePiece("white_rook", 0, 0);
        CreatePiece("white_knight", 1, 0);
        CreatePiece("white_bishop", 2, 0);
        CreatePiece("white_queen", 3, 0);
        CreatePiece("white_king", 4, 0);
        CreatePiece("white_bishop", 5, 0);
        CreatePiece("white_knight", 6, 0);
        CreatePiece("white_rook", 7, 0);
        
        for(int i = 0; i < 8; i++) 
        { 
            CreatePiece("white_pawn", i, 1); 
        }

        // <--- ADD THIS BLOCK: Spawn Black Pieces
        CreatePiece("black_rook", 0, 7);
        CreatePiece("black_knight", 1, 7);
        CreatePiece("black_bishop", 2, 7);
        CreatePiece("black_queen", 3, 7);
        CreatePiece("black_king", 4, 7);
        CreatePiece("black_bishop", 5, 7);
        CreatePiece("black_knight", 6, 7);
        CreatePiece("black_rook", 7, 7);
        
        for(int i = 0; i < 8; i++) 
        { 
            CreatePiece("black_pawn", i, 6); 
        }
    }

    void Update()
    {
        if (gameOver == true) return;

        // 0 = Left Click
        if (Input.GetMouseButtonDown(0))
        {
            // Raycast Logic
            // 1. Get the mouse position in World Space
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            
            // 2. Set the Z to a standard value (2D doesn't care about depth for raycasts usually, but good practice)
            Vector2 mousePos2D = new Vector2(mousePos.x, mousePos.y);
            
            // 3. Fire the laser!
            RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);

            if (hit.collider != null)
            {
                // We hit something!
                if (hit.collider.GetComponent<ChessPiece>() != null)
                {
                   ChessPiece clickedPiece = hit.collider.GetComponent<ChessPiece>();
                   
                   // Only allow moves if it's the player's turn and not waiting for AI
                   if (waitingForAI)
                   {
                       Debug.Log("Waiting for AI to move...");
                       return;
                   }
                   
                   // Check if the piece belongs to the current player
                   if (clickedPiece.player == currentPlayer)
                   {
                       Debug.Log("I clicked: " + hit.collider.name);
                       clickedPiece.InitiateMovePlates();
                   }
                   else
                   {
                       Debug.Log("Not your turn! Current player: " + currentPlayer);
                   }
                }
            }
        }
    }

    // Function to draw the 8x8 Grid
    private void GenerateBoard()
    {
        // 8 columns (x), 8 rows (y)
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                // 1. Create the tile in the game world
                // We offset by x and y so they don't stack on top of each other
                GameObject newTile = Instantiate(tilePrefab, new Vector3(x, y, 0), Quaternion.identity);
                
                // 2. Name it nicely in the Hierarchy (e.g., "Tile 0,0")
                newTile.name = $"Tile {x},{y}";

                // 3. Coloring logic:
                // If x and y are both Even or both Odd, it's a dark square (or light, depending on preference).
                // A common trick is (x + y) % 2.
                bool isOffset = (x + y) % 2 != 0;
                
                // Let's assume the default sprite is White. We only color the offset ones.
                if (isOffset) 
                {
                    // Tint the sprite darker
                    newTile.GetComponent<SpriteRenderer>().color = new Color(0.6f, 0.6f, 0.6f); 
                }

                // 4. (Important) Store this tile in our "Logical" grid
                // This allows us to access "A4" later just by asking for positions[0, 3]
                positions[x, y] = newTile;
            }
        }

        // Optional: Move the camera to the center of the board
        Camera.main.transform.position = new Vector3(3.5f, 3.5f, -10);
    }
    
    public void CreatePiece(string name, int x, int y)
    {
        // 1. Instantiate the generic prefab
        GameObject obj = Instantiate(chesspiece, new Vector3(0,0,-1), Quaternion.identity);
        
        // 2. Access the script attached to it
        ChessPiece cm = obj.GetComponent<ChessPiece>();
        
        // 3. Set the name. This is crucial because Activate() uses the name to pick the sprite!
        obj.name = name; 
        
        // 4. Set logical coordinates
        cm.SetXBoard(x);
        cm.SetYBoard(y);
        
        // 5. Trigger the setup (loads sprite, snaps to grid)
        cm.Activate();
        
        // 6. Add to our Logic Grid so we know a piece is there
        pieces[x, y] = obj;
    }

    // Helper to get a piece at a coordinate (returns null if empty or out of bounds)
    public GameObject GetPosition(int x, int y)
    {
        if (x < 0 || x > 7 || y < 0 || y > 7) return null;
        return pieces[x, y];
    }

    // Helper to update the grid when a piece moves
    public void SetPosition(int x, int y, GameObject obj)
    {
        pieces[x, y] = obj;
    }
    
    // helper to clean up valid coordinates
    public bool PositionOnBoard(int x, int y)
    {
        if (x < 0 || y < 0 || x >= positions.GetLength(0) || y >= positions.GetLength(1)) return false;
        return true;
    }

    public void MovePiece(GameObject piece, int x, int y)
    {
        // 1. Get the script of the piece trying to move
        ChessPiece cp = piece.GetComponent<ChessPiece>();
        
        // 2. Clear its OLD position in the grid
        // We set the slot it was standing on to null
        SetPosition(cp.GetXBoard(), cp.GetYBoard(), null);

        // 3. Update the piece's internal coordinates
        cp.SetXBoard(x);
        cp.SetYBoard(y);
        cp.SetCoords(); // This physically moves the sprite

        // 4. Update the grid's NEW position
        SetPosition(x, y, piece);

        // 5. Update castling rights and en passant
        if (boardConverter != null)
        {
            boardConverter.UpdateCastlingRights(piece, cp.GetXBoard(), cp.GetYBoard());
            
            // Check if pawn moved (for en passant and halfmove clock)
            bool isPawn = piece.name.ToLower().Contains("pawn");
            if (isPawn)
            {
                boardConverter.SetEnPassantSquare(cp.GetYBoard(), y, x);
            }
            else
            {
                boardConverter.ClearEnPassant();
            }
        }

        // 6. Switch Turns
        NextTurn();
    }
    
    void NextTurn()
    {
        // Switch player
        currentPlayer = (currentPlayer == "white") ? "black" : "white";
        Debug.Log("Turn: " + currentPlayer);
        
        // Update move counters
        if (boardConverter != null)
        {
            boardConverter.IncrementMoveCounters(false, currentPlayer == "white");
        }
        
        // If it's black's turn and we have Stockfish, get AI move
        if (currentPlayer == "black" && stockfishManager != null)
        {
            TriggerAIMove();
        }
    }
    
    void TriggerAIMove()
    {
        waitingForAI = true;
        
        // Get current board state as FEN
        string fen = boardConverter.BoardToFEN(pieces, currentPlayer);
        Debug.Log("Current FEN: " + fen);
        
        // Request best move from Stockfish
        stockfishManager.GetBestMove(fen, OnAIMoveReceived);
    }
    
    void OnAIMoveReceived(string uciMove)
    {
        if (string.IsNullOrEmpty(uciMove))
        {
            Debug.LogError("Stockfish did not return a valid move!");
            waitingForAI = false;
            return;
        }
        
        Debug.Log("AI wants to move: " + uciMove);
        
        // Parse UCI move
        if (boardConverter.ParseUCIMove(uciMove, out int fromX, out int fromY, out int toX, out int toY, out char promotion))
        {
            ExecuteAIMove(fromX, fromY, toX, toY, promotion);
        }
        else
        {
            Debug.LogError("Failed to parse UCI move: " + uciMove);
            waitingForAI = false;
        }
    }
    
    void ExecuteAIMove(int fromX, int fromY, int toX, int toY, char promotion)
    {
        GameObject piece = GetPosition(fromX, fromY);
        
        if (piece == null)
        {
            Debug.LogError($"No piece found at {fromX},{fromY}");
            waitingForAI = false;
            return;
        }
        
        // Check if capturing opponent piece
        GameObject targetPiece = GetPosition(toX, toY);
        if (targetPiece != null)
        {
            // Destroy captured piece
            Destroy(targetPiece);
        }
        
        // Move the piece
        MovePiece(piece, toX, toY);
        
        // Handle pawn promotion
        if (promotion != ' ')
        {
            PromotePawn(piece, toX, toY, promotion);
        }
        
        waitingForAI = false;
    }
    
    void PromotePawn(GameObject pawn, int x, int y, char promotionPiece)
    {
        // Destroy the pawn
        Destroy(pawn);
        
        // Create new piece based on promotion character
        string color = currentPlayer;
        string pieceName = "";
        
        switch (promotionPiece)
        {
            case 'q': pieceName = color + "_queen"; break;
            case 'r': pieceName = color + "_rook"; break;
            case 'b': pieceName = color + "_bishop"; break;
            case 'n': pieceName = color + "_knight"; break;
            default: pieceName = color + "_queen"; break; // Default to queen
        }
        
        CreatePiece(pieceName, x, y);
    }
}