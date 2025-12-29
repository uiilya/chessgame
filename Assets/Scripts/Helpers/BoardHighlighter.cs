using UnityEngine;
using System.Collections.Generic;

public class BoardHighlighter : MonoBehaviour
{
    [Header("Highlight Prefabs")]
    public GameObject highlightSquarePrefab; // FSH
    public GameObject highlightBorderPrefab; // BH
    public GameObject highlightDotPrefab;    // DH
    public GameObject movePlatePrefab;       // For Fallback

    // Tracking lists
    private List<GameObject> moveHints = new List<GameObject>(); 
    
    // Separated History Lists
    private List<GameObject> lastMoveHighlights = new List<GameObject>();
    private GameObject lastDeployHighlight = null; 

    public enum HighlightType { FullSquare, Border, Dot }

    public void ClearMoveHints()
    {
        foreach (GameObject g in moveHints) 
            if(g != null) Destroy(g);
        moveHints.Clear();
    }

    public void ClearLastMove()
    {
        foreach (GameObject g in lastMoveHighlights) 
            if(g != null) Destroy(g);
        lastMoveHighlights.Clear();
    }

    public void ClearLastDeploy()
    {
        if (lastDeployHighlight != null) Destroy(lastDeployHighlight);
        lastDeployHighlight = null;
    }

    public void ClearAll()
    {
        ClearMoveHints();
        ClearLastMove();
        ClearLastDeploy();
    }

    public GameObject CreateHighlight(int x, int y, Color color, HighlightType type, bool isHistory)
    {
        GameObject prefabToUse = highlightSquarePrefab; // Default FSH
        
        // Z-FIX: Force Z to be safely behind pieces (-1) but above board (0)
        float zPos = -0.1f; 
        
        // SORTING ORDER FIX: 
        // Board = 0
        // Highlights = 1 (FSH), 2 (Border/Dot)
        // Pieces = 5+
        int sortOrder = 1;

        switch (type)
        {
            case HighlightType.Border:
                prefabToUse = highlightBorderPrefab;
                zPos = -0.2f; 
                sortOrder = 2; // Draw borders on top of FSH squares
                break;
            case HighlightType.Dot:
                prefabToUse = highlightDotPrefab;
                zPos = -0.2f; 
                sortOrder = 2;
                break;
        }

        if (prefabToUse == null) prefabToUse = movePlatePrefab;

        GameObject hl = Instantiate(prefabToUse, new Vector3(x, y, zPos), Quaternion.identity);
        
        // Apply Color & Sorting Order
        SpriteRenderer[] renderers = hl.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sr in renderers)
        {
            sr.color = color;
            sr.sortingOrder = sortOrder; // CRITICAL FIX
        }

        // Cleanup logic
        if (hl.GetComponent<MovePlate>()) Destroy(hl.GetComponent<MovePlate>());
        
        if (isHistory) 
        {
            // Handled by specific history functions
        }
        else 
        {
            moveHints.Add(hl);
        }

        return hl;
    }

    public void HighlightLastMove(int fromX, int fromY, int toX, int toY)
    {
        ClearLastMove();
        Color historyColor = new Color(1f, 0.84f, 0f, 0.4f); // Gold FSH
        
        GameObject h1 = CreateHighlight(fromX, fromY, historyColor, HighlightType.FullSquare, true);
        GameObject h2 = CreateHighlight(toX, toY, historyColor, HighlightType.FullSquare, true);
        
        if(h1) lastMoveHighlights.Add(h1);
        if(h2) lastMoveHighlights.Add(h2);
    }

    public void HighlightEnemyDeploy(int x, int y)
    {
        ClearLastDeploy(); 
        GameObject hl = CreateHighlight(x, y, Color.red, HighlightType.Border, true);
        lastDeployHighlight = hl;
    }
}