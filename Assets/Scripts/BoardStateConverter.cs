using UnityEngine;

public class BoardStateConverter
{
    // Track game state information needed for FEN
    public bool whiteCanCastleKingside = true;
    public bool whiteCanCastleQueenside = true;
    public bool blackCanCastleKingside = true;
    public bool blackCanCastleQueenside = true;
    public string enPassantSquare = "-";
    public int halfmoveClock = 0;
    public int fullmoveNumber = 1;

    /// <summary>
    /// Converts the Unity board state to FEN (Forsyth-Edwards Notation)
    /// </summary>
    public string BoardToFEN(GameObject[,] positions, string currentPlayer)
    {
        string piecePlacement = GeneratePiecePlacement(positions);
        string activeColor = currentPlayer == "white" ? "w" : "b";
        string castlingRights = GenerateCastlingRights();
        
        // FEN format: piece_placement activeColor castling enPassant halfmove fullmove
        return $"{piecePlacement} {activeColor} {castlingRights} {enPassantSquare} {halfmoveClock} {fullmoveNumber}";
    }

    string GeneratePiecePlacement(GameObject[,] positions)
    {
        string fen = "";
        
        // FEN starts from rank 8 (top) to rank 1 (bottom)
        // Our board: positions[x,y] where y=7 is top
        for (int y = 7; y >= 0; y--)
        {
            int emptyCount = 0;
            
            for (int x = 0; x < 8; x++)
            {
                GameObject piece = positions[x, y];
                
                if (piece == null)
                {
                    emptyCount++;
                }
                else
                {
                    // Add empty squares count if any
                    if (emptyCount > 0)
                    {
                        fen += emptyCount.ToString();
                        emptyCount = 0;
                    }
                    
                    // Add piece character
                    fen += GetFENCharForPiece(piece);
                }
            }
            
            // Add remaining empty squares
            if (emptyCount > 0)
            {
                fen += emptyCount.ToString();
            }
            
            // Add rank separator (except for last rank)
            if (y > 0)
            {
                fen += "/";
            }
        }
        
        return fen;
    }

    char GetFENCharForPiece(GameObject piece)
    {
        string name = piece.name.ToLower();
        bool isWhite = name.Contains("white");
        
        char pieceChar = ' ';
        
        if (name.Contains("rook")) pieceChar = 'r';
        else if (name.Contains("knight")) pieceChar = 'n';
        else if (name.Contains("bishop")) pieceChar = 'b';
        else if (name.Contains("queen")) pieceChar = 'q';
        else if (name.Contains("king")) pieceChar = 'k';
        else if (name.Contains("pawn")) pieceChar = 'p';
        
        // White pieces are uppercase in FEN
        if (isWhite)
        {
            pieceChar = char.ToUpper(pieceChar);
        }
        
        return pieceChar;
    }

    string GenerateCastlingRights()
    {
        string rights = "";
        
        if (whiteCanCastleKingside) rights += "K";
        if (whiteCanCastleQueenside) rights += "Q";
        if (blackCanCastleKingside) rights += "k";
        if (blackCanCastleQueenside) rights += "q";
        
        return string.IsNullOrEmpty(rights) ? "-" : rights;
    }

    /// <summary>
    /// Parses UCI move notation (e.g., "e2e4", "e7e8q") to board coordinates
    /// Returns: (fromX, fromY, toX, toY, promotionPiece)
    /// </summary>
    public bool ParseUCIMove(string uciMove, out int fromX, out int fromY, out int toX, out int toY, out char promotion)
    {
        fromX = fromY = toX = toY = -1;
        promotion = ' ';
        
        if (string.IsNullOrEmpty(uciMove) || uciMove.Length < 4)
        {
            return false;
        }
        
        // Parse source square (e.g., "e2")
        fromX = uciMove[0] - 'a'; // 'a' = 0, 'b' = 1, etc.
        fromY = uciMove[1] - '1'; // '1' = 0, '2' = 1, etc.
        
        // Parse destination square (e.g., "e4")
        toX = uciMove[2] - 'a';
        toY = uciMove[3] - '1';
        
        // Check for promotion (5th character: q, r, b, n)
        if (uciMove.Length >= 5)
        {
            promotion = uciMove[4];
        }
        
        // Validate coordinates
        if (fromX < 0 || fromX > 7 || fromY < 0 || fromY > 7 ||
            toX < 0 || toX > 7 || toY < 0 || toY > 7)
        {
            return false;
        }
        
        return true;
    }

    /// <summary>
    /// Converts board coordinates to UCI notation
    /// </summary>
    public string CoordinatesToUCI(int fromX, int fromY, int toX, int toY, char promotion = ' ')
    {
        char fromFile = (char)('a' + fromX);
        char fromRank = (char)('1' + fromY);
        char toFile = (char)('a' + toX);
        char toRank = (char)('1' + toY);
        
        string uci = $"{fromFile}{fromRank}{toFile}{toRank}";
        
        if (promotion != ' ')
        {
            uci += promotion;
        }
        
        return uci;
    }

    /// <summary>
    /// Updates castling rights based on piece movement
    /// </summary>
    public void UpdateCastlingRights(GameObject piece, int fromX, int fromY)
    {
        if (piece == null) return;
        
        string name = piece.name.ToLower();
        
        // King moved - lose all castling rights for that color
        if (name.Contains("king"))
        {
            if (name.Contains("white"))
            {
                whiteCanCastleKingside = false;
                whiteCanCastleQueenside = false;
            }
            else
            {
                blackCanCastleKingside = false;
                blackCanCastleQueenside = false;
            }
        }
        // Rook moved - lose castling right for that side
        else if (name.Contains("rook"))
        {
            if (name.Contains("white"))
            {
                if (fromX == 0 && fromY == 0) whiteCanCastleQueenside = false; // a1
                if (fromX == 7 && fromY == 0) whiteCanCastleKingside = false;  // h1
            }
            else
            {
                if (fromX == 0 && fromY == 7) blackCanCastleQueenside = false; // a8
                if (fromX == 7 && fromY == 7) blackCanCastleKingside = false;  // h8
            }
        }
    }

    /// <summary>
    /// Sets en passant target square after a pawn double move
    /// </summary>
    public void SetEnPassantSquare(int fromY, int toY, int x)
    {
        // Check if pawn moved two squares
        if (Mathf.Abs(toY - fromY) == 2)
        {
            int enPassantY = (fromY + toY) / 2;
            char file = (char)('a' + x);
            char rank = (char)('1' + enPassantY);
            enPassantSquare = $"{file}{rank}";
        }
        else
        {
            enPassantSquare = "-";
        }
    }

    /// <summary>
    /// Resets en passant square (call at start of each turn)
    /// </summary>
    public void ClearEnPassant()
    {
        enPassantSquare = "-";
    }

    /// <summary>
    /// Increments move counters
    /// </summary>
    public void IncrementMoveCounters(bool isPawnMoveOrCapture, bool isBlackMove)
    {
        if (isPawnMoveOrCapture)
        {
            halfmoveClock = 0;
        }
        else
        {
            halfmoveClock++;
        }
        
        if (isBlackMove)
        {
            fullmoveNumber++;
        }
    }
}
