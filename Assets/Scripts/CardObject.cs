using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class CardObject : MonoBehaviour
{
    // Data
    public Card cardData;
    public int handIndex;
    
    // Visual References
    public SpriteRenderer cardArt; 
    public SpriteRenderer borderSprite;      
    public SpriteRenderer backgroundSprite;  

    public TextMeshPro nameText;
    public TextMeshPro descriptionText;
    public TextMeshPro costText;

    public List<GameObject> visualsToHide; 

    // State Variables
    private Vector3 originalPos;
    private Vector3 originalScale; 
    private int originalSortingOrder; 
    private bool isDragging = false;
    
    private GameManager manager;
    private GameObject currentHighlight = null;

    [Header("Hover Settings")]
    public float hoverStrength = 0.1f; 
    public float hoverSpeed = 2.5f;    
    public float hoverScale = 1.2f;    
    public float dragOffsetX = 0.5f; // Visual offset while dragging

    void Awake()
    {
        originalScale = transform.localScale;

        // --- AUTO-FIND VISUALS ---
        if (borderSprite == null)
        {
            Transform t = transform.Find("Border");
            if (t != null) borderSprite = t.GetComponent<SpriteRenderer>();
        }
        if (backgroundSprite == null)
        {
            Transform t = transform.Find("Background");
            if (t != null) backgroundSprite = t.GetComponent<SpriteRenderer>();
        }
        if (cardArt == null)
        {
            Transform t = transform.Find("Portrait");
            if (t != null) cardArt = t.GetComponent<SpriteRenderer>();
        }
    }

    void Start()
    {
        manager = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameManager>();
    }

    void Update()
    {
        if (!isDragging)
        {
            float timeOffset = handIndex * 0.5f;
            float newY = originalPos.y + Mathf.Sin(Time.time * hoverSpeed + timeOffset) * hoverStrength;
            transform.position = new Vector3(originalPos.x, newY, originalPos.z);
        }
    }

    public void Setup(Card data, int index, Sprite art, int sortingOrder)
    {
        cardData = data;
        handIndex = index;
        originalSortingOrder = sortingOrder; 

        if(cardArt != null) cardArt.sprite = art;
        UpdateVisuals();
        UpdateSorting(sortingOrder);
    }

    void UpdateVisuals()
    {
        if (cardData == null) return;
        if(nameText) nameText.text = cardData.cardName;
        if(descriptionText) descriptionText.text = cardData.description;
        if(costText) costText.text = cardData.cost.ToString();
    }

    void UpdateSorting(int order)
    {
        // 1. Root Renderer (if exists) -> Base Layer
        SpriteRenderer rootSr = GetComponent<SpriteRenderer>();
        if (rootSr != null) rootSr.sortingOrder = order;

        // 2. Generic Visuals -> Default to Middle Layer (Order + 1)
        foreach(var g in visualsToHide)
        {
            if (g == null) continue;
            SpriteRenderer sr = g.GetComponent<SpriteRenderer>();
            if(sr) sr.sortingOrder = order + 1;
        }

        // --- STRICT LAYERING OVERRIDES ---
        
        // Layer 0: Border (Bottom)
        if (borderSprite != null) borderSprite.sortingOrder = order;

        // Layer 1: Background (Middle)
        if (backgroundSprite != null) backgroundSprite.sortingOrder = order + 1;

        // Layer 2: Portrait (Top)
        if (cardArt != null) cardArt.sortingOrder = order + 2;

        // Layer 3: Text (Overlay)
        int textOrder = order + 3;
        if(nameText) nameText.GetComponent<MeshRenderer>().sortingOrder = textOrder;
        if(descriptionText) descriptionText.GetComponent<MeshRenderer>().sortingOrder = textOrder;
        if(costText) costText.GetComponent<MeshRenderer>().sortingOrder = textOrder;
    }

    // --- HELPER TO TOGGLE CARD SHELL ---
    void SetDecorationsActive(bool isActive)
    {
        if (borderSprite != null) borderSprite.enabled = isActive;
        if (backgroundSprite != null) backgroundSprite.enabled = isActive;
        
        if (nameText != null) nameText.enabled = isActive;
        if (descriptionText != null) descriptionText.enabled = isActive;
        if (costText != null) costText.enabled = isActive;

        foreach (GameObject g in visualsToHide)
        {
            if (g != null) g.SetActive(isActive);
        }
    }

    // --- MOUSE INTERACTIONS ---

    void OnMouseEnter()
    {
        if (isDragging) return;
        transform.localScale = originalScale * hoverScale;
        UpdateSorting(originalSortingOrder + 20); // Pop to front
    }

    void OnMouseExit()
    {
        if (isDragging) return;
        transform.localScale = originalScale;
        UpdateSorting(originalSortingOrder); // Restore order
    }

    void OnMouseDown()
    {
        if (manager == null) return;
        
        transform.localScale = originalScale * 1.2f;
        UpdateSorting(100); 
        isDragging = true;
        
        // Hide the card shell so only the portrait floats
        SetDecorationsActive(false);
    }

    void OnMouseDrag()
    {
        if (!isDragging) return;

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        
        // Apply Bobbing logic
        float dragBobY = mousePos.y + Mathf.Sin(Time.time * hoverSpeed) * hoverStrength;
        
        // Apply visual offset so card isn't directly under cursor
        transform.position = new Vector3(mousePos.x + dragOffsetX, dragBobY, -5.0f); 

        // NOTE: We calculate 'x' and 'y' based on the TRUE mouse position, not the offset card position
        int x = Mathf.RoundToInt(mousePos.x);
        int y = Mathf.RoundToInt(mousePos.y);
        
        bool isValid = manager.CanPlacePiece(x, y, "white", cardData.pieceType);
        
        if (currentHighlight != null && (currentHighlight.transform.position.x != x || currentHighlight.transform.position.y != y))
        {
            Destroy(currentHighlight);
        }

        if (isValid && currentHighlight == null)
        {
            currentHighlight = manager.CreateHighlight(x, y, new Color(0f, 0.84f, 0f, 0.4f), BoardHighlighter.HighlightType.FullSquare, false);
        }
        else if (!isValid && currentHighlight != null)
        {
            Destroy(currentHighlight);
        }
    }

    void OnMouseUp()
    {
        isDragging = false;
        if (currentHighlight != null) Destroy(currentHighlight);

        // FIX: Use 'mousePos' (Cursor) instead of 'transform.position' (Card Visual).
        // This ensures the drop location matches exactly where the mouse pointer is,
        // ignoring the dragOffsetX and bobbing effect.
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        bool success = manager.TryDeployCard(cardData, handIndex, mousePos);

        if (!success)
        {
            // Reset Visuals (Show the shell again)
            SetDecorationsActive(true);
            if(cardArt != null) cardArt.transform.localScale = Vector3.one; 
            
            // Snap back
            transform.position = originalPos;
            transform.localScale = originalScale; 
            UpdateSorting(originalSortingOrder);
        }
    }

    public void SetOriginalPos(Vector3 pos)
    {
        originalPos = pos;
    }
}