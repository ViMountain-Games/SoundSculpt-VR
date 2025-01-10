using UnityEngine;

public class ApplyTorqueController : MonoBehaviour
{
    [Header("Torque Settings")]
    [Tooltip("Direzione del torque in termini di assi locali del GameObject.")]
    public Vector3 torqueAxis = Vector3.up;

    [Tooltip("Intensità del torque.")]
    public float torqueIntensity = 10f;

    [Tooltip("Durata dell'applicazione del torque in secondi.")]
    public float torqueDuration = 2f;

    private Rigidbody rb;
    private float torqueEndTime;
    private bool isTorqueActive = false;

    void Start()
    {
        // Ottieni il Rigidbody associato al GameObject
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError("Nessun Rigidbody trovato! Aggiungi un Rigidbody al GameObject.");
        }

        // Imposta il tempo di fine applicazione del torque
        torqueEndTime = Time.time + torqueDuration;
        isTorqueActive = true;
    }

    void FixedUpdate()
    {
        if (isTorqueActive && rb != null)
        {
            if (Time.time <= torqueEndTime)
            {
                ApplyTorque();
            }
            else
            {
                isTorqueActive = false; // Termina l'applicazione del torque
            }
        }
    }

    public void ApplyTorque()
    {
        if (rb != null)
        {
            // Calcola il torque
            Vector3 torque = transform.TransformDirection(torqueAxis.normalized) * torqueIntensity;

            // Applica il torque
            rb.AddTorque(torque, ForceMode.Force);
        }
    }

    void OnDrawGizmosSelected()
    {
        // Disegna una freccia che rappresenta la direzione del torque
        Gizmos.color = Color.red;
        Vector3 worldTorqueDirection = transform.TransformDirection(torqueAxis.normalized);
        Gizmos.DrawLine(transform.position, transform.position + worldTorqueDirection * 2);
        Gizmos.DrawSphere(transform.position + worldTorqueDirection * 2, 0.1f);
    }
}
