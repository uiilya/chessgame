using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections; 
using System.Collections.Generic;

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

    // Stores who won so the Next button knows what to do
    private string winningPlayer = "";

    private int pointsEarnedThisMatch = 0;
    
    private Dictionary<string, int> pieceValues = new Dictionary<string, int>() {
        { "pawn", 1 },
        { "knight", 3 },
        { "bishop", 3 },
        { "rook", 4 },
        { "queen", 5 },
        { "king", 5 }
    };

    void Start()
    {
        if(!pieceManager) pieceManager = GetComponent<PieceManager>();
        if(!deckManager) deckManager = GetComponent<DeckManager>();
        if(!highlighter) highlighter = GetComponent<BoardHighlighter>();
        if(!audioManager) audioManager = GetComponent<AudioManager>();
        if(!uiManager) uiManager = GetComponent<UIManager>();
        if(!handVisuals) handVisuals = GetComponent<HandVisualsManager>();

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

    public void ClearMoveHints() => highlighter.ClearMoveHints();
    public GameObject CreateHighlight(int x, int y, Color c, BoardHighlighter.HighlightType t, bool h) 
        => highlighter.CreateHighlight(x, y, c, t, h);
    public bool PositionOnBoard(int x, int y) => pieceManager.PositionOnBoard(x, y);
    public GameObject GetPosition(int x, int y) => pieceManager.GetPosition(x, y);
    
    public void CapturePiece(GameObject piece) 
    {
        if (piece == null) return;

        ChessPiece cp = piece.GetComponent<ChessPiece>();
        
        // Points Calculation
        if (cp != null && cp.player != "white") 
        {
            foreach(var kvp in pieceValues)
            {
                if (piece.name.ToLower().Contains(kvp.Key))
                {
                    pointsEarnedThisMatch += kvp.Value;
                    Debug.Log($"Captured {kvp.Key}! Points: {pointsEarnedThisMatch}");
                    break;
                }
            }
        }

        if(audioManager) audioManager.PlayPieceImpact();
        pieceManager.CapturePiece(piece);
    }
    
    public void PlayMoveSound()
    {
        if(audioManager) audioManager.PlayPieceMove();
    }

    public void CheckPawnPromotion(GameObject piece, int x, int y)
    {
        if (piece == null) return;
        
        ChessPiece cp = piece.GetComponent<ChessPiece>();
        if (piece.name.ToLower().Contains("pawn"))
        {
            if ((cp.player == "white" && y == 7) || (cp.player == "black" && y == 0))
            {
                if(audioManager) audioManager.PlayPieceImpact(); 

                string color = cp.player;
                Destroy(piece);
                
                string newPieceName = color + "_queen";
                pieceManager.CreatePiece(newPieceName, x, y);
                
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
        bool captured = false;

        // 1. Handle Capture
        if (target != null) 
        {
            CapturePiece(target); 
            captured = true;
        }

        // 2. Perform Visual Move
        yield return StartCoroutine(pieceManager.MovePieceLogic(piece, x, y));

        if(!captured && audioManager) {audioManager.PlayPieceImpactOne();}
        
        CheckPawnPromotion(piece, x, y);
        highlighter.HighlightLastMove(startX, startY, x, y);
        highlighter.ClearMoveHints(); 

        // 3. Check for Winner (Multiple Kings Logic)
        if (CheckWinCondition())
        {
            // Game Over handled inside CheckWinCondition
        }
        else
        {
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

        // 1. Handle Capture
        if (target != null) 
        {
             CapturePiece(target);
        }
        else if(audioManager) 
        {
            audioManager.PlayPieceMove();
        }

        // 2. Perform Visual Move
        yield return StartCoroutine(pieceManager.MovePieceLogic(aiPiece, toX, toY));
        highlighter.HighlightLastMove(fromX, fromY, toX, toY);
        CheckPawnPromotion(aiPiece, toX, toY);

        // 3. Check for Winner
        if (CheckWinCondition())
        {
            // Game Over handled inside
        }
        else
        {
            StartPlayerTurn();
        }
    }

    bool CheckWinCondition()
    {
        GameObject[,] allPieces = pieceManager.GetAllPieces();
        bool whiteHasKing = false;
        bool blackHasKing = false;

        foreach(GameObject obj in allPieces)
        {
            if (obj != null)
            {
                if (obj.name.Contains("white_king")) whiteHasKing = true;
                if (obj.name.Contains("black_king")) blackHasKing = true;
            }
        }

        if (!whiteHasKing)
        {
            Winner("black");
            return true;
        }
        if (!blackHasKing)
        {
            Winner("white");
            return true;
        }

        return false;
    }

    void StartSetupPhase() {
        currentPhase = GamePhase.SetupDraw;
        uiManager.SetTurnText("Setup Phase");
        
        deckManager.DrawSpecificCard("white", "king"); 
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
        if (!result.success || result.bestmove == "(none)" || result.bestmove == "none") { 
            if (CheckWinCondition()) return;
            Winner("white"); 
            return; 
        }
        
        string uciMove = result.bestmove;
        bool aiClaimsMate = (result.mate != null && result.mate > 0);

        if (boardConverter.ParseUCIMove(uciMove, out int fromX, out int fromY, out int toX, out int toY, out char promotion)) {
            ExecuteAIMove(fromX, fromY, toX, toY, promotion, aiClaimsMate);
        }
    }

    // --- RESTART / QUIT / NEXT LEVEL ---

    public void RestartGame() { SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
    public void QuitGame() { Application.Quit(); }
    public void ReturnToMenu() { SceneManager.LoadScene("Main Menu"); }

    public void Winner(string playerWinner) 
    { 
        gameOver = true;
        winningPlayer = playerWinner; // Store winner to decide what buttons do

        string message = "DEFEAT";
        
        if (playerWinner == "white")
        {
            message = "VICTORY!";
            // Check if this is the final level
            if (ProgressionManager.Instance != null && 
                ProgressionManager.Instance.currentLevel >= ProgressionManager.Instance.maxLevels)
            {
                message = "CONGRATULATIONS! RUN COMPLETE!";
            }
        }
        
        uiManager.ShowVictory(message); 
        
        // Note: Automatic scene transition is removed. 
        // Game now waits for user to click "Next" (OnNextLevelButton) or "Exit".
    }

    // LINK THIS TO YOUR "NEXT" BUTTON
    public void OnNextLevelButton()
    {
        if (winningPlayer == "white")
        {
            // Player won: Proceed with points
            int total = pointsEarnedThisMatch + 5;
            if (ProgressionManager.Instance)
            {
                // This handles moving to DeckBuilder OR Main Menu if it was the final level
                ProgressionManager.Instance.CompleteLevel(total);
            }
            else
            {
                // Fallback for testing without ProgressionManager
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }
        else
        {
            // Player lost: Return to Main Menu
            SceneManager.LoadScene("Main Menu");
        }
    }

    public bool IsPlayerTurn() { return (currentPlayer == "white" && currentPhase == GamePhase.PlayerTurnMove); }
}