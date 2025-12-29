using UnityEngine;

public class ChessPiece : MonoBehaviour
{
    public GameObject controller;
    public GameObject movePlate; 

    public Sprite black_queen, black_knight, black_bishop, black_king, black_rook, black_pawn;
    public Sprite white_queen, white_knight, white_bishop, white_king, white_rook, white_pawn;

    private int xBoard = -1;
    private int yBoard = -1;
    public string player; 

    private bool isDragging = false;
    private Vector3 dragStartPos;
    private GameObject selfHighlight = null;

    public void Activate()
    {
        controller = GameObject.FindGameObjectWithTag("GameController");
        if (xBoard >= 0 && yBoard >= 0) SetCoords();

        // FIX: Force Hitbox to fill the entire 1x1 grid square
        // This ensures clicks register even if the sprite is small or thin.
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col == null) col = gameObject.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1.0f, 1.0f);
        col.offset = Vector2.zero;

        // UPDATED: Use GetComponentInChildren so we can move visuals to a child object
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        string pieceName = this.name.Replace("(Clone)", "").Trim();
        
        if (sr != null)
        {
            switch (pieceName)
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
    }

    public void SetCoords()
    {
        float x = xBoard;
        float y = yBoard;
        this.transform.position = new Vector3(x, y, -1.0f);
    }

    public int GetXBoard() { return xBoard; }
    public int GetYBoard() { return yBoard; }
    public void SetXBoard(int x) { xBoard = x; }
    public void SetYBoard(int y) { yBoard = y; }

    void OnMouseDown()
    {
        GameManager gm = controller.GetComponent<GameManager>();
        if (!gm.IsPlayerTurn()) return;
        if (player != "white") return;

        isDragging = true;
        dragStartPos = new Vector3(xBoard, yBoard, -1.0f);
        
        InitiateMovePlates();
        
        transform.position = new Vector3(transform.position.x, transform.position.y, -5.0f);
    }

    void OnMouseDrag()
    {
        if (!isDragging) return;
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        transform.position = new Vector3(mousePos.x, mousePos.y, -5.0f);
    }

    void OnMouseUp()
    {
        if (!isDragging) return;
        isDragging = false;

        Collider2D myCollider = GetComponent<Collider2D>();
        if(myCollider) myCollider.enabled = false;

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mousePos2D = new Vector2(mousePos.x, mousePos.y);

        RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);
        if(myCollider) myCollider.enabled = true;

        bool droppedOnPlate = false;

        if (hit.collider != null)
        {
            MovePlate mp = hit.collider.GetComponent<MovePlate>();
            if (mp != null)
            {
                droppedOnPlate = true;
                mp.OnMouseUpReference(); 
            }
        }

        if (!droppedOnPlate)
        {
            transform.position = dragStartPos;
            
            float dist = Vector3.Distance(dragStartPos, new Vector3(mousePos.x, mousePos.y, -1.0f));
            if (dist > 0.5f)
            {
                controller.GetComponent<GameManager>().ClearMoveHints();
                DestroyMovePlates(); 
            }
        }
    }

    // ===============================================
    //               MOVEMENT LOGIC
    // ===============================================

    public void InitiateMovePlates()
    {
        DestroyMovePlates();
        GameManager gm = controller.GetComponent<GameManager>();
        gm.ClearMoveHints();

        selfHighlight = gm.CreateHighlight(xBoard, yBoard, new Color(0f, 1f, 0f, 0.5f), BoardHighlighter.HighlightType.FullSquare, false);
        
        if (selfHighlight != null)
        {
            selfHighlight.transform.position = new Vector3(xBoard, yBoard, -0.1f);
            
            SpriteRenderer sr = selfHighlight.GetComponent<SpriteRenderer>();
            if(sr) sr.sortingOrder = 1;
        }

        string pieceName = this.name.Replace("(Clone)", "").Trim();

        switch (pieceName)
        {
            case "white_queen":
                LineMovePlate(1, 0); LineMovePlate(0, 1); LineMovePlate(1, 1);
                LineMovePlate(-1, 0); LineMovePlate(0, -1); LineMovePlate(-1, -1);
                LineMovePlate(-1, 1); LineMovePlate(1, -1);
                break;
            case "white_knight": LMovePlate(); break;
            case "white_bishop": 
                LineMovePlate(1, 1); LineMovePlate(1, -1);
                LineMovePlate(-1, 1); LineMovePlate(-1, -1);
                break;
            case "white_king": SurroundMovePlate(); break;
            case "white_rook": 
                LineMovePlate(1, 0); LineMovePlate(0, 1);
                LineMovePlate(-1, 0); LineMovePlate(0, -1);
                break;
            case "white_pawn":
                PawnMovePlate(xBoard, yBoard + 1);
                if (yBoard == 1 && controller.GetComponent<GameManager>().GetPosition(xBoard, yBoard + 1) == null)
                    PawnMovePlate(xBoard, yBoard + 2);
                break;
        }
    }
    
    public void LineMovePlate(int xIncrement, int yIncrement) {
        GameManager sc = controller.GetComponent<GameManager>();
        int x = xBoard + xIncrement; int y = yBoard + yIncrement;
        while (sc.PositionOnBoard(x, y) && sc.GetPosition(x, y) == null) { MovePlateSpawn(x, y); x += xIncrement; y += yIncrement; }
        if (sc.PositionOnBoard(x, y) && sc.GetPosition(x, y).GetComponent<ChessPiece>().player != player) MovePlateAttackSpawn(x, y);
    }
    
    public void LMovePlate() {
        PointMovePlate(xBoard + 1, yBoard + 2); PointMovePlate(xBoard - 1, yBoard + 2);
        PointMovePlate(xBoard + 2, yBoard + 1); PointMovePlate(xBoard - 2, yBoard + 1);
        PointMovePlate(xBoard + 1, yBoard - 2); PointMovePlate(xBoard - 1, yBoard - 2);
        PointMovePlate(xBoard + 2, yBoard - 1); PointMovePlate(xBoard - 2, yBoard - 1);
    }

    public void SurroundMovePlate() {
        PointMovePlate(xBoard, yBoard + 1); PointMovePlate(xBoard, yBoard - 1);
        PointMovePlate(xBoard - 1, yBoard - 1); PointMovePlate(xBoard - 1, yBoard);
        PointMovePlate(xBoard - 1, yBoard + 1); PointMovePlate(xBoard + 1, yBoard - 1);
        PointMovePlate(xBoard + 1, yBoard); PointMovePlate(xBoard + 1, yBoard + 1);
    }

    public void PointMovePlate(int x, int y) {
        GameManager sc = controller.GetComponent<GameManager>();
        if (sc.PositionOnBoard(x, y)) {
            GameObject cp = sc.GetPosition(x, y);
            if (cp == null) MovePlateSpawn(x, y);
            else if (cp.GetComponent<ChessPiece>().player != player) MovePlateAttackSpawn(x, y);
        }
    }

    public void PawnMovePlate(int x, int y) {
        GameManager sc = controller.GetComponent<GameManager>();
        if (sc.PositionOnBoard(x, y)) {
            if (sc.GetPosition(x, y) == null) MovePlateSpawn(x, y);
            if (sc.PositionOnBoard(x + 1, y) && sc.GetPosition(x + 1, y) != null && sc.GetPosition(x + 1, y).GetComponent<ChessPiece>().player != player) MovePlateAttackSpawn(x + 1, y);
            if (sc.PositionOnBoard(x - 1, y) && sc.GetPosition(x - 1, y) != null && sc.GetPosition(x - 1, y).GetComponent<ChessPiece>().player != player) MovePlateAttackSpawn(x - 1, y);
        }
    }

    public void MovePlateSpawn(int matrixX, int matrixY)
    {
        GameObject mp = Instantiate(movePlate, new Vector3(matrixX, matrixY, -3.0f), Quaternion.identity);
        MovePlate mpScript = mp.GetComponent<MovePlate>();
        mpScript.SetReference(gameObject);
        mpScript.SetCoords(matrixX, matrixY);
        mp.GetComponent<SpriteRenderer>().enabled = false; 

        GameManager gm = controller.GetComponent<GameManager>();
        gm.CreateHighlight(matrixX, matrixY, Color.green, BoardHighlighter.HighlightType.Dot, false);
    }

    public void MovePlateAttackSpawn(int matrixX, int matrixY)
    {
        GameObject mp = Instantiate(movePlate, new Vector3(matrixX, matrixY, -3.0f), Quaternion.identity);
        MovePlate mpScript = mp.GetComponent<MovePlate>();
        mpScript.attack = true;
        mpScript.SetReference(gameObject);
        mpScript.SetCoords(matrixX, matrixY);
        mp.GetComponent<SpriteRenderer>().enabled = false;

        GameManager gm = controller.GetComponent<GameManager>();
        gm.CreateHighlight(matrixX, matrixY, Color.green, BoardHighlighter.HighlightType.Border, false);
    }

    public void DestroyMovePlates()
    {
        GameObject[] movePlates = GameObject.FindGameObjectsWithTag("MovePlate");
        for (int i = 0; i < movePlates.Length; i++) Destroy(movePlates[i]);
        
        if(selfHighlight != null) 
        {
            Destroy(selfHighlight);
            selfHighlight = null;
        }
    }
}