using UnityEngine;

public class BoardStateConverter
{
    // CHANGE: Default to FALSE (No castling) for custom setups to prevent "Illegal FEN" errors
    public bool whiteCanCastleKingside = false;
    public bool whiteCanCastleQueenside = false;
    public bool blackCanCastleKingside = false;
    public bool blackCanCastleQueenside = false;
    
    public string enPassantSquare = "-";
    public int halfmoveClock = 0;
    public int fullmoveNumber = 1;

    public string BoardToFEN(GameObject[,] positions, string currentPlayer)
    {
        string piecePlacement = GeneratePiecePlacement(positions);
        string activeColor = currentPlayer == "white" ? "w" : "b";
        
        // This will now generate "-" by default unless you explicitly set flags to true later
        string castlingRights = GenerateCastlingRights(); 
        
        // SAFEGUARDS (From previous fix):
        if (halfmoveClock < 0) halfmoveClock = 0;
        if (fullmoveNumber < 1) fullmoveNumber = 1;

        return $"{piecePlacement} {activeColor} {castlingRights} {enPassantSquare} {halfmoveClock} {fullmoveNumber}";
    }

    string GeneratePiecePlacement(GameObject[,] positions)
    {
        string fen = "";
        for (int y = 7; y >= 0; y--)
        {
            int emptyCount = 0;
            for (int x = 0; x < 8; x++)
            {
                GameObject piece = positions[x, y];
                if (piece == null) emptyCount++;
                else
                {
                    if (emptyCount > 0) { fen += emptyCount.ToString(); emptyCount = 0; }
                    fen += GetFENCharForPiece(piece);
                }
            }
            if (emptyCount > 0) fen += emptyCount.ToString();
            if (y > 0) fen += "/";
        }
        return fen;
    }

    char GetFENCharForPiece(GameObject piece)
    {
        string name = piece.name.ToLower();
        char p = ' ';
        if (name.Contains("rook")) p = 'r';
        else if (name.Contains("knight")) p = 'n';
        else if (name.Contains("bishop")) p = 'b';
        else if (name.Contains("queen")) p = 'q';
        else if (name.Contains("king")) p = 'k';
        else if (name.Contains("pawn")) p = 'p';
        return name.Contains("white") ? char.ToUpper(p) : p;
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

    public bool ParseUCIMove(string uciMove, out int fromX, out int fromY, out int toX, out int toY, out char promotion)
    {
        fromX = fromY = toX = toY = -1;
        promotion = ' ';
        if (string.IsNullOrEmpty(uciMove) || uciMove.Length < 4) return false;

        fromX = uciMove[0] - 'a';
        fromY = uciMove[1] - '1';
        toX = uciMove[2] - 'a';
        toY = uciMove[3] - '1';
        if (uciMove.Length >= 5) promotion = uciMove[4];
        return true;
    }

    public void UpdateCastlingRights(GameObject piece, int fromX, int fromY)
    {
        // For your custom game, you can likely ignore updating these since we disabled them by default.
        // But keeping the logic here doesn't hurt, it just won't turn them ON if they start OFF.
        
        if (piece == null) return;
        string name = piece.name.ToLower();

        if (name.Contains("king"))
        {
            if (name.Contains("white")) { whiteCanCastleKingside = false; whiteCanCastleQueenside = false; }
            else { blackCanCastleKingside = false; blackCanCastleQueenside = false; }
        }
        else if (name.Contains("rook"))
        {
            if (name.Contains("white"))
            {
                if (fromX == 0 && fromY == 0) whiteCanCastleQueenside = false;
                if (fromX == 7 && fromY == 0) whiteCanCastleKingside = false;
            }
            else
            {
                if (fromX == 0 && fromY == 7) blackCanCastleQueenside = false;
                if (fromX == 7 && fromY == 7) blackCanCastleKingside = false;
            }
        }
    }
}