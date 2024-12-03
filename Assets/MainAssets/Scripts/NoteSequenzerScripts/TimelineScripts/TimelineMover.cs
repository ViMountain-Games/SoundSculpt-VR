using UnityEngine;
using CustomInspector;

public class TimelineMover : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Beats per minute controlling the speed.")]
    [Min(60), Max(240)]
    public float bpm = 120f;

    [Tooltip("Direction of movement.")]
    public Vector3 direction = Vector3.right;

    [Tooltip("Speed scale multiplier.")]
    [Range(0.1f, 5f)]
    public float speedScale = 1f;

    [Tooltip("Maximum distance before resetting.")]
    [Min(1)]
    public float maxDistance = 10f;

    [Tooltip("Time scaling factor to adjust real-world movement.")]
    [Range(0.1f, 2f)]
    public float timeScale = 1f;

    [Header("Debug Settings")]
    [Tooltip("Color of the Gizmos for visualization.")]
    public Color gizmoColor = Color.red;

    [ReadOnly] // Show in Inspector but not editable
    public float speed;

    private Vector3 startPosition;
    private bool isMoving = false;

    void Start()
    {
        startPosition = transform.position;
        CalculateSpeed();
    }

    void Update()
    {
        if (isMoving)
        {
            MoveTimeline();
        }
    }

    [ContextMenu("Start Movement")]
    public void StartMovement()
    {
        if (!isMoving)
        {
            isMoving = true;
            startPosition = transform.position; // Reset the initial position
            Debug.Log("Movement started.");
        }
    }

    [ContextMenu("Calculate Speed")]
    private void CalculateSpeed()
    {
        // Normalizing speed based on timeScale
        speed = (bpm / 60f) * speedScale * timeScale;
        Debug.Log("Speed recalculated: " + speed);
    }

    private void MoveTimeline()
    {
        transform.Translate(direction.normalized * speed * Time.deltaTime);

        if (Vector3.Distance(startPosition, transform.position) >= maxDistance)
        {
            StopMovement();
            ReturnToStartPosition();
        }
    }

    private void StopMovement()
    {
        isMoving = false;
        Debug.Log("Movement stopped.");
    }

    private void ReturnToStartPosition()
    {
        transform.position = startPosition;
        Debug.Log("Returned to start position.");
    }

    private void OnTriggerEnter(Collider other)
    {
        Note note = other.GetComponent<Note>();
        if (note != null)
        {
            note.PlayNote();
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;

        // Use the current position as the start point
        Vector3 currentStartPosition = Application.isPlaying ? startPosition : transform.position;

        // Calculate the sphere radius as the average of Y and Z scales
        float sphereRadius = ((transform.localScale.y + transform.localScale.z) / 2f) / 50;

        // Draw the line from the current position
        Gizmos.DrawLine(currentStartPosition, currentStartPosition + direction.normalized * maxDistance);

        // Draw a sphere at the end of the max distance with a dynamic radius
        Gizmos.DrawWireSphere(currentStartPosition + direction.normalized * maxDistance, sphereRadius);
    }
}
