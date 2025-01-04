using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CombinationEventManager : MonoBehaviour
{
    [Header("Event Configuration")]
    [Tooltip("Numero di eventi necessari per attivare l'evento combinato.")]
    public int requiredEventCount = 2;

    [Header("Combined Event")]
    [Tooltip("Evento attivato quando tutti gli eventi necessari sono stati chiamati.")]
    public UnityEvent onAllEventsTriggered;

    private HashSet<int> triggeredEvents = new HashSet<int>(); // Per tenere traccia degli eventi attivati
    private int currentEventId = 0; // Identificativo univoco per ciascun evento

    /// <summary>
    /// Metodo da assegnare agli eventi di altri codici (es. B e C).
    /// </summary>
    public void RegisterEvent()
    {
        triggeredEvents.Add(currentEventId++);

        // Verifica se il numero di eventi richiesti è stato raggiunto
        if (triggeredEvents.Count >= requiredEventCount)
        {
            onAllEventsTriggered?.Invoke();
            ResetEvents(); // Resetta per poter riutilizzare il sistema
        }
    }

    /// <summary>
    /// Resetta lo stato degli eventi per un nuovo ciclo.
    /// </summary>
    private void ResetEvents()
    {
        triggeredEvents.Clear();
    }
}
