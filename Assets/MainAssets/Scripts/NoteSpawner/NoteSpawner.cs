using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using CustomInspector;

[System.Serializable]
public class NotePrefabMapping
{
    public NoteData noteData;   // Il NoteData corrispondente
    public GameObject prefab;   // Il Prefab associato
}

public class NoteSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Origin point for spawning prefabs.")]
    public GameObject origin;

    [Tooltip("Mapping tra NoteData e Prefabs.")]
    public List<NotePrefabMapping> noteMappings;

    [ReadOnly]
    [Tooltip("Indice corrente del prefab selezionato (solo per debug).")]
    public int selectedPrefabIndex = 0;

    [Header("Direction and Force")]
    [Tooltip("Direction in which the prefab will be pushed.")]
    public Vector3 spawnDirection = Vector3.forward;

    [Tooltip("Amount of force applied to the prefab.")]
    public float forceAmount = 10f;

    [Header("Rotation Settings")]
    public bool applyRotation = false;

    [Tooltip("Torque vector to apply to the spawned prefab.")]
    public Vector3 rotationTorque;

    [Tooltip("Intensity of the torque.")]
    public float torqueIntensity = 1f;

    [Header("Gizmo Settings")]
    [Tooltip("Color of the Gizmo direction arrow.")]
    public Color gizmoColor = Color.green;

    [Tooltip("Length of the Gizmo direction arrow.")]
    public float gizmoLength = 2f;

    [Header("Note Picker Reference")]
    [Tooltip("Riferimento al NotePicker da cui recuperare la nota selezionata")]
    public GridGen.NotePicker notePicker;

    // ------------------------- NUOVE PROPRIETÀ -------------------------

    [Header("Spawn Limit")]
    [Tooltip("Numero massimo di volte in cui è possibile fare lo spawn.")]
    public int spawnLimit = 5;
    private int currentSpawnCount = 0;

    [Header("Spawn Delay")]
    [Tooltip("Ritardo (in secondi) tra la chiamata del metodo e lo spawn effettivo.")]
    public float spawnDelay = 1f;

    [Header("Spawn Events")]
    [Tooltip("Evento chiamato all'avvio del metodo SpawnNote.")]
    public UnityEvent onSpawnNoteCalled;

    [Tooltip("Evento chiamato dopo il ritardo, prima di effettivamente istanziare l'oggetto.")]
    public UnityEvent onSpawnNoteDelayed;

    // ---------------------------------------------------------------

    private List<Queue<GameObject>> objectPools;

    private void Awake()
    {
        InitializePools();
    }

    private void InitializePools()
    {
        if (noteMappings == null || noteMappings.Count == 0)
        {
            Debug.LogWarning("Note mappings are empty or not assigned!");
            return;
        }

        objectPools = new List<Queue<GameObject>>(noteMappings.Count);

        foreach (var mapping in noteMappings)
        {
            Queue<GameObject> pool = new Queue<GameObject>();
            for (int j = 0; j < 10; j++) // Initial pool size
            {
                GameObject obj = Instantiate(mapping.prefab);
                obj.SetActive(false);
                pool.Enqueue(obj);
            }
            objectPools.Add(pool);
        }
    }

    private GameObject GetPooledObject(int prefabIndex)
    {
        if (prefabIndex < 0 || prefabIndex >= objectPools.Count)
        {
            Debug.LogWarning("Selected prefab index is out of range!");
            return null;
        }

        Queue<GameObject> pool = objectPools[prefabIndex];

        GameObject pooledObj;
        if (pool.Count > 0)
        {
            pooledObj = pool.Dequeue();
        }
        else
        {
            pooledObj = Instantiate(noteMappings[prefabIndex].prefab);
        }

        pooledObj.SetActive(true);
        return pooledObj;
    }

    // Questo è il metodo pubblico che viene chiamato per spawnare una nota.
    // Appena entra in questo metodo, richiama l'evento onSpawnNoteCalled, poi avvia la coroutine che eseguirà lo spawn effettivo dopo un delay.
    public void SpawnNote()
    {
        // Controlla se si è raggiunto il limite massimo di spawn
        if (currentSpawnCount >= spawnLimit)
        {
            Debug.LogWarning("Hai raggiunto il limite massimo di spawn!");
            return;
        }

        // Invoca l'evento al momento della chiamata del metodo
        if (onSpawnNoteCalled != null)
        {
            onSpawnNoteCalled.Invoke();
        }

        // Avvia la coroutine che effettua lo spawn dopo il ritardo
        StartCoroutine(SpawnNoteCoroutine());
    }

    // Questa coroutine viene avviata da SpawnNote e si occupa di attendere il ritardo prima di effettuare lo spawn.
    private IEnumerator SpawnNoteCoroutine()
    {
        // Attendere il ritardo definito
        yield return new WaitForSeconds(spawnDelay);

        // Invoca l'evento dopo il ritardo, prima dello spawn effettivo
        if (onSpawnNoteDelayed != null)
        {
            onSpawnNoteDelayed.Invoke();
        }

        // Esegue lo spawn vero e proprio
        PerformSpawn();

        // Incrementa il contatore di spawn effettuati
        currentSpawnCount++;
    }

    // Qui mettiamo la logica che era inizialmente in SpawnNote per il picking della nota e lo spawn effettivo dell'oggetto.
    private void PerformSpawn()
    {
        // Controlla se il NotePicker ha un oggetto selezionato
        if (notePicker == null || notePicker.selectedObject == null)
        {
            Debug.LogWarning("SelectedObject is not assigned in NotePicker. Cannot spawn an object!");
            return;
        }

        // Recupera il NoteData del selectedObject
        var selectedNote = notePicker.selectedObject.GetComponent<GridGen.Note>();
        if (selectedNote == null || selectedNote.noteData == null)
        {
            Debug.LogWarning("SelectedObject does not have a valid Note or NoteData. Cannot spawn an object!");
            return;
        }

        // Trova l'indice del NoteData nel mapping
        bool foundMapping = false;
        for (int i = 0; i < noteMappings.Count; i++)
        {
            if (noteMappings[i].noteData == selectedNote.noteData)
            {
                selectedPrefabIndex = i;
                foundMapping = true;
                break;
            }
        }

        if (!foundMapping)
        {
            Debug.LogWarning("No matching NoteData found in NoteMappings for the selected object!");
            return;
        }

        // Ottieni un oggetto dal pool
        GameObject spawnedObject = GetPooledObject(selectedPrefabIndex);
        if (spawnedObject == null)
        {
            Debug.LogWarning("Could not spawn object: pooled object is null!");
            return;
        }

        // Posizionalo all'origine
        spawnedObject.transform.position = origin.transform.position;
        spawnedObject.transform.rotation = origin.transform.rotation;

        // Gestione fisica
        Rigidbody rb = spawnedObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Calcola la direzione in coordinate globali
            Vector3 globalDirection = origin.transform.TransformDirection(spawnDirection.normalized);

            // Resetta eventuali velocità residue
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // Applica la forza
            rb.AddForce(globalDirection * forceAmount, ForceMode.Impulse);

            // Applica la rotazione se richiesto
            if (applyRotation)
            {
                Vector3 torque = rotationTorque.normalized * torqueIntensity;
                rb.AddTorque(torque, ForceMode.Impulse);
            }
        }
    }

    // Disegna la freccia del gizmo in scena, utile per il debugging
    private void OnDrawGizmos()
    {
        if (origin == null) return;

        Gizmos.color = gizmoColor;
        Vector3 direction = origin.transform.TransformDirection(spawnDirection.normalized) * gizmoLength;
        Gizmos.DrawRay(origin.transform.position, direction);
    }
}
