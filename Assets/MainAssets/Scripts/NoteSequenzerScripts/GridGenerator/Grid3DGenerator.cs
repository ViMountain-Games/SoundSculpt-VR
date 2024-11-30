using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;  // Aggiungi questo namespace per EditorUtility
using VInspector;

public class Grid3DGenerator : MonoBehaviour
{
    public static Grid3DGenerator Instance { get; private set; } // Variabile statica per accedere all'istanza

    [Header("Grid Settings")]
    [Min(1)] public int gridSizeX = 5;
    [Min(1)] public int gridSizeY = 5;
    [Min(1)] public int gridSizeZ = 5;
    [Range(0.01f, 10f)] public float cellSize = 1f;

    [Header("Prefab Settings")]
    [SerializeField] private GameObject cellPrefab;

    [Header("Line Renderer Settings")]
    [SerializeField] private Material lineMaterial;
    [ColorUsage(false, true)] public Color lineColor = Color.white;
    [Range(0.001f, 0.5f)] public float lineWidth = 0.05f;

    [Header("Grid Matrix")]
    [Tooltip("Visualizza gli oggetti posizionati nella griglia")]
    public GameObject[,,] gridMatrix;

    private void Awake()
    {
        // Assegna l'istanza
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogError("Un'altra istanza di Grid3DGenerator esiste già!");
        }
    }

    private void OnDestroy()
    {
        // Rimuovi l'istanza quando l'oggetto viene distrutto
        if (Instance == this)
        {
            Instance = null;
        }
    }

    [Button("Generate Grid")]
    public void GenerateGrid()
    {
        ClearGrid();

        // Inizializza la matrice 3D
        gridMatrix = new GameObject[gridSizeX, gridSizeY, gridSizeZ];

        GameObject gridParent = new GameObject("3DGrid") { transform = { parent = this.transform } };
        Vector3 origin = transform.position;

        // Disegna le linee della griglia
        DrawGridLines(origin, gridParent);

        // Posiziona il prefab e assegna il selectedObject alla matrice
        InstantiateAndAssignObjects(origin, gridParent);

        // Forza l'aggiornamento dell'Inspector
        EditorUtility.SetDirty(this);
    }

    private void DrawGridLines(Vector3 origin, GameObject gridParent)
    {
        // Disegna le linee della griglia lungo gli assi X, Y e Z
        for (int x = 0; x <= gridSizeX; x++)
        {
            for (int y = 0; y <= gridSizeY; y++)
            {
                DrawLine(
                    origin + new Vector3(x * cellSize, y * cellSize, 0),
                    origin + new Vector3(x * cellSize, y * cellSize, gridSizeZ * cellSize),
                    gridParent
                );
            }
        }

        for (int y = 0; y <= gridSizeY; y++)
        {
            for (int z = 0; z <= gridSizeZ; z++)
            {
                DrawLine(
                    origin + new Vector3(0, y * cellSize, z * cellSize),
                    origin + new Vector3(gridSizeX * cellSize, y * cellSize, z * cellSize),
                    gridParent
                );
            }
        }

        for (int x = 0; x <= gridSizeX; x++)
        {
            for (int z = 0; z <= gridSizeZ; z++)
            {
                DrawLine(
                    origin + new Vector3(x * cellSize, 0, z * cellSize),
                    origin + new Vector3(x * cellSize, gridSizeY * cellSize, z * cellSize),
                    gridParent
                );
            }
        }
    }

    private void InstantiateAndAssignObjects(Vector3 origin, GameObject gridParent)
    {
        // Posiziona i prefab e assegna gli oggetti selezionati alla matrice
        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                for (int z = 0; z < gridSizeZ; z++)
                {
                    Vector3 cellCenter = origin + new Vector3(
                        x * cellSize + cellSize / 2,
                        y * cellSize + cellSize / 2,
                        z * cellSize + cellSize / 2
                    );

                    // Instanzia il prefab
                    GameObject instance = Instantiate(cellPrefab, cellCenter, Quaternion.identity, gridParent.transform);

                    // Ottieni il NotePicker e assegna il selectedObject alla matrice
                    NotePicker picker = instance.GetComponent<NotePicker>();
                    if (picker != null && picker.selectedObject != null)
                    {
                        gridMatrix[x, y, z] = picker.selectedObject;
                    }
                    else
                    {
                        gridMatrix[x, y, z] = null;
                    }

                    // Forza l'aggiornamento dell'Inspector per ogni cella
                    EditorUtility.SetDirty(instance);
                }
            }
        }
    }

    [Button("Clear Grid")]
    public void ClearGrid()
    {
        foreach (Transform child in transform)
        {
            DestroyImmediate(child.gameObject);
        }

        gridMatrix = null;

        // Forza l'aggiornamento dell'Inspector
        EditorUtility.SetDirty(this);
    }

    private void DrawLine(Vector3 start, Vector3 end, GameObject parent)
    {
        GameObject lineObject = new GameObject("GridLine");
        lineObject.transform.parent = parent.transform;

        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
        lineRenderer.material = lineMaterial;
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
    }

    // Metodo per aggiornare la matrice manualmente, chiamato dal NotePicker o altro
    public void UpdateGridMatrix(int x, int y, int z, GameObject newObject)
    {
        // Modifica il valore nella griglia e aggiorna l'Inspector
        gridMatrix[x, y, z] = newObject;

        // Forza l'aggiornamento
        EditorUtility.SetDirty(this);
    }
}
