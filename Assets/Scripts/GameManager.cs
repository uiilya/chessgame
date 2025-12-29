using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections; 

public class GameManager : MonoBehaviour
{
    [Header("Managers")]
    public PieceManager pieceManager;       
    public DeckManager deckManager;         
    public BoardHighlighter highlighter;    
    public StockfishManager stockfishManager;
    public AudioManager audioManager;
    public UIManager uiManager;
    public HandVisualsManager handVisuals;

    [Header("Board Assets")]
    public GameObject tilePrefab; 
    
    [Header("Settings")]
    public bool useCustomBoardArt = true; 
    public bool autoConfigureCamera = true; 

    private BoardStateConverter boardConverter;
    public enum GamePhase { SetupDraw, SetupDeploy, PlayerTurnDeploy, PlayerTurnMove, AITurn }
    private GamePhase currentPhase = GamePhase.SetupDraw;
    private string currentPlayer = "white";
    private bool gameOver = false;

    void Start()
    {
        if(!pieceManager) pieceManager = GetComponent<PieceManager>();
        if(!deckManager) deckManager = GetComponent<DeckManager>();
        if(!highlighter) highlighter = GetComponent<BoardHighlighter>();
        if(!audioManager) audioManager = GetComponent<AudioManager>();
        if(!uiManager) uiManager = GetComponent<UIManager>();
        if(!handVisuals) handVisuals = GetComponent<HandVisualsManager>();

        boardConverter = new BoardStateConverter();
        
        GenerateBoard();
        SpawnKingsAndGuards();

        if (autoConfigureCamera)
        {
            Camera.main.transform.position = new Vector3(3.5f, 2.5f, -10);
            Camera.main.orthographicSize = 7.0f; 
        }

        deckManager.InitializeDecks();
        StartSetupPhase();
    }

    void Update()
    {
        if (gameOver) return;
        if (currentPhase == GamePhase.PlayerTurnMove) HandlePlayerMoveInput();
    }

    // --- DELEGATES & HELPERS ---
    public void ClearMoveHints() => highlighter.ClearMoveHints();
    public GameObject CreateHighlight(int x, int y, Color c, BoardHighlighter.HighlightType t, bool h) 
        => highlighter.CreateHighlight(x, y, c, t, h);
    public bool PositionOnBoard(int x, int y) => pieceManager.PositionOnBoard(x, y);
    public GameObject GetPosition(int x, int y) => pieceManager.GetPosition(x, y);
    
    public void CapturePiece(GameObject piece) 
    {
        if(audioManager) audioManager.PlayPieceImpact();
        pieceManager.CapturePiece(piece);
    }
    
    public void PlayMoveSound()
    {
        if(audioManager) audioManager.PlayPieceMove();
    }

    public void MovePiece(GameObject piece, int x, int y)
    {
        StartCoroutine(MoveSequence(piece, x, y));
    }

    IEnumerator MoveSequence(GameObject piece, int x, int y)
    {
        ChessPiece cp = piece.GetComponent<ChessPiece>();
        int startX = cp.GetXBoard();
        int startY = cp.GetYBoard();

        yield return StartCoroutine(pieceManager.MovePieceLogic(piece, x, y));

        highlighter.HighlightLastMove(startX, startY, x, y);
        highlighter.ClearMoveHints(); 

        if (currentPlayer == "white") StartAITurn();
        else StartPlayerTurn();
    }

    // UPDATED: Now accepts aiClaimedMate flag
    void ExecuteAIMove(int fromX, int fromY, int toX, int toY, char promotion, bool aiClaimedMate)
    {
        StartCoroutine(AIMoveSequence(fromX, fromY, toX, toY, promotion, aiClaimedMate));
    }

    IEnumerator AIMoveSequence(int fromX, int fromY, int toX, int toY, char promotion, bool aiClaimedMate)
    {
        GameObject aiPiece = pieceManager.GetPosition(fromX, fromY);
        GameObject target = pieceManager.GetPosition(toX, toY);
        
        if (target != null) CapturePiece(target);
        else if(audioManager) audioManager.PlayPieceMove();

        yield return StartCoroutine(pieceManager.MovePieceLogic(aiPiece, toX, toY));
        
        highlighter.HighlightLastMove(fromX, fromY, toX, toY);

        if(promotion == 'q')
        {
            Destroy(aiPiece);
            pieceManager.CreatePiece("black_queen", toX, toY);
            pieceManager.SetPosition(toX, toY, pieceManager.GetPosition(toX, toY));
        }

        // CRITICAL FIX: The "Trust but Verify" Check
        // If the AI claimed it was going to mate us, we now check the ACTUAL board state.
        if (aiClaimedMate)
        {
            if (stockfishManager.debugMode) Debug.Log("[GameManager] Verifying Checkmate...");
            VerifyPlayerSurvival();
        }
        else
        {
            StartPlayerTurn();
        }
    }

    // NEW: Generates FEN for WHITE and asks Stockfish "Can I move?"
    void VerifyPlayerSurvival()
    {
        // 1. Generate FEN for White (Player)
        string fen = boardConverter.BoardToFEN(pieceManager.GetAllPieces(), "white");
        
        // 2. Ask Stockfish for the best move for White
        stockfishManager.GetBestMove(fen, OnPlayerStatusVerified);
    }

    void OnPlayerStatusVerified(StockfishResult result)
    {
        // 3. Analyze Result
        // If Stockfish says "bestmove (none)" OR "mate 0", it confirms we are truly stuck.
        bool isMated = (result.bestmove == "(none)" || result.bestmove == "none");
        
        if (!isMated && result.mate != null && result.mate == 0) 
            isMated = true;

        if (isMated)
        {
            if (stockfishManager.debugMode) Debug.Log("[GameManager] Verification Complete: DEFEAT Confirmed.");
            Winner("black");
        }
        else
        {
            if (stockfishManager.debugMode) Debug.Log("[GameManager] Verification Complete: AI Blundered! Game Continues.");
            StartPlayerTurn();
        }
    }

    // --- GAME FLOW ---

    void StartSetupPhase() {
        currentPhase = GamePhase.SetupDraw;
        uiManager.SetTurnText("Setup Phase");
        deckManager.DrawCard("white", 5); deckManager.DrawCard("black", 5);
        if(audioManager) audioManager.PlayDeckDeal(); 
        handVisuals.RefreshPlayerHand(deckManager.whiteHand);
        StartCoroutine(AnimateSetupPhase());
    }

    IEnumerator AnimateSetupPhase()
    {
        uiManager.SetPhaseText("Opponent Deploying...");
        for (int i = 0; i < 5; i++)
        {
            Vector3? deploySpot = GetValidAIDeploySpot();
            if (deploySpot.HasValue && deckManager.blackHand.Count > 0)
            {
                Card cardToDeploy = deckManager.blackHand[0];
                Vector3 startPos = deckManager.GetTopAICardPosition();
                deckManager.RemoveCardFromHand("black", 0);
                
                yield return StartCoroutine(handVisuals.AnimateAIDeploy(startPos, deploySpot.Value, cardToDeploy.pieceType));
                
                if(audioManager) audioManager.PlayPieceImpact();

                int x = (int)deploySpot.Value.x;
                int y = (int)deploySpot.Value.y;
                pieceManager.CreatePiece("black_" + cardToDeploy.pieceType, x, y);
                
                highlighter.HighlightEnemyDeploy(x, y);
                yield return new WaitForSeconds(0.2f);
            }
        }
        currentPhase = GamePhase.SetupDeploy;
        uiManager.SetPhaseText("Deploy Your Hand!");
    }

    void StartPlayerTurn()
    {
        currentPlayer = "white";
        deckManager.DrawCard("white", 1);
        if(audioManager) audioManager.PlayDeckDeal(); 
        handVisuals.RefreshPlayerHand(deckManager.whiteHand);
        
        if (deckManager.whiteHand.Count > 0)
        {
            currentPhase = GamePhase.PlayerTurnDeploy;
            uiManager.SetPhaseText("Deploy 1 Card");
        }
        else
        {
            currentPhase = GamePhase.PlayerTurnMove;
            uiManager.SetPhaseText("Make Your Move");
        }
        uiManager.SetTurnText("Turn: White");
    }

    void StartGameLoop() {
        StartPlayerTurn(); 
    }

    void StartAITurn()
    {
        currentPlayer = "black";
        currentPhase = GamePhase.AITurn;
        uiManager.SetTurnText("Turn: Black");
        uiManager.SetPhaseText("AI Thinking...");
        StartCoroutine(AITurnSequence());
    }

    IEnumerator AITurnSequence()
    {
        deckManager.DrawCard("black", 1);
        if(audioManager) audioManager.PlayDeckDeal(); 

        yield return new WaitForSeconds(0.5f);
        uiManager.SetPhaseText("AI Deploying...");
        
        highlighter.ClearLastDeploy();
        
        Vector3? deploySpot = GetValidAIDeploySpot();
        if (deploySpot.HasValue && deckManager.blackHand.Count > 0)
        {
            Card cardToDeploy = deckManager.blackHand[0];
            Vector3 startPos = deckManager.GetTopAICardPosition();
            deckManager.RemoveCardFromHand("black", 0); 
            
            yield return StartCoroutine(handVisuals.AnimateAIDeploy(startPos, deploySpot.Value, cardToDeploy.pieceType));
            if(audioManager) audioManager.PlayPieceImpact(); 

            int x = (int)deploySpot.Value.x;
            int y = (int)deploySpot.Value.y;
            pieceManager.CreatePiece("black_" + cardToDeploy.pieceType, x, y);
            highlighter.HighlightEnemyDeploy(x, y);
            yield return new WaitForSeconds(0.5f);
        }

        uiManager.SetPhaseText("AI Moving...");
        TriggerStockfish();
    }

    // --- LOGIC HELPERS ---

    public bool TryDeployCard(Card card, int handIndex, Vector3 dropPosition)
    {
        if (currentPhase != GamePhase.SetupDeploy && currentPhase != GamePhase.PlayerTurnDeploy) return false;
        if (currentPlayer != "white") return false;

        int x = Mathf.RoundToInt(dropPosition.x);
        int y = Mathf.RoundToInt(dropPosition.y);

        if (CanPlacePiece(x, y, "white", card.pieceType))
        {
            pieceManager.CreatePiece("white_" + card.pieceType, x, y);
            deckManager.whiteHand.RemoveAt(handIndex);
            
            handVisuals.RefreshPlayerHand(deckManager.whiteHand);
            highlighter.ClearMoveHints();
            highlighter.CreateHighlight(x, y, new Color(0f, 1f, 0f, 0.4f), BoardHighlighter.HighlightType.FullSquare, false);

            if(audioManager) audioManager.PlayPieceImpact(); 

            if (currentPhase == GamePhase.SetupDeploy)
            {
                if (deckManager.whiteHand.Count == 0) StartGameLoop();
            }
            else if (currentPhase == GamePhase.PlayerTurnDeploy)
            {
                currentPhase = GamePhase.PlayerTurnMove;
                uiManager.SetPhaseText("Make Your Move");
            }
            return true; 
        }
        return false; 
    }

    // ... (GenerateBoard, SpawnKings, HandlePlayerMoveInput, CanPlacePiece, GetValidAIDeploySpot remain unchanged) ...
    // Note: I have hidden them to keep this snippet clean, but you should keep the logic from your previous file.
    
    void GenerateBoard() {
        for (int x = 0; x < 8; x++) {
            for (int y = 0; y < 8; y++) {
                GameObject newTile = Instantiate(tilePrefab, new Vector3(x, y, 0), Quaternion.identity);
                newTile.name = $"Tile {x},{y}";
                if (useCustomBoardArt && newTile.GetComponent<SpriteRenderer>()) 
                    newTile.GetComponent<SpriteRenderer>().enabled = false;
                else if ((x + y) % 2 != 0) 
                    newTile.GetComponent<SpriteRenderer>().color = new Color(0.6f, 0.6f, 0.6f);
                pieceManager.RegisterTile(x, y, newTile);
            }
        }
    }

    void SpawnKingsAndGuards() {
        pieceManager.CreatePiece("white_king", 4, 0);
        pieceManager.CreatePiece("black_king", 4, 7);
        pieceManager.CreatePiece("black_pawn", 3, 5);
        pieceManager.CreatePiece("black_pawn", 4, 6);
        pieceManager.CreatePiece("black_pawn", 5, 5);
    }

    Vector3? GetValidAIDeploySpot() {
        if (deckManager.blackHand.Count == 0) return null;
        Card card = deckManager.blackHand[0];
        for (int i = 0; i < 50; i++) {
            int x = Random.Range(0, 8); int y = Random.Range(5, 8);
            if (CanPlacePiece(x, y, "black", card.pieceType)) return new Vector3(x, y, 0);
        }
        return null;
    }

    public bool CanPlacePiece(int x, int y, string color, string type) {
        if (!pieceManager.PositionOnBoard(x, y)) return false;
        if (pieceManager.GetPosition(x, y) != null) return false;
        if (color == "white" && y > 2) return false;
        if (color == "black" && y < 5) return false;
        if (type == "pawn" && (y == 0 || y == 7)) return false;
        return true;
    }

    void HandlePlayerMoveInput() {
        if (Input.GetMouseButtonDown(0)) {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);
            if (hit.collider != null && hit.collider.GetComponent<ChessPiece>() != null) {
                if (hit.collider.GetComponent<ChessPiece>().player == currentPlayer) {
                    hit.collider.GetComponent<ChessPiece>().InitiateMovePlates();
                }
            }
        }
    }

    void TriggerStockfish() {
        string fen = boardConverter.BoardToFEN(pieceManager.GetAllPieces(), currentPlayer);
        stockfishManager.GetBestMove(fen, OnAIMoveReceived);
    }

    void OnAIMoveReceived(StockfishResult result) {
        if (!result.success || result.bestmove == "(none)" || result.bestmove == "none") { Winner("white"); return; }
        
        string uciMove = result.bestmove;
        
        // CHECK POTENTIAL MATE: 
        // If Stockfish says "Mate in X" (positive value), it thinks it is winning.
        // We will pass this to the mover to verify AFTER the move is made.
        bool aiClaimsMate = (result.mate != null && result.mate > 0);

        if (boardConverter.ParseUCIMove(uciMove, out int fromX, out int fromY, out int toX, out int toY, out char promotion)) {
            ExecuteAIMove(fromX, fromY, toX, toY, promotion, aiClaimsMate);
        }
    }

    public void RestartGame() { SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
    public void QuitGame() { Application.Quit(); }
    public void Winner(string playerWinner) { uiManager.ShowVictory(playerWinner); }
    public bool IsPlayerTurn() { return (currentPlayer == "white" && currentPhase == GamePhase.PlayerTurnMove); }
}