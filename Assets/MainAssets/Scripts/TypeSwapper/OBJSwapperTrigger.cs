using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Mappa il nome di un oggetto "root" (originalObjectName)
/// con il prefab da instanziare (prefabToSpawn).
/// </summary>
[System.Serializable]
public class ObjectPrefabPair
{
    [Tooltip("Nome esatto dell'oggetto 'root' da sostituire.")]
    public string originalObjectName;

    [Tooltip("Prefab da instanziare in sostituzione.")]
    public GameObject prefabToSpawn;
}

public class OBJSwapperTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    [Tooltip("Se true, avviene lo swap quando l'oggetto entra nel trigger.")]
    public bool enableSwap = true;

    [Tooltip("Riferimento all'altro trigger per evitare doppio swap simultaneo.")]
    public OBJSwapperTrigger otherTrigger;

    [Header("Object-Prefab Mapping")]
    [Tooltip("Lista di coppie (Nome Oggetto, Prefab).")]
    public List<ObjectPrefabPair> objectPrefabPairs = new List<ObjectPrefabPair>();

    [Header("Delays")]
    [Tooltip("Tempo di attesa (in secondi) prima di distruggere l'oggetto.")]
    public float delayBeforeDestroy = 1f;

    [Tooltip("Tempo di attesa (in secondi) prima di istanziare il prefab dopo aver distrutto l'oggetto.")]
    public float delayBeforeInstantiate = 0.5f;

    [Header("Force Settings")]
    [Tooltip("Oggetto da cui prendere posizione e direzione Up per l'AddForce.")]
    public GameObject forceTarget;

    [Tooltip("Intensità della forza (impulso).")]
    public float forceMagnitude = 5f;

    [Header("Events")]
    [Tooltip("Chiamato quando l'oggetto entra nel trigger con 'enableSwap' = true (prima del delay).")]
    public UnityEvent onObjectEnteredWithSwapEnabled;

    [Tooltip("Chiamato quando il prefab viene effettivamente istanziato.")]
    public UnityEvent onPrefabInstantiated;

    /// <summary>
    /// Dimensione iniziale di ogni pool.
    /// </summary>
    [Header("Pooling Settings")]
    [Tooltip("Numero di istanze iniziali per ciascun prefab.")]
    public int initialPoolSize = 5;

    /// <summary>
    /// Dizionario che mappa ciascun prefab a una coda di oggetti (pool).
    /// </summary>
    private Dictionary<GameObject, Queue<GameObject>> poolDictionary;

    // Oggetti attualmente dentro questo trigger (riferimento ai "root objects")
    private HashSet<GameObject> objectsInside = new HashSet<GameObject>();

    // Per ogni oggetto (root), teniamo traccia se è già stato swappato (o è in corso di swap)
    private Dictionary<GameObject, bool> objectsSwapped = new Dictionary<GameObject, bool>();

    private void Awake()
    {
        InitializePool();
    }

    /// <summary>
    /// Inizializza il dizionario dei pool per tutti i prefabToSpawn presenti in objectPrefabPairs.
    /// </summary>
    private void InitializePool()
    {
        poolDictionary = new Dictionary<GameObject, Queue<GameObject>>();

        foreach (var pair in objectPrefabPairs)
        {
            // Se il prefab è valido e non è già presente nel dizionario, creiamo una nuova coda
            if (pair.prefabToSpawn != null && !poolDictionary.ContainsKey(pair.prefabToSpawn))
            {
                Queue<GameObject> newPool = new Queue<GameObject>();

                // Istanzio un certo numero di oggetti e li disattivo
                for (int i = 0; i < initialPoolSize; i++)
                {
                    GameObject obj = Instantiate(pair.prefabToSpawn);
                    obj.SetActive(false);
                    newPool.Enqueue(obj);
                }

                poolDictionary.Add(pair.prefabToSpawn, newPool);
            }
        }
    }

    /// <summary>
    /// Restituisce un oggetto da un pool specifico. Se il pool è vuoto, ne viene instanziato uno nuovo.
    /// </summary>
    private GameObject GetPooledObject(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(prefab))
        {
            Debug.LogWarning($"[OBJSwapperTrigger] Nessun pool per il prefab: {prefab.name}. " +
                             "Lo istanzio al volo senza pooling.");
            return Instantiate(prefab, position, rotation);
        }

        Queue<GameObject> pool = poolDictionary[prefab];

        // Se il pool è vuoto, istanziamo un nuovo oggetto
        if (pool.Count == 0)
        {
            GameObject newObj = Instantiate(prefab, position, rotation);
            return newObj;
        }

        // Altrimenti recuperiamo il primo disponibile
        GameObject pooledObj = pool.Dequeue();

        // Settiamo posizione e rotazione, attiviamolo e lo restituiamo
        pooledObj.transform.position = position;
        pooledObj.transform.rotation = rotation;
        pooledObj.SetActive(true);

        return pooledObj;
    }

    /// <summary>
    /// Restituisce un oggetto al pool, se presente nel dizionario.
    /// </summary>
    private void ReturnToPool(GameObject prefab, GameObject instance)
    {
        if (!poolDictionary.ContainsKey(prefab))
        {
            // Non gestiamo il ritorno al pool di prefab sconosciuti,
            // eventualmente distruggiamo
            Destroy(instance);
            return;
        }

        instance.SetActive(false);
        poolDictionary[prefab].Enqueue(instance);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Facciamo sempre riferimento al root per evitare duplicazioni dovute a child-colliders
        GameObject rootObject = other.transform.root.gameObject;

        if (!objectsInside.Contains(rootObject))
        {
            objectsInside.Add(rootObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Facciamo sempre riferimento al root per evitare duplicazioni
        GameObject rootObject = other.transform.root.gameObject;

        if (objectsInside.Contains(rootObject))
        {
            objectsInside.Remove(rootObject);
        }

        // Pulizia: se era in objectsSwapped, rimuovilo
        if (objectsSwapped.ContainsKey(rootObject))
        {
            objectsSwapped.Remove(rootObject);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // Se lo swap è disabilitato, esci
        if (!enableSwap) return;

        // Sempre root object
        GameObject rootObject = other.transform.root.gameObject;

        // Se l'oggetto non è effettivamente dentro il mio trigger, ignora
        if (!objectsInside.Contains(rootObject)) return;

        // Se esiste un altro trigger e l'oggetto è dentro anche l'altro, non swappare
        if (otherTrigger != null && otherTrigger.IsObjectInside(rootObject))
        {
            return;
        }

        // Verifichiamo se è già in fase di swap
        bool alreadySwapped;
        objectsSwapped.TryGetValue(rootObject, out alreadySwapped);

        // Se non è ancora stato swappato (o in elaborazione), avviamo la procedura
        if (!alreadySwapped)
        {
            // Segniamo subito come "in corso di swap"
            objectsSwapped[rootObject] = true;

            // Avviamo la coroutine di swap
            StartCoroutine(SwapCoroutine(rootObject));
        }
    }

    /// <summary>
    /// Coroutine che gestisce il flusso di distruzione e istanziazione dopo i delay.
    /// </summary>
    private IEnumerator SwapCoroutine(GameObject rootObject)
    {
        // 1) Invoke dell'evento "onObjectEnteredWithSwapEnabled" prima del delay
        onObjectEnteredWithSwapEnabled?.Invoke();

        // 2) Attendi il delay prima di distruggere l'oggetto
        yield return new WaitForSeconds(delayBeforeDestroy);

        if (rootObject == null) yield break;

        string rootName = rootObject.name;

        // Cerchiamo nella lista la coppia corrispondente
        ObjectPrefabPair pairFound = objectPrefabPairs.Find(pair => pair.originalObjectName == rootName);

        if (pairFound != null && pairFound.prefabToSpawn != null)
        {
            // Distruggiamo l'oggetto originale
            Destroy(rootObject);

            // 3) Attendi il delay prima di instanziare il prefab
            yield return new WaitForSeconds(delayBeforeInstantiate);

            // Se lo distruggi e l'oggetto root non esiste più, pazienza, non facciamo nulla
            if (forceTarget == null && rootObject == null) 
            {
                yield break;
            }

            // 4) Istanziamo (o recuperiamo dal pool) il prefab
            Vector3 spawnPosition = (forceTarget != null)
                ? forceTarget.transform.position
                : Vector3.zero; // se non hai una posizione di default, definiscila qui

            Quaternion spawnRotation = (forceTarget != null)
                ? forceTarget.transform.rotation
                : Quaternion.identity; // idem come sopra

            GameObject newObj = GetPooledObject(pairFound.prefabToSpawn, spawnPosition, spawnRotation);

            // Applichiamo una forza se il prefab ha un Rigidbody
            if (forceTarget != null)
            {
                Rigidbody rb = newObj.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Vector3 forceDirection = forceTarget.transform.up;
                    rb.AddForce(forceDirection * forceMagnitude, ForceMode.Impulse);
                }
            }

            // 5) Invoke dell'evento "onPrefabInstantiated" appena istanziato il prefab
            onPrefabInstantiated?.Invoke();
        }
    }

    /// <summary>
    /// Restituisce true se il root object specificato è dentro questo trigger.
    /// </summary>
    public bool IsObjectInside(GameObject rootObject)
    {
        return objectsInside.Contains(rootObject);
    }

    private void OnDrawGizmos()
    {
        // Salviamo il colore originale
        Color originalColor = Gizmos.color;

        // Impostiamo il colore: verde se enableSwap è true, rosso altrimenti
        Gizmos.color = enableSwap ? Color.green : Color.red;

        // Disegniamo il collider come Gizmo
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;

            if (col is BoxCollider boxCol)
            {
                Gizmos.DrawWireCube(boxCol.center, boxCol.size);
            }
            else if (col is SphereCollider sphereCol)
            {
                Gizmos.DrawWireSphere(sphereCol.center, sphereCol.radius);
            }
            else if (col is CapsuleCollider capsuleCol)
            {
                float radius = capsuleCol.radius;
                float height = Mathf.Max(0, capsuleCol.height - 2 * radius);

                Vector3 center = capsuleCol.center;
                Vector3 up = Vector3.up * (height * 0.5f);

                // Disegniamo le due semisfere
                Gizmos.DrawWireSphere(center + up, radius);
                Gizmos.DrawWireSphere(center - up, radius);

                // Connettiamo le due sfere con linee per simulare il cilindro
                Gizmos.DrawLine(center + up + Vector3.forward * radius, center - up + Vector3.forward * radius);
                Gizmos.DrawLine(center + up + Vector3.back * radius, center - up + Vector3.back * radius);
                Gizmos.DrawLine(center + up + Vector3.left * radius, center - up + Vector3.left * radius);
                Gizmos.DrawLine(center + up + Vector3.right * radius, center - up + Vector3.right * radius);
            }
        }

        // Ripristiniamo il colore originale
        Gizmos.color = originalColor;
    }
}
