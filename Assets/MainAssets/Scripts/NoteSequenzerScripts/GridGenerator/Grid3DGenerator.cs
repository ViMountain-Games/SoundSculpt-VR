// Grid3DGenerator.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CustomInspector;
using UnityEditor;

public class Grid3DGenerator : MonoBehaviour
{
    public static Grid3DGenerator Instance { get; private set; }

    [System.Serializable]
    public class GridEntry
    {
        public GameObject gameObject;
        public int x, y, z;

        public GridEntry(GameObject gameObject, int x, int y, int z)
        {
            this.gameObject = gameObject;
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }

    [Title("Grid Settings", fontSize = 14, alignment = TextAlignment.Center)]
    [Min(1)] public int gridSizeX = 5;
    [Min(1)] public int gridSizeY = 5;
    [Min(1)] public int gridSizeZ = 5;
    [DynamicSlider]
    public DynamicSlider cellSize = new DynamicSlider(1f, 0.1f, 5f); // Default value, min, max

    [HorizontalLine("Prefabs and Materials", 2)]
    [ForceFill] public GameObject cellPrefab;
    [ForceFill] public Material lineMaterial;

    [HorizontalLine("Visual Settings", 2)]
    [ColorPalette] public Color emptyCellColor = Color.gray;
    [ColorPalette] public Color lineColor = Color.white;
    [Range(0.001f, 0.5f)] public float lineWidth = 0.05f;

    [HorizontalLine("Grid Matrix", 2)]
    [ReadOnly] public GameObject[,,] gridMatrix;

    [HorizontalLine("Object List", 2)]
    [ReadOnly] public List<GridEntry> objectList = new List<GridEntry>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogError("Another instance of Grid3DGenerator already exists!");
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void GenerateGrid()
    {
        ClearGrid();

        gridMatrix = new GameObject[gridSizeX, gridSizeY, gridSizeZ];
        GameObject gridParent = new GameObject("3DGrid") { transform = { parent = this.transform } };
        Vector3 origin = transform.position;

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

                    GameObject instance = Instantiate(cellPrefab, cellCenter, Quaternion.identity, gridParent.transform);
                    gridMatrix[x, y, z] = null; // Default to empty
                }
            }
        }

        // Draw grid lines
        DrawGridLines(origin, gridParent);

        EditorUtility.SetDirty(this);
    }

    public void ClearGrid()
    {
        foreach (Transform child in transform)
        {
            DestroyImmediate(child.gameObject);
        }

        gridMatrix = null;
        objectList.Clear();
        EditorUtility.SetDirty(this);
    }

    public void UpdateGridMatrix(int x, int y, int z, GameObject newObject)
    {
        if (gridMatrix == null)
        {
            Debug.LogError("GridMatrix is not initialized. Ensure you call GenerateGrid before updating the matrix.");
            return;
        }

        if (x < 0 || x >= gridSizeX || y < 0 || y >= gridSizeY || z < 0 || z >= gridSizeZ)
        {
            Debug.LogError($"Invalid indices: ({x}, {y}, {z}). Ensure they are within the grid bounds.");
            return;
        }

        // Assign the new object to the grid matrix
        gridMatrix[x, y, z] = newObject;

        // Update the objectList
        // First, remove any existing entries at the same coordinates
        objectList.RemoveAll(entry => entry.x == x && entry.y == y && entry.z == z);

        if (newObject != null)
        {
            // Add new entry
            GridEntry newEntry = new GridEntry(newObject, x, y, z);
            objectList.Add(newEntry);
        }

        // Sort the list based on x, then z, then y
        objectList.Sort((a, b) =>
        {
            int xComparison = a.x.CompareTo(b.x);
            if (xComparison != 0) return xComparison;

            int zComparison = a.z.CompareTo(b.z);
            if (zComparison != 0) return zComparison;

            return a.y.CompareTo(b.y);
        });

        EditorUtility.SetDirty(this);
    }

    private void DrawGridLines(Vector3 origin, GameObject parent)
    {
        for (int x = 0; x <= gridSizeX; x++)
        {
            for (int y = 0; y <= gridSizeY; y++)
            {
                DrawLine(
                    origin + new Vector3(x * cellSize, y * cellSize, 0),
                    origin + new Vector3(x * cellSize, y * cellSize, gridSizeZ * cellSize),
                    parent
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
                    parent
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
                    parent
                );
            }
        }
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
