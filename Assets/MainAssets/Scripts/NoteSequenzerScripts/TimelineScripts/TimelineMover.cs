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

    // Removed public maxDistance; it will be calculated based on grid size
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

    private Grid3DGenerator gridGenerator;

    private float maxDistance; // Now calculated based on grid size

    void Start()
    {
        // Get the instance of Grid3DGenerator
        gridGenerator = Grid3DGenerator.Instance;
        if (gridGenerator == null)
        {
            Debug.LogError("Grid3DGenerator instance not found!");
            return;
        }

        // Calculate grid dimensions
        float gridHeight = gridGenerator.gridSizeY * gridGenerator.cellSize.value;
        float gridDepth = gridGenerator.gridSizeZ * gridGenerator.cellSize.value;
        float gridWidth = gridGenerator.gridSizeX * gridGenerator.cellSize.value; // Get grid width

        // Scale the TimelineMover based on grid dimensions on Y and Z axes
        Vector3 newScale = transform.localScale;
        newScale.y = gridHeight;
        newScale.z = gridDepth;
        transform.localScale = newScale;

        // Position the TimelineMover to the left of the grid
        Vector3 gridOrigin = gridGenerator.transform.position;

        float gridCenterY = gridOrigin.y + gridHeight / 2;
        float gridCenterZ = gridOrigin.z + gridDepth / 2;

        float halfTimelineWidth = transform.localScale.x / 2;

        // Set the initial position
        startPosition = new Vector3(
            gridOrigin.x - halfTimelineWidth, // To the left of the grid
            gridCenterY,                      // Centered on Y axis
            gridCenterZ                       // Centered on Z axis
        );

        transform.position = startPosition;

        // Set maxDistance based on the length of the grid on the X axis
        maxDistance = gridWidth + halfTimelineWidth * 2; // So it moves past the grid

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
            transform.position = startPosition; // Reset position
            Debug.Log("Movement started.");
        }
    }

    [ContextMenu("Calculate Speed")]
    private void CalculateSpeed()
    {
        // Normalize speed based on timeScale
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
