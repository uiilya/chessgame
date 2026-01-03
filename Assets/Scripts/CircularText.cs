using UnityEngine;
using TMPro;

[ExecuteInEditMode]
public class CircularText : MonoBehaviour
{
    [Header("Settings")]
    public TMP_Text textComponent;

    [Tooltip("The radius of the circle. Positive = Arch Up, Negative = Smile.")]
    public float radius = 10f;

    [Tooltip("Increases the gap between letters without modifying font size.")]
    public float characterSpacing = 1f;

    private void Awake()
    {
        if (textComponent == null) textComponent = GetComponent<TMP_Text>();
    }

    private void Update()
    {
        CurveText();
    }

    private void OnValidate()
    {
        CurveText();
    }

    public void CurveText()
    {
        if (textComponent == null) return;

        // 1. Force update to ensure we have current character data
        textComponent.ForceMeshUpdate();

        var textInfo = textComponent.textInfo;
        int characterCount = textInfo.characterCount;

        if (characterCount == 0) return;

        // 2. Loop through every character
        for (int i = 0; i < characterCount; i++)
        {
            var charInfo = textInfo.characterInfo[i];

            if (!charInfo.isVisible) continue;

            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;
            Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

            // 3. Find the center point of the character (The pivot for rotation)
            // We use the average of the left and right x-coordinates
            float charMidX = (vertices[vertexIndex + 0].x + vertices[vertexIndex + 2].x) / 2f;
            
            // We define the pivot at y=0 (baseline) so characters rotate around the ring properly
            Vector3 charMidPos = new Vector3(charMidX, 0, 0);

            // 4. Calculate the Angle on the circle
            // Arc Length = radius * theta  ->  theta = Arc Length / radius
            // We use the character's X position as the "Arc Length" distance along the circle
            float angle = (charMidX / radius) * characterSpacing;

            // 5. Calculate position and rotation on the circle
            Vector3 circlePos = new Vector3(Mathf.Sin(angle) * radius, Mathf.Cos(angle) * radius, 0);
            
            // Calculate the rotation needed to point perpendicular to the circle center
            // We rotate around the Z axis
            Quaternion rotation = Quaternion.Euler(0, 0, -angle * Mathf.Rad2Deg);

            // 6. Apply to all 4 vertices of the character
            for (int j = 0; j < 4; j++)
            {
                // Get vertex position relative to the character's own center pivot
                Vector3 original = vertices[vertexIndex + j];
                Vector3 offset = original - charMidPos;

                // Rotate the vertex, then move it to the circle position
                Vector3 rotated = rotation * offset;
                Vector3 final = circlePos + rotated;

                // Offset the whole thing so the text stays near the GameObject's anchor
                // This keeps the "peak" of the arch at (0,0) locally
                final.y -= radius;

                vertices[vertexIndex + j] = final;
            }
        }

        // 7. Apply changes
        textComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
    }
}