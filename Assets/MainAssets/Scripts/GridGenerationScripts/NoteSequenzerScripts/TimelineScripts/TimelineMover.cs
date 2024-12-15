using UnityEngine;
using CustomInspector;
using GridGen;
using UnityEngine.Events;

namespace GridGen
{
    public class TimelineMover : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Min(60), Max(240)]
        public float bpm = 120f;
        public Vector3 direction = Vector3.right;
        [Range(0.1f, 5f)]
        public float speedScale = 1f;
        [Range(0.1f, 2f)]
        public float timeScale = 1f;
        public Vector3 positionOffset = Vector3.zero;

        [Header("Debug Settings")]
        public Color gizmoColor = Color.red;

        [ReadOnly]
        public float speed;

        private Vector3 startPosition;
        private bool isMoving = false;

        private Grid3DGenerator gridGenerator;
        private float maxDistance;

        [Header("Loop Settings")]
        public bool loopMode = false;

        [Header("Timeline Events")]
        public UnityEvent OnMovementStarted;
        public UnityEvent OnMovementFinished;

        public bool IsMoving { get { return isMoving; } }
        public float MaxDistance { get { return maxDistance; } }

        // **Nuova Variabile**: Aggiunge una lunghezza extra (non moltiplicata) alla timeline.
        [Header("Extra Length (Additive)")]
        public float additionalLength = 0f;

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
            float gridCenterY = gridOrigin.y + gridHeight / 2f;
            float gridCenterZ = gridOrigin.z + gridDepth / 2f;

            float halfTimelineWidth = transform.localScale.x * 0.5f;
            startPosition = new Vector3(
                gridOrigin.x - halfTimelineWidth,
                gridCenterY,
                gridCenterZ
            ) + positionOffset;

            transform.position = startPosition;

            // Distanza totale base che la timeline deve percorrere
            maxDistance = gridWidth + halfTimelineWidth * 2f;

            // Aggiungiamo la distanza extra (additiva)
            maxDistance += additionalLength;

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
                OnMovementStarted?.Invoke();
            }
        }

        [ContextMenu("Stop Movement")]
        public void StopMovement()
        {
            if (isMoving)
            {
                isMoving = false;
                Grid3DGenerator.Instance?.CheckCombination();
                OnMovementFinished?.Invoke();
            }
        }

        [ContextMenu("Toggle Loop Mode")]
        public void ToggleLoopMode()
        {
            loopMode = !loopMode;
        }

        [ContextMenu("Calculate Speed")]
        private void CalculateSpeed()
        {
            speed = (bpm / 60f) * speedScale * timeScale;
        }

        private void MoveTimeline()
        {
            transform.Translate(direction.normalized * speed * Time.deltaTime);

            if (Vector3.Distance(startPosition, transform.position) >= maxDistance)
            {
                if (loopMode)
                {
                    transform.position = startPosition;
                    OnMovementStarted?.Invoke();
                    Grid3DGenerator.Instance?.CheckCombination();
                }
                else
                {
                    StopMovement();
                    ReturnToStartPosition();
                }
            }
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
                //Debug.Log("Timeline is not moving. Note will not play.");
            }
        }

        private void ReturnToStartPosition()
        {
            transform.position = startPosition;
            OnMovementFinished?.Invoke();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;
            Vector3 currentStartPosition = Application.isPlaying ? startPosition : transform.position;
            float sphereRadius = ((transform.localScale.y + transform.localScale.z) / 2f) / 50f;

            // Disegna la linea e la sfera che segnalano la fine del percorso
            Gizmos.DrawLine(currentStartPosition, currentStartPosition + direction.normalized * maxDistance);
            Gizmos.DrawWireSphere(currentStartPosition + direction.normalized * maxDistance, sphereRadius);
        }
    }
}
