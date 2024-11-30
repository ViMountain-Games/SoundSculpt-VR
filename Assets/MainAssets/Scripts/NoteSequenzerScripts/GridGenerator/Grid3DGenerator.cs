using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CustomInspector;
using UnityEditor;

public class Grid3DGenerator : MonoBehaviour
{
    public static Grid3DGenerator Instance { get; private set; }

    [Title("Grid Settings", fontSize = 14, alignment = TextAlignment.Center)]
    [Min(1)] public int gridSizeX = 5;
    [Min(1)] public int gridSizeY = 5;
    [Min(1)] public int gridSizeZ = 5;
    [DynamicSlider] public float cellSize = 1f;

    [HorizontalLine("Prefabs and Materials", 2)]
    [ForceFill] public GameObject cellPrefab;
    [ForceFill] public Material lineMaterial;

    [HorizontalLine("Visual Settings", 2)]
    [ColorPalette] public Color emptyCellColor = Color.gray;
    [ColorPalette] public Color filledCellColor = Color.blue;
    [ColorPalette] public Color lineColor = Color.white;
    [Range(0.001f, 0.5f)] public float lineWidth = 0.05f;

    [HorizontalLine("Grid Matrix", 2)]
    [ReadOnly] public GameObject[,,] gridMatrix;

    private void Awake()
    {
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

        // Disegna le linee della griglia
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
        EditorUtility.SetDirty(this);
    }

    public void UpdateGridMatrix(int x, int y, int z, GameObject newObject)
    {
        gridMatrix[x, y, z] = newObject;
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
