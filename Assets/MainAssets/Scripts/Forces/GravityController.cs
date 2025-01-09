using UnityEngine;
using System.Collections.Generic;
using CustomInspector;

public class GravityController : MonoBehaviour
{
    [Header("Configurazione")]
    [Tooltip("Tag degli oggetti da monitorare")]
    [Tag]
    public string targetTag = "YourTag";

    [Tooltip("Distanza massima per attivare/disattivare la gravità")]
    public float maxDistance = 10f;

    [Tooltip("Intervallo di aggiornamento in secondi")]
    public float updateInterval = 0.5f;

    // Lista per tenere traccia degli oggetti monitorati (Rigidbody)
    [ReadOnly]
    public List<Rigidbody> monitoredObjects = new List<Rigidbody>();

    // Dizionario per tracciare i dati associati a ciascun Rigidbody
    private Dictionary<Rigidbody, MonitoredObjectData> monitoredObjectsData = new Dictionary<Rigidbody, MonitoredObjectData>();

    private float lastUpdateTime = 0f;

    [System.Serializable]
    public class MonitoredObjectData
    {
        [ReadOnly] public float distance;
        [ReadOnly] public bool isInside;

        public MonitoredObjectData(float distance, bool isInside)
        {
            this.distance = distance;
            this.isInside = isInside;
        }
    }

    void Start()
    {
        // Trova subito tutti i GameObject con il tag desiderato
        GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(targetTag);
        foreach (GameObject obj in taggedObjects)
        {
            Rigidbody rb = obj.GetComponentInParent<Rigidbody>();
            if (rb != null && !monitoredObjects.Contains(rb))
            {
                monitoredObjects.Add(rb);
                monitoredObjectsData[rb] = new MonitoredObjectData(Vector3.Distance(transform.position, rb.transform.position), false);
            }
        }
    }

    void Update()
    {
        if (Time.time >= lastUpdateTime + updateInterval)
        {
            lastUpdateTime = Time.time;
            UpdateGravityStates();
        }
    }

    void UpdateGravityStates()
    {
        foreach (Rigidbody rb in monitoredObjects)
        {
            if (rb == null) continue;

            // Calcoliamo la distanza e lo stato
            float distance = Vector3.Distance(transform.position, rb.transform.position);
            bool isInside = distance <= maxDistance;

            // Aggiorniamo il dizionario
            MonitoredObjectData data = monitoredObjectsData[rb];
            data.distance = distance;
            data.isInside = isInside;

            // Attiviamo/disattiviamo la gravità
            if (isInside)
            {
                if (!rb.useGravity)
                {
                    rb.useGravity = true;
                }
            }
            else
            {
                if (rb.useGravity)
                {
                    rb.useGravity = false;
                }
            }
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Disegna il raggio principale
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, maxDistance);

        // Disegna una piccola sfera per ogni oggetto monitorato
        foreach (var entry in monitoredObjectsData)
        {
            Rigidbody rb = entry.Key;
            MonitoredObjectData data = entry.Value;

            if (rb == null) continue;

            Gizmos.color = data.isInside ? Color.blue : Color.red;
            Gizmos.DrawSphere(rb.transform.position, 0.2f);
        }
    }
}
#endif