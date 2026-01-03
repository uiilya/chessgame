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

        // Link dependencies
        if(handVisuals.audioManager == null) handVisuals.audioManager = audioManager;
        if(handVisuals.deckManager == null) handVisuals.deckManager = deckManager;

        boardConverter = new BoardStateConverter();
        
        GenerateBoard();
        SpawnKingsAndGuards();

        if (autoConfigureCamera)
        {
            Camera.main.transform.position = new Vector3(3.5f, 2.5f, -10);
            Camera.main.orthographicSize = 7.0f; 
        }

        deckManager.InitializeDecks();
        handVisuals.InitializeDeckVisuals(deckManager.whiteDeck.Count, deckManager.blackDeck.Count);

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
    
    // UPDATED: CapturePiece simply handles destruction/audio now.
    // It returns TRUE if the piece captured was a King.
    public bool CapturePiece(GameObject piece) 
    {
        if (piece == null) return false;

        bool isKing = piece.name.ToLower().Contains("king");
        
        if(audioManager) audioManager.PlayPieceImpact();
        pieceManager.CapturePiece(piece);

        return isKing;
    }
    
    public void PlayMoveSound()
    {
        if(audioManager) audioManager.PlayPieceMove();
    }

    // NEW: Handles automatic promotion to Queen for both Player and AI
    public void CheckPawnPromotion(GameObject piece, int x, int y)
    {
        if (piece == null) return;
        
        ChessPiece cp = piece.GetComponent<ChessPiece>();
        // Ensure we only promote pawns
        if (piece.name.ToLower().Contains("pawn"))
        {
            // White Promotes at Y=7, Black at Y=0
            if ((cp.player == "white" && y == 7) || (cp.player == "black" && y == 0))
            {
                if(audioManager) audioManager.PlayPieceImpact(); // Optional: Sound for promotion

                string color = cp.player;
                Destroy(piece);
                
                // Create Queen
                string newPieceName = color + "_queen";
                pieceManager.CreatePiece(newPieceName, x, y);
                
                // Safety: Ensure the piece manager registers the new piece at this location
                // (Mirroring the logic previously used in AIMoveSequence)
                GameObject newPiece = pieceManager.GetPosition(x, y);
                pieceManager.SetPosition(x, y, newPiece);
            }
        }
    }

    public void MovePiece(GameObject piece, int x, int y)
    {
        if (gameOver) return;
        StartCoroutine(MoveSequence(piece, x, y));
    }

    IEnumerator MoveSequence(GameObject piece, int x, int y)
    {
        ChessPiece cp = piece.GetComponent<ChessPiece>();
        int startX = cp.GetXBoard();
        int startY = cp.GetYBoard();

        GameObject target = pieceManager.GetPosition(x, y);
        bool kingCaptured = false;
        string winner = "";

        // 1. Handle Capture
        if (target != null) 
        {
            ChessPiece targetCp = target.GetComponent<ChessPiece>();
            // Check win condition BEFORE destroying the object
            if (target.name.ToLower().Contains("king"))
            {
                kingCaptured = true;
                // If target is white, black wins. If target is black, white wins.
                winner = (targetCp != null && targetCp.player == "white") ? "black" : "white";
            }
            CapturePiece(target); 
        }

        // 2. Perform Visual Move (ALWAYS run this, even if game is over)
        yield return StartCoroutine(pieceManager.MovePieceLogic(piece, x, y));
        
        // NEW: Check for Promotion
        CheckPawnPromotion(piece, x, y);

        highlighter.HighlightLastMove(startX, startY, x, y);
        highlighter.ClearMoveHints(); 

        // 3. Check Game Over AFTER visuals are done
        if (kingCaptured)
        {
            // Small delay so the player sees the piece land on the king
            yield return new WaitForSeconds(0.5f);
            Winner(winner);
        }
        else
        {
            // Continue game
            if (currentPlayer == "white") StartAITurn();
            else StartPlayerTurn();
        }
    }

    void ExecuteAIMove(int fromX, int fromY, int toX, int toY, char promotion, bool aiClaimedMate)
    {
        if (gameOver) return;
        StartCoroutine(AIMoveSequence(fromX, fromY, toX, toY, promotion, aiClaimedMate));
    }

    IEnumerator AIMoveSequence(int fromX, int fromY, int toX, int toY, char promotion, bool aiClaimedMate)
    {
        GameObject aiPiece = pieceManager.GetPosition(fromX, fromY);
        GameObject target = pieceManager.GetPosition(toX, toY);
        bool kingCaptured = false;
        string winner = "";

        // 1. Handle Capture
        if (target != null) 
        {
             ChessPiece targetCp = target.GetComponent<ChessPiece>();
             if (target.name.ToLower().Contains("king"))
             {
                 kingCaptured = true;
                 winner = (targetCp != null && targetCp.player == "white") ? "black" : "white";
             }
             CapturePiece(target);
        }
        else if(audioManager) 
        {
            audioManager.PlayPieceMove();
        }

        // 2. Perform Visual Move
        yield return StartCoroutine(pieceManager.MovePieceLogic(aiPiece, toX, toY));
        
        highlighter.HighlightLastMove(fromX, fromY, toX, toY);

        // NEW: Use the generic promotion check instead of the promotion char check.
        // This ensures the visuals always match the "Auto Queen" rule.
        CheckPawnPromotion(aiPiece, toX, toY);

        // 3. Check Game Over
        if (kingCaptured)
        {
            yield return new WaitForSeconds(0.5f);
            Winner(winner);
        }
        else
        {
            StartPlayerTurn();
        }
    }

    // --- GAME FLOW ---

    void StartSetupPhase() {
        currentPhase = GamePhase.SetupDraw;
        uiManager.SetTurnText("Setup Phase");
        
        deckManager.DrawCard("white", 5); 
        deckManager.DrawCard("black", 5);
        
        StartCoroutine(AnimateSetupPhase());
    }

    IEnumerator AnimateSetupPhase()
    {
        uiManager.SetPhaseText("Drawing Hand...");
        
        int whiteCount = deckManager.whiteHand.Count;
        if (whiteCount > 0)
            yield return StartCoroutine(handVisuals.AnimateDraw(deckManager.whiteHand, whiteCount));
        
        int blackCount = deckManager.blackHand.Count;
        if (blackCount > 0)
            yield return StartCoroutine(handVisuals.AnimateAIDraw(deckManager.blackHand.Count, blackCount));

        uiManager.SetPhaseText("Opponent Deploying...");
        for (int i = 0; i < 5; i++)
        {
            Vector3? deploySpot = GetValidAIDeploySpot();
            if (deploySpot.HasValue && deckManager.blackHand.Count > 0)
            {
                Card cardToDeploy = deckManager.blackHand[0];
                Vector3 startPos = handVisuals.GetAIHandPosition(0);
                handVisuals.RemoveAIHandCard(0);
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
        if (gameOver) return;
        StartCoroutine(PlayerTurnSequence());
    }

    IEnumerator PlayerTurnSequence()
    {
        currentPlayer = "white";
        
        int countBefore = deckManager.whiteHand.Count;
        deckManager.DrawCard("white", 1);
        int cardsDrawn = deckManager.whiteHand.Count - countBefore;
        
        uiManager.SetPhaseText("Drawing...");
        
        if (cardsDrawn > 0)
            yield return StartCoroutine(handVisuals.AnimateDraw(deckManager.whiteHand, cardsDrawn));

        if (deckManager.whiteHand.Count > 0)
        {
            currentPhase = GamePhase.PlayerTurnDeploy;
            uiManager.SetPhaseText("Drag and drop to deploy 1 card!");
        }
        else
        {
            currentPhase = GamePhase.PlayerTurnMove;
            uiManager.SetPhaseText("No cards left! Move your pieces.");
        }
        uiManager.SetTurnText("Turn: White");
    }

    void StartGameLoop() {
        StartPlayerTurn(); 
    }

    void StartAITurn()
    {
        if (gameOver) return;
        currentPlayer = "black";
        currentPhase = GamePhase.AITurn;
        uiManager.SetTurnText("Turn: Black");
        uiManager.SetPhaseText("AI Thinking...");
        StartCoroutine(AITurnSequence());
    }

    IEnumerator AITurnSequence()
    {
        int countBefore = deckManager.blackHand.Count;
        deckManager.DrawCard("black", 1);
        int cardsDrawn = deckManager.blackHand.Count - countBefore;
        
        if (cardsDrawn > 0)
            yield return StartCoroutine(handVisuals.AnimateAIDraw(deckManager.blackHand.Count, cardsDrawn));

        yield return new WaitForSeconds(0.3f);
        uiManager.SetPhaseText("AI Deploying...");
        
        highlighter.ClearLastDeploy();
        
        Vector3? deploySpot = GetValidAIDeploySpot();
        if (deploySpot.HasValue && deckManager.blackHand.Count > 0)
        {
            Card cardToDeploy = deckManager.blackHand[0];
            Vector3 startPos = handVisuals.GetAIHandPosition(0);
            handVisuals.RemoveAIHandCard(0);
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
                uiManager.SetPhaseText("Move a piece on the board.");
            }
            return true; 
        }
        return false; 
    }

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
        bool aiClaimsMate = (result.mate != null && result.mate > 0);

        if (boardConverter.ParseUCIMove(uciMove, out int fromX, out int fromY, out int toX, out int toY, out char promotion)) {
            ExecuteAIMove(fromX, fromY, toX, toY, promotion, aiClaimsMate);
        }
    }

    public void RestartGame() { SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
    public void QuitGame() { Application.Quit(); }
    public void Winner(string playerWinner) { 
        gameOver = true;
        uiManager.ShowVictory(playerWinner); 
    }
    public bool IsPlayerTurn() { return (currentPlayer == "white" && currentPhase == GamePhase.PlayerTurnMove); }
}