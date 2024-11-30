// Grid3DGenerator.cs
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

        // If the new object is not null, try to apply the NoteData color
        if (newObject != null)
        {
            Note noteComponent = newObject.GetComponent<Note>();
            if (noteComponent != null && noteComponent.noteData != null)
            {
                //Debug.Log($"Assigned object with color {noteComponent.noteData.color} to grid at ({x}, {y}, {z}).");
            }
            else
            {
                //Debug.LogWarning($"The object at ({x}, {y}, {z}) does not have a valid Note or NoteData. No color applied.");
            }
        }
        else
        {
            Debug.Log($"Grid cell at ({x}, {y}, {z}) set to empty.");
        }

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
