using System.Collections.Generic;
using CustomInspector;
using UnityEngine;

public class RespawnTrigger : MonoBehaviour
{
    [Header("Configurazione")]
    [Tooltip("Tag degli oggetti da teletrasportare")]
    [Tag]
    public string targetTag = "YourTag";

    [Tooltip("Oggetto dove teletrasportare l'oggetto")]
    public Transform teleportTarget;

    [Tooltip("Forza applicata dopo il teletrasporto")]
    public float addForceAmount = 10f;

    // Lista per tenere traccia degli oggetti già processati durante l'evento
    private HashSet<Rigidbody> processedRigidbodies = new HashSet<Rigidbody>();

    private void Start()
    {
        // Controlla subito se il teleportTarget è assegnato
        if (teleportTarget == null)
        {
            Debug.LogError("Teleport target non assegnato! Disabilito lo script per evitare errori.");
            enabled = false; // Disabilita lo script per prevenire problemi
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Trova il Rigidbody nel parent (o nello stesso oggetto)
        Rigidbody rb = other.GetComponentInParent<Rigidbody>();
        if (rb == null || !rb.gameObject.CompareTag(targetTag)) return;

        // Aggiungi e processa il Rigidbody solo se non già gestito
        if (processedRigidbodies.Add(rb))
        {
            TeleportObject(rb);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Rimuoviamo il Rigidbody dalla lista quando esce dal trigger
        Rigidbody rb = other.GetComponentInParent<Rigidbody>();
        if (rb != null)
        {
            processedRigidbodies.Remove(rb);
        }
    }

    private void TeleportObject(Rigidbody rb)
    {
        // Teletrasporta l'oggetto alla posizione del target
        rb.position = teleportTarget.position;

        // Calcola la forza lungo l'asse Y locale del target
        Vector3 forceDirection = teleportTarget.up * addForceAmount;

        // Resetta la velocità per evitare movimenti residui e applica la forza
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(forceDirection, ForceMode.Impulse);

#if UNITY_EDITOR
        //Debug.Log($"Oggetto {rb.gameObject.name} teletrasportato a {teleportTarget.position} con forza: {forceDirection}");
#endif
    }
}
