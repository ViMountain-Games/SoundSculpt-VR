using UnityEngine;
using System.Collections.Generic;
using CustomInspector;

[System.Serializable]
public class NotePrefabMapping
{
    public NoteData noteData;  // Il NoteData corrispondente
    public GameObject prefab; // Il Prefab associato
}

public class NoteSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Origin point for spawning prefabs.")]
    public GameObject origin;

    [Tooltip("Mapping tra NoteData e Prefabs.")]
    public List<NotePrefabMapping> noteMappings;

    [ReadOnly]
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

    public void SpawnNote()
    {
        // ------------------------- INIZIO MODIFICA -------------------------
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
        // -------------------------- FINE MODIFICA --------------------------

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

        Rigidbody rb = spawnedObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 globalDirection = origin.transform.TransformDirection(spawnDirection.normalized);
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(globalDirection * forceAmount, ForceMode.Impulse);

            if (applyRotation)
            {
                Vector3 torque = rotationTorque.normalized * torqueIntensity;
                rb.AddTorque(torque, ForceMode.Impulse);
            }
        }
    }
}
