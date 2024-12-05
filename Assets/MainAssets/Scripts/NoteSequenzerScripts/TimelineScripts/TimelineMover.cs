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

    [Tooltip("Time scaling factor to adjust real-world movement.")]
    [Range(0.1f, 2f)]
    public float timeScale = 1f;

    [Tooltip("Offset for the initial position of the timeline.")]
    public Vector3 positionOffset = Vector3.zero;

    [Header("Debug Settings")]
    [Tooltip("Color of the Gizmos for visualization.")]
    public Color gizmoColor = Color.red;

    [ReadOnly] // Show in Inspector but not editable
    public float speed;

    private Vector3 startPosition;
    private bool isMoving = false;

    private Grid3DGenerator gridGenerator;

    private float maxDistance;

    [Header("Loop Settings")]
    [Tooltip("Enable or disable loop mode.")]
    public bool loopMode = false; // Toggle for loop mode

    void Start()
    {
        gridGenerator = Grid3DGenerator.Instance;
        if (gridGenerator == null)
        {
            Debug.LogError("Grid3DGenerator instance not found!");
            return;
        }

        float gridHeight = gridGenerator.gridSizeY * gridGenerator.cellSize.value;
        float gridDepth = gridGenerator.gridSizeZ * gridGenerator.cellSize.value;
        float gridWidth = gridGenerator.gridSizeX * gridGenerator.cellSize.value;

        Vector3 newScale = transform.localScale;
        newScale.y = gridHeight;
        newScale.z = gridDepth;
        transform.localScale = newScale;

        Vector3 gridOrigin = gridGenerator.transform.position;

        float gridCenterY = gridOrigin.y + gridHeight / 2;
        float gridCenterZ = gridOrigin.z + gridDepth / 2;

        float halfTimelineWidth = transform.localScale.x / 2;

        startPosition = new Vector3(
            gridOrigin.x - halfTimelineWidth,
            gridCenterY,
            gridCenterZ
        ) + positionOffset; // Apply the offset

        transform.position = startPosition;

        maxDistance = gridWidth + halfTimelineWidth * 2;

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
            transform.position = startPosition;
            Debug.Log("Movement started.");
        }
    }

    [ContextMenu("Stop Movement")]
    public void StopMovement()
    {
        isMoving = false;
        Debug.Log("Movement stopped.");
    }

    [ContextMenu("Toggle Loop Mode")]
    public void ToggleLoopMode()
    {
        loopMode = !loopMode;
        Debug.Log("Loop mode toggled. Current state: " + (loopMode ? "Enabled" : "Disabled"));
    }

    [ContextMenu("Calculate Speed")]
    private void CalculateSpeed()
    {
        speed = (bpm / 60f) * speedScale * timeScale;
        Debug.Log("Speed recalculated: " + speed);
    }

    private void MoveTimeline()
    {
        transform.Translate(direction.normalized * speed * Time.deltaTime);

        if (Vector3.Distance(startPosition, transform.position) >= maxDistance)
        {
            if (loopMode)
            {
                transform.position = startPosition; // Reset position for looping
                Debug.Log("Timeline looped.");
            }
            else
            {
                StopMovement();
                ReturnToStartPosition();
            }
        }
    }

    private void ReturnToStartPosition()
    {
        transform.position = startPosition;
        Debug.Log("Returned to start position.");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isMoving)
        {
            Note note = other.GetComponent<Note>();
            if (note != null)
            {
                note.PlayNote();
            }
        }
        else
        {
            Debug.Log("Timeline is not moving. Note will not play.");
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;

        Vector3 currentStartPosition = Application.isPlaying ? startPosition : transform.position;

        float sphereRadius = ((transform.localScale.y + transform.localScale.z) / 2f) / 50;

        Gizmos.DrawLine(currentStartPosition, currentStartPosition + direction.normalized * maxDistance);

        Gizmos.DrawWireSphere(currentStartPosition + direction.normalized * maxDistance, sphereRadius);
    }
}
