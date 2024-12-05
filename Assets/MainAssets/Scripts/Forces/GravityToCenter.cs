using UnityEngine;

public class GravityToCenter : MonoBehaviour
{
    public float sphereRadius = 5f; // Raggio della sfera
    public float attractionForce = 10f; // Forza di attrazione
    public float maxSpeed = 5f; // Velocit� massima dell'oggetto attirato

    private void OnDrawGizmos()
    {
        // Disegna una sfera visibile nell'editor per rappresentare il range
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, sphereRadius);
    }

    private void FixedUpdate()
    {
        // Trova tutti i collider all'interno della scena
        Collider[] colliders = Physics.OverlapSphere(transform.position, sphereRadius);
        foreach (var col in colliders)
        {
            // Verifica se l'oggetto ha un Rigidbody
            Rigidbody rb = col.attachedRigidbody;
            if (rb != null && !rb.isKinematic)
            {
                Vector3 toCenter = transform.position - rb.position;

                // Se l'oggetto � fuori dal range, applica una forza
                if (toCenter.magnitude > sphereRadius)
                {
                    Vector3 attraction = toCenter.normalized * attractionForce;
                    rb.AddForce(attraction);

                    // Limita la velocit� massima
                    if (rb.linearVelocity.magnitude > maxSpeed)
                    {
                        rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
                    }
                }
            }
        }
    }
}
