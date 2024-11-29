using UnityEngine;
using VInspector;

public class Grid3DGenerator : MonoBehaviour
{
    [Header("Grid Settings")]
    [Min(1)] public int gridSizeX = 5; // Numero di celle lungo l'asse X
    [Min(1)] public int gridSizeY = 5; // Numero di celle lungo l'asse Y
    [Min(1)] public int gridSizeZ = 5; // Numero di celle lungo l'asse Z
    [Range(0.01f, 10f)] public float cellSize = 1f; // Dimensione di ciascuna cella

    [Header("Prefab Settings")]
    [SerializeField] private GameObject cellPrefab; // Prefab da posizionare al centro di ogni cella

    [Header("Line Renderer Settings")]
    [SerializeField] private Material lineMaterial; // Materiale per il LineRenderer
    [ColorUsage(false, true)] public Color lineColor = Color.white; // Colore delle linee
    [Range(0.001f, 0.5f)] public float lineWidth = 0.05f; // Spessore delle linee

    [Header("Grid Matrix")]
    [Tooltip("Visualizza gli oggetti posizionati nella griglia")]
    public GameObject[,,] gridMatrix; // Matrice 3D per gli oggetti

    [Button("Generate Grid")]
    public void GenerateGrid()
    {
        ClearGrid();

        // Inizializza la matrice 3D
        gridMatrix = new GameObject[gridSizeX, gridSizeY, gridSizeZ];

        GameObject gridParent = new GameObject("3DGrid") { transform = { parent = this.transform } };
        Vector3 origin = transform.position;

        // Disegna le linee della griglia
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

        // Posiziona il prefab al centro di ogni cella
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

                    if (cellPrefab != null)
                    {
                        GameObject instance = Instantiate(cellPrefab, cellCenter, Quaternion.identity, gridParent.transform);
                        gridMatrix[x, y, z] = instance; // Salva l'istanza nella matrice
                    }
                }
            }
        }
    }

    [Button("Clear Grid")]
    public void ClearGrid()
    {
        // Elimina tutti i figli e resetta la matrice
        foreach (Transform child in transform)
        {
            DestroyImmediate(child.gameObject);
        }

        gridMatrix = null;
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
}
