using UnityEngine;
using System.Collections.Generic;
// Aggiungi gli using appropriati per i tuoi namespace interni:
using GridGen;
using CustomInspector;
using MoreMountains.Feedbacks;

// Assicurati di avere la classe NoteData, Note, etc. nel tuo progetto
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

    // Memorizziamo quali oggetti hanno già avuto il cambio di NoteData dentro questo trigger
    // in modo da riconoscerli al momento dell'uscita dal trigger.
    private Dictionary<GameObject, bool> objectsDataChanged = new Dictionary<GameObject, bool>();

    private void OnTriggerEnter(Collider other)
    {
        // Aggiungiamo l'oggetto alla lista se il tag è corretto.
        if (other.CompareTag(targetTag))
        {
            objectsInsideThisTrigger.Add(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Rimuoviamo l'oggetto dalla lista se il tag è corretto
        if (other.CompareTag(targetTag))
        {
            objectsInsideThisTrigger.Remove(other.gameObject);

            // Se questo trigger ha enableSwap attivo, verifichiamo se l'oggetto
            // aveva subito il cambio di NoteData. In tal caso, avviamo la funzione
            // che cerca il child "DropWaterFeed" e chiama PlayFeedBacks().
            if (enableSwap && objectsDataChanged.ContainsKey(other.gameObject) && objectsDataChanged[other.gameObject])
            {
                TriggerDropWaterFeed(other.gameObject);
            }

            // Rimuoviamo dalla mappa l'oggetto per pulizia, se esiste.
            if (objectsDataChanged.ContainsKey(other.gameObject))
            {
                objectsDataChanged.Remove(other.gameObject);
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // Se lo swap è disabilitato su questo trigger (es. Trigger B), non facciamo nulla.
        if (!enableSwap) return;

        // Se l'oggetto non ha il tag corretto, ignoriamo.
        if (!other.CompareTag(targetTag)) return;

        // Verifichiamo che l'oggetto sia dentro al MIO trigger.
        if (!objectsInsideThisTrigger.Contains(other.gameObject)) return;

        // Se l'altro trigger esiste e l'oggetto è contemporaneamente dentro l'altro trigger,
        // NON assegniamo il NoteData.
        if (otherTrigger != null && otherTrigger.IsObjectInside(other.gameObject))
        {
            return;
        }

        // Recupera il componente Note
        Note noteComponent = other.GetComponent<Note>();
        if (noteComponent != null && noteComponent.noteData != null)
        {
            // Per evitare di riassegnare ogni frame, controlliamo se l'oggetto
            // ha già subito il cambio in questo trigger.
            if (!objectsDataChanged.ContainsKey(other.gameObject) || !objectsDataChanged[other.gameObject])
            {
                // Assegniamo il NoteData in base alla durata della nota
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

                // Segnaliamo che a questo oggetto è già stato cambiato il NoteData
                objectsDataChanged[other.gameObject] = true;
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

    /// <summary>
    /// Questa funzione trova il child "DropWaterFeed" dell'oggetto specificato,
    /// recupera il componente corrispondente e invoca PlayFeedBacks().
    /// </summary>
    private void TriggerDropWaterFeed(GameObject obj)
    {
        Transform dropWaterFeedTransform = obj.transform.Find("DropWaterFeed");
        if (dropWaterFeedTransform != null)
        {
            // Assumi che il componente si chiami "DropWaterFeed"
            MMF_Player dropWaterFeed = dropWaterFeedTransform.GetComponent<MMF_Player>();
            if (dropWaterFeed != null)
            {
                dropWaterFeed.PlayFeedbacks();
            }
        }
        else
        {
            Debug.LogWarning($"[NoteDataTrigger] Il GameObject '{obj.name}' non contiene un child di nome 'DropWaterFeed'.");
        }
    }
}
