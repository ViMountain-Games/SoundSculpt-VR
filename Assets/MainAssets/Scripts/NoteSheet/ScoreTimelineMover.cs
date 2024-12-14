using UnityEngine;
using GridGen;
using UnityEngine.Events;

public class ScoreTimelineMover : MonoBehaviour
{
    public TimelineMover mainTimelineMover;
    public ScoreGenerator scoreGenerator;

    private Vector3 startPosition;
    public float staffLength;
    private float maxStaffDistance;
    private float scoreboardSpeed;

    private bool isMoving = false;
    private bool loopMode;
    private Vector3 movementDirection;

    void OnEnable()
    {
        if (mainTimelineMover != null)
        {
            mainTimelineMover.OnMovementStarted.AddListener(OnMainTimelineStarted);
            mainTimelineMover.OnMovementFinished.AddListener(OnMainTimelineStopped);
        }
    }

    void OnDisable()
    {
        if (mainTimelineMover != null)
        {
            mainTimelineMover.OnMovementStarted.RemoveListener(OnMainTimelineStarted);
            mainTimelineMover.OnMovementFinished.RemoveListener(OnMainTimelineStopped);
        }
    }

    void Start()
    {
        if (mainTimelineMover == null || scoreGenerator == null)
        {
            Debug.LogError("Assegna TimelineMover e ScoreGenerator!");
            return;
        }

        // Ora staffLength è pubblico in ScoreGenerator (o esposto con una proprietà)
        //staffLength = scoreGenerator.lineLength;

        float pentagramHeight = (scoreGenerator.numberOfLines - 1) * scoreGenerator.lineSpacing;
        Vector3 newScale = transform.localScale;
        newScale.x = 0.01f;
        newScale.y = pentagramHeight;
        newScale.z = 0.01f;
        transform.localScale = newScale;

        // Allineare la timeline esattamente dove inizia lo spartito
        // Supponiamo che lo spartito cominci a scoreGenerator.transform.position.x
        float staffLeftX = scoreGenerator.transform.position.x;
        float centerY = scoreGenerator.transform.position.y + pentagramHeight * 0.5f;
        float centerZ = scoreGenerator.transform.position.z;

        startPosition = new Vector3(staffLeftX, centerY, centerZ);
        transform.position = startPosition;

        maxStaffDistance = staffLength;   // La timeline copre tutta la lunghezza dello spartito

        UpdateLocalParams();
        CalculateScoreboardSpeed();
    }

    void Update()
    {
        UpdateLocalParams();
        CalculateScoreboardSpeed();

        if (isMoving)
        {
            MoveTimeline();
        }
    }

    private void OnMainTimelineStarted()
    {
        isMoving = true;
        transform.position = startPosition;
    }

    private void OnMainTimelineStopped()
    {
        isMoving = false;
        transform.position = startPosition;
    }

    private void UpdateLocalParams()
    {
        if (mainTimelineMover == null) return;
        movementDirection = mainTimelineMover.direction.normalized;
        loopMode = mainTimelineMover.loopMode;
    }

    private void CalculateScoreboardSpeed()
    {
        if (mainTimelineMover == null) return;

        float mainSpeed = mainTimelineMover.speed;
        float mainMaxDistance = mainTimelineMover.MaxDistance;

        // Evita divisioni per zero
        if (mainMaxDistance > 0)
        {
            scoreboardSpeed = mainSpeed * (staffLength / mainMaxDistance);
        }
        else
        {
            scoreboardSpeed = 0f;
        }
    }

    private void MoveTimeline()
    {
        transform.Translate(movementDirection * scoreboardSpeed * Time.deltaTime);

        float traveled = Vector3.Distance(startPosition, transform.position);
        if (traveled >= maxStaffDistance)
        {
            if (loopMode)
            {
                transform.position = startPosition;
            }
            else
            {
                transform.position = startPosition;
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (scoreGenerator == null || mainTimelineMover == null) return;

        Gizmos.color = Color.magenta;
        Vector3 currentStart = Application.isPlaying ? startPosition : transform.position;
        float sphereRadius = transform.localScale.y * 0.5f / 50f;

        // Disegna linea pari a staffLength
        Vector3 endPos = currentStart + (movementDirection * staffLength);
        Gizmos.DrawLine(currentStart, endPos);
        Gizmos.DrawWireSphere(endPos, sphereRadius);
    }
}
