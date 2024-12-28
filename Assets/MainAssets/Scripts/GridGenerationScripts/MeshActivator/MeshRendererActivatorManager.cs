using UnityEngine;
using System.Collections.Generic;
using CustomInspector;

public class MeshRendererActivatorManager : MonoBehaviour
{
    [Header("Settings")]
    public float activationRange = 10f;

    [Tag]
    public string targetTag = "Target";

    // Intervallo per la verifica della distanza dai target
    public float updateInterval = 0.25f;

    // Intervallo per il refresh (ricerca) dei target nella scena
    public float targetRefreshInterval = 1f;

    [ReadOnly]
    public List<Renderer> _allCellsRenderers = new List<Renderer>();
    [ReadOnly]
    public List<Transform> _targets = new List<Transform>();

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
    }

    private void Start()
    {
        // Invoca la funzione di calcolo distanza a intervalli regolari
        InvokeRepeating(nameof(UpdateCellsActivation), 0f, updateInterval);

        // Invoca la funzione di refresh dei target a intervalli regolari (più lento del calcolo distanza)
        InvokeRepeating(nameof(RefreshTargets), 0f, targetRefreshInterval);
    }

    /// <summary>
    /// Aggiunge alla lista tutti i Renderer trovati nel GameObject cell (e nei suoi figli).
    /// </summary>
    public void RegisterCell(GameObject cell)
    {
        if (cell == null) return;

        Renderer[] renderers = cell.GetComponentsInChildren<Renderer>(true);
        foreach (var rend in renderers)
        {
            if (rend != null)
            {
                _allCellsRenderers.Add(rend);
            }
        }
    }

    /// <summary>
    /// Aggiorna la lista dei target usando il tag specificato.
    /// </summary>
    private void RefreshTargets()
    {
        _targets.Clear();
        GameObject[] foundTargets = GameObject.FindGameObjectsWithTag(targetTag);
        foreach (var t in foundTargets)
        {
            if (t != null)
            {
                _targets.Add(t.transform);
            }
        }
    }

    /// <summary>
    /// Verifica la distanza di ogni Renderer di cella dai target.
    /// Se un Renderer è entro activationRange da almeno un target, lo abilita, altrimenti lo disabilita.
    /// </summary>
    private void UpdateCellsActivation()
    {
        for (int i = 0; i < _allCellsRenderers.Count; i++)
        {
            Renderer rend = _allCellsRenderers[i];
            if (rend == null) continue;

            Vector3 cellPos = rend.transform.position;
            bool shouldBeActive = false;

            for (int t = 0; t < _targets.Count; t++)
            {
                Transform target = _targets[t];
                if (target == null) continue;

                float dist = Vector3.Distance(cellPos, target.position);
                if (dist <= activationRange)
                {
                    shouldBeActive = true;
                    break;
                }
            }

            if (rend.enabled != shouldBeActive)
            {
                rend.enabled = shouldBeActive;
            }
        }
    }
}
