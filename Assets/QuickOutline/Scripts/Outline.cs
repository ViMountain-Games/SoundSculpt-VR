//
//  Outline.cs
//  QuickOutline
//
//  Created by Chris Nolet on 3/30/18.
//  Modified by [Il Tuo Nome] on [Data].

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public class Outline : MonoBehaviour
{
    // HashSet per tracciare le mesh registrate e prevenire duplicati
    private static HashSet<Mesh> registeredMeshes = new HashSet<Mesh>();

    // Modalità di outline disponibili
    public enum Mode
    {
        OutlineAll,
        OutlineVisible,
        OutlineHidden,
        OutlineAndSilhouette,
        SilhouetteOnly
    }

    // Proprietà per gestire la modalità di outline
    public Mode OutlineMode
    {
        get { return outlineMode; }
        set
        {
            outlineMode = value;
            needsUpdate = true;
        }
    }

    // Proprietà per il colore dell'outline
    public Color OutlineColor
    {
        get { return outlineColor; }
        set
        {
            outlineColor = value;
            needsUpdate = true;
        }
    }

    // Proprietà per la larghezza dell'outline
    public float OutlineWidth
    {
        get { return outlineWidth; }
        set
        {
            outlineWidth = value;
            needsUpdate = true;
        }
    }

    // Classe serializzabile per memorizzare le normali smooth
    [Serializable]
    private class ListVector3
    {
        public List<Vector3> data;
    }

    // Variabili serializzate per configurare l'outline
    [SerializeField]
    private Mode outlineMode;

    [SerializeField]
    private Color outlineColor = Color.white;

    [SerializeField, Range(0f, 10f)]
    private float outlineWidth = 2f;

    [Header("Smooth Transition Settings")]
    [Tooltip("Velocità del cambiamento dell'Outline Width. Un valore maggiore indica un cambiamento più rapido.")]
    public float changeSpeed = 1f; // Velocità di cambiamento del valore
    private Coroutine outlineWidthCoroutine; // Coroutine per gestire il cambio graduale

    [Header("Optional")]
    [SerializeField, Tooltip("Precompute enabled: I calcoli per l'outline sono eseguiti nell'editor e serializzati con l'oggetto. "
        + "Precompute disabled: I calcoli per l'outline sono eseguiti a runtime in Awake(). Questo può causare un ritardo per mesh grandi.")]
    private bool precomputeOutline;

    [SerializeField, HideInInspector]
    private List<Mesh> bakeKeys = new List<Mesh>();

    [SerializeField, HideInInspector]
    private List<ListVector3> bakeValues = new List<ListVector3>();

    // Riferimenti ai renderer e ai materiali di outline
    private Renderer[] renderers;
    private Material outlineMaskMaterial;
    private Material outlineFillMaterial;

    private bool needsUpdate;

    void Awake()
    {
        // Cache dei renderer figli
        renderers = GetComponentsInChildren<Renderer>();

        // Instanzia i materiali di outline
        outlineMaskMaterial = Instantiate(Resources.Load<Material>(@"Materials/OutlineMask"));
        outlineFillMaterial = Instantiate(Resources.Load<Material>(@"Materials/OutlineFill"));

        outlineMaskMaterial.name = "OutlineMask (Instance)";
        outlineFillMaterial.name = "OutlineFill (Instance)";

        // Recupera o genera le normali smooth
        LoadSmoothNormals();

        // Applica immediatamente le proprietà del materiale
        needsUpdate = true;
    }

    void OnEnable()
    {
        foreach (var renderer in renderers)
        {
            // Aggiunge i materiali di outline alla lista dei materiali del renderer
            var materials = renderer.sharedMaterials.ToList();
            materials.Add(outlineMaskMaterial);
            materials.Add(outlineFillMaterial);
            renderer.materials = materials.ToArray();
        }
    }

    void OnValidate()
    {
        // Segnala che è necessario aggiornare le proprietà del materiale
        needsUpdate = true;

        // Cancella la cache se il precompute è disabilitato o se la cache è corrotta
        if (!precomputeOutline && bakeKeys.Count != 0 || bakeKeys.Count != bakeValues.Count)
        {
            bakeKeys.Clear();
            bakeValues.Clear();
        }

        // Esegue il bake se il precompute è abilitato e non sono presenti dati di bake
        if (precomputeOutline && bakeKeys.Count == 0)
        {
            Bake();
        }
    }

    void Update()
    {
        if (needsUpdate)
        {
            needsUpdate = false;
            UpdateMaterialProperties();
        }
    }

    void OnDisable()
    {
        foreach (var renderer in renderers)
        {
            // Rimuove i materiali di outline dalla lista dei materiali del renderer
            var materials = renderer.sharedMaterials.ToList();
            materials.Remove(outlineMaskMaterial);
            materials.Remove(outlineFillMaterial);
            renderer.materials = materials.ToArray();
        }
    }

    void OnDestroy()
    {
        // Distrugge le istanze dei materiali di outline
        Destroy(outlineMaskMaterial);
        Destroy(outlineFillMaterial);
    }

    /// <summary>
    /// Esegue il bake delle normali smooth per tutte le mesh figlie.
    /// Questo metodo è chiamato solo se precomputeOutline è abilitato.
    /// </summary>
    void Bake()
    {
        // HashSet per tracciare le mesh già processate
        var bakedMeshes = new HashSet<Mesh>();

        foreach (var meshFilter in GetComponentsInChildren<MeshFilter>())
        {
            // Salta le mesh duplicate
            if (!bakedMeshes.Add(meshFilter.sharedMesh))
            {
                continue;
            }

            // Calcola le normali smooth
            var smoothNormals = SmoothNormals(meshFilter.sharedMesh);

            // Memorizza le normali smooth nella cache
            bakeKeys.Add(meshFilter.sharedMesh);
            bakeValues.Add(new ListVector3() { data = smoothNormals });
        }
    }

    /// <summary>
    /// Carica le normali smooth, precompute se abilitato, altrimenti le calcola a runtime.
    /// </summary>
    void LoadSmoothNormals()
    {
        foreach (var meshFilter in GetComponentsInChildren<MeshFilter>())
        {
            // Salta le mesh già registrate
            if (!registeredMeshes.Add(meshFilter.sharedMesh))
            {
                continue;
            }

            // Cerca nella cache se le normali sono state precompute
            var index = bakeKeys.IndexOf(meshFilter.sharedMesh);
            var smoothNormals = (index >= 0) ? bakeValues[index].data : SmoothNormals(meshFilter.sharedMesh);

            // Memorizza le normali smooth in UV3 (se supportato)
            meshFilter.sharedMesh.SetUVs(3, smoothNormals);

            // Combina i submeshes se necessario
            var renderer = meshFilter.GetComponent<Renderer>();
            if (renderer != null)
            {
                CombineSubmeshes(meshFilter.sharedMesh, renderer.sharedMaterials);
            }
        }

        foreach (var skinnedMeshRenderer in GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            if (!registeredMeshes.Add(skinnedMeshRenderer.sharedMesh))
            {
                continue;
            }

            // Pulisce UV4 per i skinned mesh renderer
            skinnedMeshRenderer.sharedMesh.uv4 = new Vector2[skinnedMeshRenderer.sharedMesh.vertexCount];

            // Combina i submeshes se necessario
            CombineSubmeshes(skinnedMeshRenderer.sharedMesh, skinnedMeshRenderer.sharedMaterials);
        }
    }

    /// <summary>
    /// Calcola le normali smooth per una mesh data.
    /// </summary>
    /// <param name="mesh">La mesh per cui calcolare le normali smooth.</param>
    /// <returns>Lista di normali smooth.</returns>
    List<Vector3> SmoothNormals(Mesh mesh)
    {
        // Raggruppa i vertici per posizione
        var groups = mesh.vertices.Select((vertex, index) => new KeyValuePair<Vector3, int>(vertex, index)).GroupBy(pair => pair.Key);

        // Copia le normali in una nuova lista
        var smoothNormals = new List<Vector3>(mesh.normals);

        // Media le normali per i vertici raggruppati
        foreach (var group in groups)
        {
            // Salta i gruppi con un solo vertice
            if (group.Count() == 1)
            {
                continue;
            }

            // Calcola la normale media
            var smoothNormal = Vector3.zero;
            foreach (var pair in group)
            {
                smoothNormal += smoothNormals[pair.Value];
            }
            smoothNormal.Normalize();

            // Assegna la normale media a ciascun vertice del gruppo
            foreach (var pair in group)
            {
                smoothNormals[pair.Value] = smoothNormal;
            }
        }

        return smoothNormals;
    }

    /// <summary>
    /// Combina i submeshes di una mesh se il numero di submeshes supera il numero di materiali.
    /// </summary>
    /// <param name="mesh">La mesh da processare.</param>
    /// <param name="materials">Materiali associati alla mesh.</param>
    void CombineSubmeshes(Mesh mesh, Material[] materials)
    {
        // Salta le mesh con un solo submesh
        if (mesh.subMeshCount == 1)
        {
            return;
        }

        // Salta se il numero di submeshes supera il numero di materiali
        if (mesh.subMeshCount > materials.Length)
        {
            return;
        }

        // Aggiunge un submesh combinato
        mesh.subMeshCount++;
        mesh.SetTriangles(mesh.triangles, mesh.subMeshCount - 1);
    }

    /// <summary>
    /// Aggiorna le proprietà dei materiali di outline in base alla modalità selezionata.
    /// </summary>
    void UpdateMaterialProperties()
    {
        // Imposta il colore dell'outline
        outlineFillMaterial.SetColor("_OutlineColor", outlineColor);

        // Configura le proprietà dei materiali in base alla modalità selezionata
        switch (outlineMode)
        {
            case Mode.OutlineAll:
                outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
                outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
                outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
                break;

            case Mode.OutlineVisible:
                outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
                outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
                outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
                break;

            case Mode.OutlineHidden:
                outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
                outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Greater);
                outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
                break;

            case Mode.OutlineAndSilhouette:
                outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
                outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
                outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
                break;

            case Mode.SilhouetteOnly:
                outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
                outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Greater);
                outlineFillMaterial.SetFloat("_OutlineWidth", 0f);
                break;
        }
    }

    /// <summary>
    /// Avvia una transizione graduale per cambiare la larghezza dell'outline.
    /// </summary>
    /// <param name="targetWidth">La larghezza target dell'outline.</param>
    public void SmoothChangeOutlineWidth(float targetWidth)
    {
        if (outlineWidthCoroutine != null)
        {
            StopCoroutine(outlineWidthCoroutine);
        }
        outlineWidthCoroutine = StartCoroutine(ChangeOutlineWidthRoutine(targetWidth));
    }

    /// <summary>
    /// Coroutine che gestisce il cambio graduale della larghezza dell'outline.
    /// </summary>
    /// <param name="targetWidth">La larghezza target dell'outline.</param>
    /// <returns>Yield instruction.</returns>
    private IEnumerator ChangeOutlineWidthRoutine(float targetWidth)
    {
        float startWidth = outlineWidth;
        float elapsed = 0f;
        float duration = 1f / changeSpeed; // Durata basata sulla velocità di cambiamento

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            outlineWidth = Mathf.Lerp(startWidth, targetWidth, t);
            needsUpdate = true;
            yield return null;
        }

        outlineWidth = targetWidth;
        needsUpdate = true;
    }
}
