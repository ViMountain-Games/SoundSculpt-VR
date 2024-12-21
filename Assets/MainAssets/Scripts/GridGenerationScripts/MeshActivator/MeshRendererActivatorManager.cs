using UnityEngine;
using System.Collections.Generic;
using CustomInspector;

public class MeshRendererActivatorManager : MonoBehaviour
{
    [Header("Settings")]
    public float activationRange = 10f;
    [Tag]
    public string targetTag = "Target";
    public float updateInterval = 0.25f;

    private List<Renderer> _allCellsRenderers = new List<Renderer>();
    private List<Transform> _targets = new List<Transform>();

    public static MeshRendererActivatorManager Instance { get; private set; }

    private void Awake()
    {
        // Singleton semplice
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogError("Esiste già un MeshRendererActivatorManager nella scena!");
            Destroy(this);
            return;
        }

        // Carica i target una sola volta
        GameObject[] foundTargets = GameObject.FindGameObjectsWithTag(targetTag);
        foreach (var t in foundTargets)
        {
            if (t != null)
                _targets.Add(t.transform);
        }
    }

    private void Start()
    {
        // Richiama la funzione di update a intervalli regolari
        InvokeRepeating(nameof(UpdateCellsActivation), 0f, updateInterval);
    }

    public void RegisterCell(GameObject cell)
    {
        if (cell == null) return;

        // Se il renderer è sul prefab o sui figli, cerca in tutti i child
        Renderer[] renderers = cell.GetComponentsInChildren<Renderer>(true);
        foreach (var rend in renderers)
        {
            if (rend != null)
            {
                _allCellsRenderers.Add(rend);
            }
        }
    }


    private void UpdateCellsActivation()
    {
        // Per ogni renderer delle celle...
        for (int i = 0; i < _allCellsRenderers.Count; i++)
        {
            Renderer rend = _allCellsRenderers[i];
            if (rend == null) continue;

            Vector3 cellPos = rend.transform.position;
            bool shouldBeActive = false;

            // Controlla la distanza dai target
            for (int t = 0; t < _targets.Count; t++)
            {
                Transform target = _targets[t];
                if (target == null)
                    continue; // se un target è stato distrutto

                float dist = Vector3.Distance(cellPos, target.position);
                if (dist <= activationRange)
                {
                    shouldBeActive = true;
                    break;
                }
            }

            // Attiva/disattiva
            if (rend.enabled != shouldBeActive)
            {
                rend.enabled = shouldBeActive;
            }
        }
    }
}
