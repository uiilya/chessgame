using UnityEngine;
using TMPro;

[ExecuteInEditMode]
public class TextCurver : MonoBehaviour
{
    [Header("Settings")]
    public TMP_Text textComponent;
    
    [Tooltip("The shape of the curve. X axis is the width of the text (0 to 1).")]
    public AnimationCurve vertexCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.25f, 2.0f), new Keyframe(0.5f, 0), new Keyframe(0.75f, 2.0f), new Keyframe(1, 0f));
    
    [Tooltip("Multiplier for the curve height.")]
    public float curveScale = 1.0f;

    [Tooltip("Force the text to update continuously. Useful for animating the curve.")]
    public bool updateContinuously = true;

    private void Awake()
    {
        if (textComponent == null) textComponent = GetComponent<TMP_Text>();
    }

    private void Update()
    {
        // Only update if parameters dictate (save performance)
        if (updateContinuously || !Application.isPlaying)
        {
            CurveText();
        }
    }

    private void OnValidate()
    {
        // Update immediately when changing values in the Inspector
        CurveText();
    }

    public void CurveText()
    {
        if (textComponent == null) return;

        // 1. Force the TMP object to update its mesh so we have valid data
        textComponent.ForceMeshUpdate();

        var textInfo = textComponent.textInfo;
        int characterCount = textInfo.characterCount;

        if (characterCount == 0) return;

        // 2. Get the bounds of the text to normalize our curve evaluation
        float boundsMinX = textComponent.bounds.min.x;
        float boundsMaxX = textComponent.bounds.max.x;
        float boundsWidth = boundsMaxX - boundsMinX;

        // 3. Loop through every character
        for (int i = 0; i < characterCount; i++)
        {
            var charInfo = textInfo.characterInfo[i];

            // Skip invisible characters (spaces, etc.)
            if (!charInfo.isVisible) continue;

            // Get the index of the material and the first vertex of this character
            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;

            // Get the vertices array for this character's mesh
            Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

            // 4. Offset the character's Y position based on the curve
            // We calculate the center X of the character to determine where on the curve it sits
            Vector3 offsetToMidBaseline = new Vector3((vertices[vertexIndex + 0].x + vertices[vertexIndex + 2].x) / 2, charInfo.baseLine);
            
            // Normalize x position to 0-1 range for the AnimationCurve
            float normalizedX = (offsetToMidBaseline.x - boundsMinX) / boundsWidth; 
            
            // Calculate the Y offset
            float yOffset = vertexCurve.Evaluate(normalizedX) * curveScale;

            // Apply offset to all 4 vertices of the character (BL, TL, TR, BR)
            vertices[vertexIndex + 0].y += yOffset;
            vertices[vertexIndex + 1].y += yOffset;
            vertices[vertexIndex + 2].y += yOffset;
            vertices[vertexIndex + 3].y += yOffset;
        }

        // 5. Upload the modified mesh data
        textComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
    }
}