using UnityEngine;
using System.Collections.Generic;
using CustomInspector;

public class NoteSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Origin point for spawning prefabs.")]
    public GameObject origin; // L'origine da cui verrà istanziato il prefab

    [Tooltip("List of prefabs to spawn.")]
    public GameObject[] prefabs; // Lista di prefabs disponibili

    [Tooltip("Select the prefab index to spawn.")]
    [Min(0)]
    public int selectedPrefabIndex = 0; // Indice del prefab selezionato

    [Header("Direction and Force")]
    [Tooltip("Direction in which the prefab will be pushed.")]
    public Vector3 spawnDirection = Vector3.forward; // Direzione in cui spingere il prefab

    [Tooltip("Amount of force applied to the prefab.")]
    public float forceAmount = 10f; // La quantità di forza da applicare

    [Header("Rotation Settings")]
    public bool applyRotation = false; // Bool per decidere se applicare una rotazione

    [Tooltip("Torque vector to apply to the spawned prefab.")]
    [ShowIf(nameof(applyRotation))]
    public Vector3 rotationTorque; // Intensità della rotazione da applicare (in torque)

    [Tooltip("Intensity of the torque.")]
    [ShowIf(nameof(applyRotation))]
    public float torqueIntensity = 1f; // Fattore moltiplicativo per la forza del torque

    [Header("Gizmo Settings")]
    [Tooltip("Color of the Gizmo direction arrow.")]
    public Color gizmoColor = Color.green; // Colore del Gizmo

    [Tooltip("Length of the Gizmo direction arrow.")]
    public float gizmoLength = 2f; // Lunghezza della freccia del Gizmo

    [Header("Pooling Settings")]
    [Tooltip("Initial size of the pool for each prefab.")]
    public int initialPoolSize = 10;

    [Button(nameof(SpawnNote),
                label = "Spawn Note")]

    private List<Queue<GameObject>> objectPools; // Un pool (coda) per ciascun prefab

    private void Awake()
    {
        InitializePools();
    }

    /// <summary>
    /// Crea i pool di oggetti per ogni prefab.
    /// </summary>
    private void InitializePools()
    {
        if (prefabs == null || prefabs.Length == 0)
        {
            Debug.LogWarning("Prefabs list is empty or not assigned!");
            return;
        }

        objectPools = new List<Queue<GameObject>>(prefabs.Length);

        for (int i = 0; i < prefabs.Length; i++)
        {
            Queue<GameObject> pool = new Queue<GameObject>();

            for (int j = 0; j < initialPoolSize; j++)
            {
                GameObject obj = Instantiate(prefabs[i]);
                obj.SetActive(false);
                pool.Enqueue(obj);
            }

            objectPools.Add(pool);
        }
    }

    /// <summary>
    /// Ottiene un oggetto dal pool, se disponibile. Altrimenti ne istanzia uno nuovo.
    /// </summary>
    private GameObject GetPooledObject(int prefabIndex)
    {
        if (prefabIndex < 0 || prefabIndex >= prefabs.Length)
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
            // Se il pool è vuoto, crea un nuovo oggetto
            pooledObj = Instantiate(prefabs[prefabIndex]);
        }

        pooledObj.SetActive(true);
        return pooledObj;
    }

    /// <summary>
    /// Restituisce un oggetto al pool. Da chiamare quando l'oggetto non serve più.
    /// </summary>
    public void ReturnToPool(int prefabIndex, GameObject obj)
    {
        if (prefabIndex < 0 || prefabIndex >= prefabs.Length)
        {
            Debug.LogWarning("Invalid prefab index when returning to pool.");
            Destroy(obj);
            return;
        }

        obj.SetActive(false);
        objectPools[prefabIndex].Enqueue(obj);
    }

    
    public void SpawnNote()
    {
        // Validazione dei parametri
        if (origin == null)
        {
            Debug.LogWarning("Origin is not assigned!");
            return;
        }

        if (prefabs == null || prefabs.Length == 0)
        {
            Debug.LogWarning("Prefabs list is empty or not assigned!");
            return;
        }

        if (selectedPrefabIndex < 0 || selectedPrefabIndex >= prefabs.Length)
        {
            Debug.LogWarning("Selected prefab index is out of range!");
            return;
        }

        GameObject prefabToSpawn = prefabs[selectedPrefabIndex];
        if (prefabToSpawn == null)
        {
            Debug.LogWarning("Selected prefab is null!");
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

        // Ottieni il Rigidbody e applica la forza
        Rigidbody rb = spawnedObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Calcola la direzione globale
            Vector3 globalDirection = origin.transform.TransformDirection(spawnDirection.normalized);

            // Resetta velocità e rotazione
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // Applica la forza al Rigidbody
            rb.AddForce(globalDirection * forceAmount, ForceMode.Impulse);

            // Applica il torque se abilitato
            if (applyRotation)
            {
                Vector3 torque = rotationTorque.normalized * torqueIntensity;
                rb.AddTorque(torque, ForceMode.Impulse);
            }
        }
        else
        {
            Debug.LogWarning("The spawned object does not have a Rigidbody component!");
        }
    }

    private void OnDrawGizmos()
    {
        if (origin == null)
            return;

        // Imposta il colore del Gizmo
        Gizmos.color = gizmoColor;

        // Calcola la direzione del Gizmo rispetto all'origine
        Vector3 startPosition = origin.transform.position;
        Vector3 endPosition = startPosition + origin.transform.TransformDirection(spawnDirection.normalized) * gizmoLength;

        // Disegna una linea per mostrare la direzione
        Gizmos.DrawLine(startPosition, endPosition);

        // Disegna una sfera per indicare la fine della freccia
        Gizmos.DrawSphere(endPosition, 0.01f);
    }
}
