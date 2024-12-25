using UnityEngine;
using System.Collections.Generic;
using GridGen;
using CustomInspector;

public class NoteDataTrigger : MonoBehaviour
{
    [Tooltip("Abilita/Disabilita l'assegnazione del NoteData in questo trigger.")]
    public bool enableSwap = true;

    [Tooltip("Il tag dell'oggetto che deve attivare il trigger (es. 'Player')")]
    [Tag]
    public string targetTag = "Player";

    [Tooltip("Il NoteData da assegnare quando la nota è 1/4 (Quarter)")]
    public NoteData quarterNoteData;

    [Tooltip("Il NoteData da assegnare quando la nota è 2/4 (Half)")]
    public NoteData halfNoteData;

    [Tooltip("Il NoteData da assegnare quando la nota è 4/4 (Whole)")]
    public NoteData wholeNoteData;

    [Tooltip("Il riferimento all'altro trigger, per evitare di assegnare se l'oggetto sta toccando entrambi")]
    public NoteDataTrigger otherTrigger;

    // Memorizziamo gli oggetti che si trovano attualmente all'interno di questo trigger
    private HashSet<GameObject> objectsInsideThisTrigger = new HashSet<GameObject>();

    private void OnTriggerEnter(Collider other)
    {
        // Quando l'oggetto entra, lo aggiungiamo alla lista solo se il tag è corretto.
        if (other.CompareTag(targetTag))
        {
            objectsInsideThisTrigger.Add(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Quando l'oggetto esce, lo rimuoviamo dalla lista
        if (other.CompareTag(targetTag))
        {
            objectsInsideThisTrigger.Remove(other.gameObject);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // Se lo swap è disabilitato su questo trigger, non facciamo nulla.
        if (!enableSwap) return;

        // Se l'oggetto non ha il tag che ci interessa, ignoriamo.
        if (!other.CompareTag(targetTag)) return;

        // Verifichiamo che l'oggetto sia dentro al MIO trigger.
        if (!objectsInsideThisTrigger.Contains(other.gameObject)) return;

        // Se l'altro trigger esiste e l'oggetto è contemporaneamente dentro l'altro trigger, NON assegniamo il NoteData.
        if (otherTrigger != null && otherTrigger.IsObjectInside(other.gameObject))
        {
            return;
        }

        // A questo punto, l'oggetto è dentro questo trigger, non è (o non esiste) l'altro trigger,
        // ed enableSwap è true: assegniamo il NoteData in base alla durata della nota.
        Note noteComponent = other.GetComponent<Note>();
        if (noteComponent != null && noteComponent.noteData != null)
        {
            switch (noteComponent.noteData.duration)
            {
                case NoteData.NoteDuration.Quarter:
                    noteComponent.ApplyNoteDataAndMaterial(quarterNoteData);
                    break;

                case NoteData.NoteDuration.Half:
                    noteComponent.ApplyNoteDataAndMaterial(halfNoteData);
                    break;

                case NoteData.NoteDuration.Whole:
                    noteComponent.ApplyNoteDataAndMaterial(wholeNoteData);
                    break;
            }
        }
    }

    /// <summary>
    /// Restituisce true se l'oggetto specificato è dentro questo trigger.
    /// </summary>
    public bool IsObjectInside(GameObject obj)
    {
        return objectsInsideThisTrigger.Contains(obj);
    }
}
