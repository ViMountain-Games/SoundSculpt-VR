using System.Collections.Generic;
using UnityEngine;
using CustomInspector;

public class GravityToCenter : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Raggio della zona 'sicura' in cui gli oggetti non vengono attratti.")]
    public float sphereRadius = 5f;

    [Tooltip("Raggio totale in cui controlliamo la presenza di oggetti (deve essere >= sphereRadius).")]
    public float checkRadius = 10f;

    [Tooltip("Forza di attrazione verso il centro (deve essere moderata per non risultare violenta).")]
    public float attractionForce = 10f;

    [Tooltip("Velocità massima degli oggetti.")]
    public float maxSpeed = 5f;

    [Tooltip("Tag degli oggetti che possono essere attratti.")]
    [Tag] public string targetTag = "Note";

    // Set per tenere traccia degli oggetti considerati all'interno della zona sicura
    private HashSet<Rigidbody> insideSet = new HashSet<Rigidbody>();

    private void OnDrawGizmos()
    {
        // Disegna la sfera del range "sicuro"
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, sphereRadius);

        // Disegna anche la sfera del range di controllo totale
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, checkRadius);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Verifica se l'oggetto ha il tag specificato
        if (!other.CompareTag(targetTag))
            return;

        Rigidbody rb = other.attachedRigidbody;
        if (rb != null && !rb.isKinematic)
        {
            // Oggetto entrato nella zona sicura
            insideSet.Add(rb);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Verifica se l'oggetto ha il tag specificato
        if (!other.CompareTag(targetTag))
            return;

        Rigidbody rb = other.attachedRigidbody;
        if (rb != null && insideSet.Contains(rb))
        {
            // Oggetto uscito dalla zona sicura, ora potrà essere attratto se si trova fuori da sphereRadius
            insideSet.Remove(rb);
        }
    }

    private void FixedUpdate()
    {
        // Trova tutti i collider entro checkRadius
        Collider[] colliders = Physics.OverlapSphere(transform.position, checkRadius);
        foreach (var col in colliders)
        {
            // Verifica se l'oggetto ha il tag specificato
            if (!col.CompareTag(targetTag))
                continue;

            Rigidbody rb = col.attachedRigidbody;
            if (rb != null && !rb.isKinematic)
            {
                Vector3 toCenter = transform.position - rb.position;
                float distance = toCenter.magnitude;

                // Se l'oggetto è fuori dalla zona sicura (distance > sphereRadius) e non è dentro il set dei "sicuri"
                if (distance > sphereRadius && !insideSet.Contains(rb))
                {
                    // Applica la forza di attrazione verso il centro
                    Vector3 attraction = toCenter.normalized * attractionForce;
                    rb.AddForce(attraction, ForceMode.Acceleration);

                    // Limita la velocità massima
                    if (rb.linearVelocity.magnitude > maxSpeed)
                    {
                        rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
                    }
                }
                // Se l'oggetto è dentro la zona sicura (insideSet) o entro sphereRadius, non applichiamo forza.
            }
        }
    }
}
