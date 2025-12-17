# Stockfish AI Integration - Setup Guide

## Implementation Complete! ✓

The following components have been added to your Unity chess game:

### New Files Created:

- **StockfishManager.cs** - Manages Stockfish process and UCI communication
- **BoardStateConverter.cs** - Converts between your board and FEN/UCI notation
- **GameManager.cs** - Updated with turn management and AI triggering
- **ChessPiece.cs** - Updated with pawn double-move support

---

## Setup Steps

### 1. Download Stockfish

1. Visit: https://stockfishchess.org/download/
2. Download **Stockfish for Windows** (latest version)
3. Extract the downloaded zip file
4. Find the `stockfish.exe` file (or `stockfish-windows-x86-64-avx2.exe`)

### 2. Place Stockfish in Your Project

**Option A (Recommended):** Place in game root directory

- Copy `stockfish.exe` to: `d:\ProjectGameDev\chessgame\stockfish.exe`

**Option B:** Place in StreamingAssets

- Create folder: `d:\ProjectGameDev\chessgame\Assets\StreamingAssets\`
- Copy `stockfish.exe` there

### 3. Configure Unity Scene

1. **Open your game scene in Unity**

2. **Create StockfishManager GameObject:**

   - Right-click in Hierarchy → Create Empty
   - Name it: `StockfishManager`
   - Add Component → `StockfishManager` script
   - Configure settings in Inspector:
     - **Stockfish Path**: `stockfish.exe` (if in root) or leave default
     - **Skill Level**: 5 (adjust 0-20, where 20 is strongest)
     - **Thinking Time**: 1000 ms (1 second)

3. **Link to GameManager:**
   - Select your `Game Controller` GameObject in Hierarchy
   - Find the `GameManager` script component in Inspector
   - Drag the `StockfishManager` GameObject into the **Stockfish Manager** field

### 4. Test the Integration

1. **Press Play** in Unity
2. Make a move as **white** (white pieces are at bottom, y=0-1)
3. After your move, Stockfish should automatically move **black** pieces
4. Check the Console for debug messages:
   - "Stockfish initialized successfully..."
   - "Turn: black"
   - "Stockfish suggests: e7e5" (example)
   - "AI wants to move: e7e5"

---

## How It Works

### Turn System

- **White = Player** (you control white pieces with mouse clicks)
- **Black = AI** (Stockfish controls black pieces automatically)
- Turns alternate automatically after each move
- You can only move pieces of your current color

### AI Flow

1. Player clicks white piece → move plates appear
2. Player clicks move plate → white piece moves
3. Turn switches to black
4. GameManager requests best move from Stockfish (sends FEN position)
5. Stockfish calculates and returns UCI move (e.g., "e7e5")
6. GameManager executes the AI move automatically
7. Turn switches back to white

### Game State Tracking

- Board position converted to FEN notation
- Castling rights tracked automatically
- En passant squares tracked for pawn double-moves
- Move counters maintained (halfmove clock, fullmove number)

---

## Customization Options

### Adjust AI Difficulty

In Unity Inspector (StockfishManager):

- **Skill Level 0-5**: Beginner (makes mistakes)
- **Skill Level 6-10**: Intermediate
- **Skill Level 11-15**: Advanced
- **Skill Level 16-20**: Master/Expert (very strong)

### Adjust Thinking Time

- **500 ms**: Fast moves, weaker play
- **1000 ms**: Balanced (default)
- **2000-5000 ms**: Stronger moves, slower game
- **10000+ ms**: Very strong, but slow

### Change AI Side

To make AI play as white instead:

In [GameManager.cs](d:/ProjectGameDev/chessgame/Assets/Scripts/GameManager.cs), find `NextTurn()` method and change:

```csharp
// Current (AI is black):
if (currentPlayer == "black" && stockfishManager != null)

// Change to (AI is white):
if (currentPlayer == "white" && stockfishManager != null)
```

---

## Troubleshooting

### "Stockfish executable not found"

- Verify `stockfish.exe` is in the correct location
- Check the file name matches exactly (case-sensitive on some systems)
- Try absolute path in Inspector: `C:/path/to/stockfish.exe`

### "Stockfish did not respond in time"

- Increase **Thinking Time** in Inspector (try 3000ms)
- Check Windows Firewall isn't blocking the process
- Verify Stockfish.exe isn't corrupted (re-download)

### AI doesn't move

- Check Console for error messages
- Verify `StockfishManager` is assigned in GameManager Inspector
- Ensure you made a white move first (AI controls black by default)

### Pieces move incorrectly

- Check FEN output in Console ("Current FEN: ...")
- Verify board coordinates match chess notation (a1 = bottom-left)
- Ensure all 32 pieces are placed correctly at game start

### Turn switching not working

- Verify player field is public in ChessPiece.cs
- Check that turn restriction code is enabled in GameManager Update()

---

## Advanced Features (Optional)

### Add Pawn Promotion UI

Currently pawns auto-promote to queen. To add UI selection, modify `PromotePawn()` in GameManager.cs

### Add Castling Support

Requires tracking king/rook movement and implementing castle move logic in ChessPiece.cs

### Add En Passant

Framework exists in BoardStateConverter, but needs implementation in ChessPiece.cs pawn attack logic

### Add Check/Checkmate Detection

Use Stockfish's output analysis or implement king-in-check validation

### Two AI Players

Create two StockfishManager instances with different skill levels to watch them play each other

---

## Files Modified

| File                   | Changes                                              |
| ---------------------- | ---------------------------------------------------- |
| GameManager.cs         | Added AI components, turn management, move execution |
| ChessPiece.cs          | Made player field public, added pawn double-move     |
| StockfishManager.cs    | NEW - UCI protocol handler                           |
| BoardStateConverter.cs | NEW - FEN/UCI translation                            |

---

## Next Steps

1. **Download Stockfish** (see step 1 above)
2. **Configure Unity scene** (see step 3 above)
3. **Test the game!**
4. Adjust difficulty to your preference
5. Consider adding advanced features listed above

**Enjoy playing chess against Stockfish!** 🎮♟️
