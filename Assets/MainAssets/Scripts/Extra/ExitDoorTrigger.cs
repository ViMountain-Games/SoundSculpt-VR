using UnityEngine;
using UnityEngine.Events;
using CustomInspector;

public class ExitDoorTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    [Tooltip("Il tag dell'oggetto che attiverà l'evento.")]
    [Tag]
    public string targetTag;

    [Header("Unity Event")] 
    [Tooltip("Evento da avviare quando il trigger viene attivato.")]
    public UnityEvent onTriggerActivated;

    private void OnTriggerEnter(Collider other)
    {
        // Controlla se l'oggetto che ha attivato il trigger ha il tag corretto
        if (other.CompareTag(targetTag))
        {
            // Attiva l'evento Unity
            onTriggerActivated?.Invoke();
        }
    }

    // Facoltativo: Metodo per configurare il tag dall'Inspector o da un altro script
    public void SetTargetTag(string newTag)
    {
        targetTag = newTag;
    }
}