using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class CardObject : MonoBehaviour
{
    // Data
    public Card cardData;
    public int handIndex;
    
    // --- UPDATED VISUAL REFERENCES ---
    public SpriteRenderer cardArt;    // The separate Card Illustration
    public SpriteRenderer cardBase;   // The new Background (includes Border)

    public TextMeshPro nameText;
    public TextMeshPro descriptionText;
    public TextMeshPro costText;

    // Generic list for any extra decorative icons you might add later
    public List<GameObject> visualsToHide; 

    // State Variables
    private Vector3 originalPos;
    private Vector3 originalScale; 
    private int originalSortingOrder; 
    private bool isDragging = false;
    
    private GameManager manager;
    private GameObject currentHighlight = null;

    private bool isInitialized = false;

    [Header("Hover Settings")]
    public float hoverStrength = 0.1f; 
    public float hoverSpeed = 2.5f;    
    public float hoverScale = 1.2f;    
    public float dragOffsetX = 0.5f; 

    void Awake()
    {
        originalScale = transform.localScale;

        // --- UPDATED AUTO-FIND LOGIC ---
        // We look for "CardBase" (renamed from Background)
        if (cardBase == null)
        {
            Transform t = transform.Find("CardBase"); 
            // Fallback for backward compatibility if you haven't renamed it yet
            if (t == null) t = transform.Find("Background"); 
            if (t != null) cardBase = t.GetComponent<SpriteRenderer>();
        }

        if (cardArt == null)
        {
            Transform t = transform.Find("Portrait");
            if (t != null) cardArt = t.GetComponent<SpriteRenderer>();
        }
    }

    void Start()
    {
        // Safety check to prevent crash if GameManager isn't found
        GameObject gc = GameObject.FindGameObjectWithTag("GameController");
        if (gc != null) manager = gc.GetComponent<GameManager>();
    }

    void Update()
    {
        if (!isInitialized) return;

        if (!isDragging)
        {
            float timeOffset = handIndex * 0.5f;
            float newY = originalPos.y + Mathf.Sin(Time.time * hoverSpeed + timeOffset) * hoverStrength;
            transform.position = new Vector3(originalPos.x, newY, originalPos.z);
        }
    }

    // Call this with the SPECIFIC card art sprite, not the chess piece sprite
    public void Setup(Card data, int index, int sortingOrder)
{
    cardData = data;
    handIndex = index;
    originalSortingOrder = sortingOrder; 

    // Use the sprite directly from the Card Data object
    if(cardArt != null && cardData.cardArt != null) 
    {
        cardArt.sprite = cardData.cardArt;
    }
    
    UpdateVisuals();
    UpdateSorting(sortingOrder);

    isInitialized = true;
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
        // 1. Root Renderer -> Base Layer
        SpriteRenderer rootSr = GetComponent<SpriteRenderer>();
        if (rootSr != null) rootSr.sortingOrder = order;

        // 2. Generic Visuals -> Middle Layer
        foreach(var g in visualsToHide)
        {
            if (g == null) continue;
            SpriteRenderer sr = g.GetComponent<SpriteRenderer>();
            if(sr) sr.sortingOrder = order + 1;
        }

        // --- NEW STRICT LAYERING ---
        
        // Layer 0: The Base (Background + Border merged)
        if (cardBase != null) cardBase.sortingOrder = order;

        // Layer 1: The Portrait (Sits on top of the base)
        if (cardArt != null) cardArt.sortingOrder = order + 1;

        // Layer 2: Text (Sits on top of everything)
        int textOrder = order + 2;
        if(nameText) nameText.GetComponent<MeshRenderer>().sortingOrder = textOrder;
        if(descriptionText) descriptionText.GetComponent<MeshRenderer>().sortingOrder = textOrder;
        if(costText) costText.GetComponent<MeshRenderer>().sortingOrder = textOrder;
    }

    // --- HELPER TO TOGGLE CARD SHELL ---
    void SetDecorationsActive(bool isActive)
    {
        // Hide the single base object
        if (cardBase != null) cardBase.enabled = isActive;
        
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
        UpdateSorting(originalSortingOrder + 50); // Pop to front
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
        
        float dragBobY = mousePos.y + Mathf.Sin(Time.time * hoverSpeed) * hoverStrength;
        
        // Keep the drag offset logic from your original script
        transform.position = new Vector3(mousePos.x + dragOffsetX, dragBobY, -5.0f); 

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