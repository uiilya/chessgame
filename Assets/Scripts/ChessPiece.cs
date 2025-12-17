using UnityEngine;

public class ChessPiece : MonoBehaviour
{
    // References
    public GameObject controller;
    public GameObject movePlate; // Reference to the prefab we drag in!

    // Sprites
    public Sprite black_queen, black_knight, black_bishop, black_king, black_rook, black_pawn;
    public Sprite white_queen, white_knight, white_bishop, white_king, white_rook, white_pawn;

    // Position
    private int xBoard = -1;
    private int yBoard = -1;
    
    // "black" or "white"
    public string player; 

    public void Activate()
    {
        controller = GameObject.FindGameObjectWithTag("GameController");

        // Take the instantiated prefab and adjust transform
        if (xBoard >= 0 && yBoard >= 0)
        {
            SetCoords();
        }

        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        // Setup sprites based on name
        switch (this.name)
        {
            case "black_queen": sr.sprite = black_queen; player = "black"; break;
            case "black_knight": sr.sprite = black_knight; player = "black"; break;
            case "black_bishop": sr.sprite = black_bishop; player = "black"; break;
            case "black_king": sr.sprite = black_king; player = "black"; break;
            case "black_rook": sr.sprite = black_rook; player = "black"; break;
            case "black_pawn": sr.sprite = black_pawn; player = "black"; break;
            
            case "white_queen": sr.sprite = white_queen; player = "white"; break;
            case "white_knight": sr.sprite = white_knight; player = "white"; break;
            case "white_bishop": sr.sprite = white_bishop; player = "white"; break;
            case "white_king": sr.sprite = white_king; player = "white"; break;
            case "white_rook": sr.sprite = white_rook; player = "white"; break;
            case "white_pawn": sr.sprite = white_pawn; player = "white"; break;
        }
    }

    public void SetCoords()
    {
        float x = xBoard;
        float y = yBoard;
        
        // Adjust geometry so pieces spawn centered
        // x *= 0.66f;
        // y *= 0.66f;

        // // Visual adjustment to fit board (You might need to tweak these numbers for your specific board sprite!)
        // x += -2.3f;
        // y += -2.3f;

        this.transform.position = new Vector3(x, y, -1.0f);
    }

    public int GetXBoard() { return xBoard; }
    public int GetYBoard() { return yBoard; }
    public void SetXBoard(int x) { xBoard = x; }
    public void SetYBoard(int y) { yBoard = y; }

    // ===============================================
    //               MOVEMENT LOGIC
    // ===============================================

    public void InitiateMovePlates()
    {
        // 1. Clean up old plates first
        DestroyMovePlates();

        // 2. Decide which plates to spawn based on piece type
        switch (this.name)
        {
            case "black_queen":
            case "white_queen":
                LineMovePlate(1, 0);
                LineMovePlate(0, 1);
                LineMovePlate(1, 1);
                LineMovePlate(-1, 0);
                LineMovePlate(0, -1);
                LineMovePlate(-1, -1);
                LineMovePlate(-1, 1);
                LineMovePlate(1, -1);
                break;

            case "black_knight":
            case "white_knight":
                LMovePlate();
                break;

            case "black_bishop":
            case "white_bishop":
                LineMovePlate(1, 1);
                LineMovePlate(1, -1);
                LineMovePlate(-1, 1);
                LineMovePlate(-1, -1);
                break;

            case "black_king":
            case "white_king":
                SurroundMovePlate();
                break;

            case "black_rook":
            case "white_rook":
                LineMovePlate(1, 0);
                LineMovePlate(0, 1);
                LineMovePlate(-1, 0);
                LineMovePlate(0, -1);
                break;

            case "black_pawn":
                PawnMovePlate(xBoard, yBoard - 1);
                break;
            case "white_pawn":
                PawnMovePlate(xBoard, yBoard + 1);
                break;
        }
    }

    // Helper: Sliding moves (Rook, Bishop, Queen)
    // xIncrement/yIncrement defines direction (e.g., 1,0 is Right)
    public void LineMovePlate(int xIncrement, int yIncrement)
    {
        GameManager sc = controller.GetComponent<GameManager>();

        int x = xBoard + xIncrement;
        int y = yBoard + yIncrement;

        while (sc.PositionOnBoard(x, y) && sc.GetPosition(x, y) == null)
        {
            MovePlateSpawn(x, y);
            x += xIncrement;
            y += yIncrement;
        }

        if (sc.PositionOnBoard(x, y) && sc.GetPosition(x, y).GetComponent<ChessPiece>().player != player)
        {
            MovePlateAttackSpawn(x, y);
        }
    }

    // Helper: Knight moves (L-shape)
    public void LMovePlate()
    {
        PointMovePlate(xBoard + 1, yBoard + 2);
        PointMovePlate(xBoard - 1, yBoard + 2);
        PointMovePlate(xBoard + 2, yBoard + 1);
        PointMovePlate(xBoard - 2, yBoard + 1);
        PointMovePlate(xBoard + 1, yBoard - 2);
        PointMovePlate(xBoard - 1, yBoard - 2);
        PointMovePlate(xBoard + 2, yBoard - 1);
        PointMovePlate(xBoard - 2, yBoard - 1);
    }

    // Helper: King moves (1 step all around)
    public void SurroundMovePlate()
    {
        PointMovePlate(xBoard, yBoard + 1);
        PointMovePlate(xBoard, yBoard - 1);
        PointMovePlate(xBoard - 1, yBoard - 1);
        PointMovePlate(xBoard - 1, yBoard);
        PointMovePlate(xBoard - 1, yBoard + 1);
        PointMovePlate(xBoard + 1, yBoard - 1);
        PointMovePlate(xBoard + 1, yBoard);
        PointMovePlate(xBoard + 1, yBoard + 1);
    }

    // Helper: Checking a single point (used by King and Knight)
    public void PointMovePlate(int x, int y)
    {
        GameManager sc = controller.GetComponent<GameManager>();
        if (sc.PositionOnBoard(x, y))
        {
            GameObject cp = sc.GetPosition(x, y);

            if (cp == null)
            {
                MovePlateSpawn(x, y);
            }
            else if (cp.GetComponent<ChessPiece>().player != player)
            {
                MovePlateAttackSpawn(x, y);
            }
        }
    }

    // Helper: Pawn Logic (Forward move, Diagonal Attack)
    public void PawnMovePlate(int x, int y)
    {
        GameManager sc = controller.GetComponent<GameManager>();
        if (sc.PositionOnBoard(x, y))
        {
            if (sc.GetPosition(x, y) == null)
            {
                MovePlateSpawn(x, y);
            }

            if (sc.PositionOnBoard(x + 1, y) && sc.GetPosition(x + 1, y) != null && sc.GetPosition(x + 1, y).GetComponent<ChessPiece>().player != player)
            {
                MovePlateAttackSpawn(x + 1, y);
            }

            if (sc.PositionOnBoard(x - 1, y) && sc.GetPosition(x - 1, y) != null && sc.GetPosition(x - 1, y).GetComponent<ChessPiece>().player != player)
            {
                MovePlateAttackSpawn(x - 1, y);
            }
        }
    }

    // Spawns a BLUE plate (Movement)
    public void MovePlateSpawn(int matrixX, int matrixY)
    {
        float x = matrixX;
        float y = matrixY;
        
        // Adjust these to match your board alignment!
        // x *= 0.66f;
        // y *= 0.66f;
        // x += -2.3f;
        // y += -2.3f;

        GameObject mp = Instantiate(movePlate, new Vector3(x, y, -3.0f), Quaternion.identity);
        
        MovePlate mpScript = mp.GetComponent<MovePlate>();
        mpScript.SetReference(gameObject);
        mpScript.SetCoords(matrixX, matrixY);
    }

    // Spawns a RED plate (Attack)
    public void MovePlateAttackSpawn(int matrixX, int matrixY)
    {
        float x = matrixX;
        float y = matrixY;
        
        // Adjust these to match your board alignment!
        // x *= 0.66f;
        // y *= 0.66f;
        // x += -2.3f;
        // y += -2.3f;

        GameObject mp = Instantiate(movePlate, new Vector3(x, y, -3.0f), Quaternion.identity);
        
        MovePlate mpScript = mp.GetComponent<MovePlate>();
        mpScript.attack = true;
        mpScript.SetReference(gameObject);
        mpScript.SetCoords(matrixX, matrixY);
    }

    // Clean up
    public void DestroyMovePlates()
    {
        GameObject[] movePlates = GameObject.FindGameObjectsWithTag("MovePlate");
        for (int i = 0; i < movePlates.Length; i++)
        {
            Destroy(movePlates[i]);
        }
    }
}