using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("Art Assets")]
    public GameObject tilePrefab; 
    public GameObject chesspiece; 
    public GameObject movePlatePrefab; 

    [Header("UI Elements")]
    public GameObject gameOverPanel; 
    public Text winnerText;
    public GameObject startButton;
    public Text turnIndicator; 

    [Header("AI Settings")]
    public StockfishManager stockfishManager;
    private BoardStateConverter boardConverter;
    private bool waitingForAI = false;

    // Internal Variables
    private GameObject[,] positions = new GameObject[8, 8];
    private GameObject[,] pieces = new GameObject[8, 8]; 

    private string currentPlayer = "white";
    private bool gameOver = false;
    private bool isSetupPhase = true; 
    private string pieceToPlaceName = null; 

    void Start()
    {
        boardConverter = new BoardStateConverter();
        GenerateBoard();
        
        // Spawn Kings
        CreatePiece("white_king", 4, 0);
        CreatePiece("black_king", 4, 7);

        SpawnBank();
        
        Camera.main.transform.position = new Vector3(3.5f, 3.5f, -10);
        Camera.main.orthographicSize = 6.0f;

        if(turnIndicator) turnIndicator.text = "Setup Phase";
    }

    void Update()
    {
        if (gameOver) return;

        if (isSetupPhase) UpdateSetupPhase();
        else UpdateGameplayPhase();
    }

    // =========================================================
    //                PHASE 1: SETUP LOGIC
    // =========================================================

    void SpawnBank()
    {
        string[] bankPieces = { "queen", "bishop", "knight", "rook", "pawn" };
        for (int i = 0; i < bankPieces.Length; i++)
        {
            SpawnBankIcon("white_" + bankPieces[i], -2.5f, 1.0f + (i * 1.2f));
            SpawnBankIcon("black_" + bankPieces[i], 9.5f, 1.0f + (i * 1.2f));
        }
    }

    void SpawnBankIcon(string name, float x, float y)
    {
        GameObject obj = Instantiate(chesspiece, new Vector3(x, y, -1), Quaternion.identity);
        obj.name = name; 
        obj.tag = "Bank"; 
        
        ChessPiece cp = obj.GetComponent<ChessPiece>();
        cp.Activate();
        
        Destroy(cp); 
        Destroy(obj.GetComponent<BoxCollider2D>()); 
        
        obj.AddComponent<BoxCollider2D>(); 
        BankPiece bp = obj.AddComponent<BankPiece>();
        bp.pieceName = name; 
    }

    public void SelectPieceToPlace(string name) { pieceToPlaceName = name; }

    void UpdateSetupPhase()
    {
        if (Input.GetMouseButtonDown(0) && pieceToPlaceName != null)
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            int x = Mathf.RoundToInt(mousePos.x);
            int y = Mathf.RoundToInt(mousePos.y);

            if (CanPlacePiece(x, y)) CreatePiece(pieceToPlaceName, x, y);
        }
    }

    bool CanPlacePiece(int x, int y)
    {
        if (!PositionOnBoard(x, y)) return false;
        if (GetPosition(x, y) != null) return false;

        string[] parts = pieceToPlaceName.Split('_');
        string colorToPlace = parts[0];
        string typeToPlace = parts[1];

        if (colorToPlace == "white" && y > 2) return false;
        if (colorToPlace == "black" && y < 5) return false;

        if (typeToPlace == "pawn" && (y == 0 || y == 7)) return false;

        return true;
    }

    public void StartGame()
    {
        isSetupPhase = false;
        startButton.SetActive(false);
        
        GameObject[] bankObjs = GameObject.FindGameObjectsWithTag("Bank"); 
        foreach(GameObject g in bankObjs) Destroy(g);
        
        boardConverter.fullmoveNumber = 1;
        boardConverter.halfmoveClock = 0;

        currentPlayer = "white";
        if(turnIndicator) turnIndicator.text = "Turn: White";
        
        // Check if White is somehow mated instantly (Unlikely, but consistent)
        TriggerOracle();
    }

    // =========================================================
    //                PHASE 2: GAMEPLAY + AI LOGIC
    // =========================================================

    void UpdateGameplayPhase()
    {
        // Only block input if it is strictly the AI's turn to MOVE
        if (currentPlayer == "black") return;
        
        // Note: We do NOT block input while "waitingForAI" during White's turn.
        // We let the player think/move while Stockfish checks for mate in the background.

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 mousePos2D = new Vector2(mousePos.x, mousePos.y);
            RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);

            if (hit.collider != null)
            {
                if (hit.collider.GetComponent<ChessPiece>() != null)
                {
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
        currentPlayer = (currentPlayer == "white") ? "black" : "white";
        if(turnIndicator) turnIndicator.text = "Turn: " + char.ToUpper(currentPlayer[0]) + currentPlayer.Substring(1);

        // Always ask Stockfish to evaluate the position
        TriggerOracle();
    }

    void TriggerOracle()
    {
        // We set this flag so we know we are expecting a reply
        // But we only strictly block Input if it's the AI's turn
        if(currentPlayer == "black") waitingForAI = true;

        string fen = boardConverter.BoardToFEN(pieces, currentPlayer);
        stockfishManager.GetBestMove(fen, OnStockfishResponse);
    }

    void OnStockfishResponse(string uciMove)
    {
        // 1. CHECK FOR CHECKMATE / STALEMATE
        // Stockfish returns "(none)" or "none" if there are no legal moves
        if (uciMove == "(none)" || uciMove == "none")
        {
            Debug.Log("Stockfish returned (none). Checkmate detected!");
            
            // If it's White's turn and no moves -> White Lost
            if (currentPlayer == "white") Winner("black");
            
            // If it's Black's turn and no moves -> Black Lost
            else Winner("white");
            
            waitingForAI = false;
            return;
        }

        if (string.IsNullOrEmpty(uciMove))
        {
            Debug.LogError("Stockfish failed to return a move.");
            waitingForAI = false;
            return;
        }

        // 2. HANDLE MOVES BASED ON TURN
        if (currentPlayer == "black")
        {
            // AI Turn: Execute the move
            if (boardConverter.ParseUCIMove(uciMove, out int fromX, out int fromY, out int toX, out int toY, out char promotion))
            {
                ExecuteAIMove(fromX, fromY, toX, toY, promotion);
            }
        }
        else
        {
            // Human Turn: Do nothing. 
            // We just wanted to confirm we weren't checkmated.
            // (Optional: You could log "Hint: Stockfish suggests " + uciMove);
        }
    }

    void ExecuteAIMove(int fromX, int fromY, int toX, int toY, char promotion)
    {
        GameObject aiPiece = GetPosition(fromX, fromY);
        GameObject targetPiece = GetPosition(toX, toY);
        
        if (targetPiece != null)
        {
            // FAILSAFE: If logic somehow missed a mate, but AI captures King, declare win.
            if (targetPiece.name.Contains("king"))
            {
                Destroy(targetPiece);
                Winner("black"); 
                return;
            }
            Destroy(targetPiece);
        }

        MovePiece(aiPiece, toX, toY);
        
        if(promotion == 'q')
        {
            Destroy(aiPiece);
            CreatePiece("black_queen", toX, toY);
            pieces[toX, toY] = GetPosition(toX, toY); 
        }
        
        waitingForAI = false;
    }

    public void MovePiece(GameObject piece, int x, int y)
    {
        ChessPiece cp = piece.GetComponent<ChessPiece>();
        
        SetPosition(cp.GetXBoard(), cp.GetYBoard(), null);
        cp.SetXBoard(x);
        cp.SetYBoard(y);
        cp.SetCoords(); 
        SetPosition(x, y, piece);

        NextTurn();
    }
    
    // Core Helpers
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

    public void CreatePiece(string name, int x, int y) {
        GameObject obj = Instantiate(chesspiece, new Vector3(0,0,-1), Quaternion.identity);
        ChessPiece cm = obj.GetComponent<ChessPiece>();
        obj.name = name; 
        cm.SetXBoard(x);
        cm.SetYBoard(y);
        cm.Activate();
        pieces[x, y] = obj;
    }

    public bool PositionOnBoard(int x, int y) {
        if (x < 0 || y < 0 || x >= 8 || y >= 8) return false;
        return true;
    }

    public GameObject GetPosition(int x, int y) {
        if (!PositionOnBoard(x,y)) return null;
        return pieces[x, y];
    }

    public void SetPosition(int x, int y, GameObject obj) {
        pieces[x, y] = obj;
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void Winner(string playerWinner) {
        gameOver = true;
        gameOverPanel.SetActive(true);
        winnerText.text = playerWinner + " wins!";
    }
}