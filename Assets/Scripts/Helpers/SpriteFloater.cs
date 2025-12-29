using UnityEngine;

public class SpriteFloater : MonoBehaviour
{
    [Header("Bobbing Settings")]
    public float amplitude = 0.08f; // How high/low it moves (in units)
    public float frequency = 2.5f;  // How fast it moves
    
    private Vector3 startLocalPos;
    private float timeOffset;

    void Start()
    {
        startLocalPos = transform.localPosition;
        // Add a random offset so all pieces don't bob in perfect synchronization
        timeOffset = Random.Range(0f, 2f * Mathf.PI);
    }

    void Update()
    {
        // We only modify the Y position relative to the parent
        float newY = startLocalPos.y + Mathf.Sin(Time.time * frequency + timeOffset) * amplitude;
        
        transform.localPosition = new Vector3(startLocalPos.x, newY, startLocalPos.z);
    }
}