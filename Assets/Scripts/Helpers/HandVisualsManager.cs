using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HandVisualsManager : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject cardPrefab;
    public GameObject cardBackPrefab;

    [Header("White Icons")]
    public Sprite icon_queen_white;
    public Sprite icon_rook_white;
    public Sprite icon_bishop_white;
    public Sprite icon_knight_white;
    public Sprite icon_pawn_white;

    [Header("Black Icons")]
    public Sprite icon_queen_black;
    public Sprite icon_rook_black;
    public Sprite icon_bishop_black;
    public Sprite icon_knight_black;
    public Sprite icon_pawn_black;

    public void RefreshPlayerHand(List<Card> hand)
    {
        // Cleanup old cards
        GameObject[] existing = GameObject.FindGameObjectsWithTag("Bank");
        foreach (GameObject g in existing) Destroy(g);

        float cardSpacing = 1.2f;
        float startY = -2.5f;
        float totalWidth = (hand.Count - 1) * cardSpacing;
        float startX = 3.5f - (totalWidth / 2);

        for (int i = 0; i < hand.Count; i++)
        {
            float x = startX + (i * cardSpacing);
            float z = -2.0f - (i * 0.1f);
            int sortOrder = 10 + (i * 10);
            SpawnCardObject(hand[i], x, startY, z, i, sortOrder);
        }
    }

    void SpawnCardObject(Card data, float x, float y, float z, int index, int sortOrder)
    {
        GameObject obj = Instantiate(cardPrefab, new Vector3(x, y, z), Quaternion.identity);
        obj.name = "Card_" + data.cardName;
        obj.tag = "Bank";

        Sprite art = GetIcon(data.pieceType, "white");
        
        CardObject co = obj.GetComponent<CardObject>();
        co.SetOriginalPos(new Vector3(x, y, z));
        co.Setup(data, index, art, sortOrder);
    }

    public Sprite GetIcon(string type, string color)
    {
        if (color == "white")
        {
            switch (type)
            {
                case "queen": return icon_queen_white;
                case "rook": return icon_rook_white;
                case "bishop": return icon_bishop_white;
                case "knight": return icon_knight_white;
                case "pawn": return icon_pawn_white;
            }
        }
        else
        {
            switch (type)
            {
                case "queen": return icon_queen_black;
                case "rook": return icon_rook_black;
                case "bishop": return icon_bishop_black;
                case "knight": return icon_knight_black;
                case "pawn": return icon_pawn_black;
            }
        }
        return null;
    }

    // Moved the complex morph animation here
    public IEnumerator AnimateAIDeploy(Vector3 startPos, Vector3 targetPos, string pieceType)
    {
        GameObject visualObj = Instantiate(cardBackPrefab, startPos, Quaternion.identity);

        // FIX: Remove CardObject script if present to prevent teleporting
        CardObject co = visualObj.GetComponent<CardObject>();
        if (co != null) Destroy(co);
        
        yield return new WaitForSeconds(0.2f);

        Sprite pieceSprite = GetIcon(pieceType, "black");

        if (pieceSprite != null)
        {
            foreach (Transform child in visualObj.transform) Destroy(child.gameObject);
            SpriteRenderer sr = visualObj.GetComponent<SpriteRenderer>();
            if (sr == null) sr = visualObj.AddComponent<SpriteRenderer>();

            sr.sprite = pieceSprite;
            visualObj.transform.localScale = new Vector3(1f, 1f, 1f);
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 100;
            sr.color = Color.white;
        }

        yield return new WaitForSeconds(0.5f);

        float duration = 0.4f;
        float elapsed = 0f;
        Vector3 morphPos = visualObj.transform.position;

        while (elapsed < duration)
        {
            if (visualObj == null) break;
            float t = elapsed / duration;
            t = t * t * (3f - 2f * t);

            visualObj.transform.position = Vector3.Lerp(morphPos, new Vector3(targetPos.x, targetPos.y, -3.0f), t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (visualObj != null) Destroy(visualObj);
    }
}