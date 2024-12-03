using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CustomInspector;
using TMPro; // For TextMeshPro
using UnityEditor;

[DefaultExecutionOrder(-100)] // Ensure this script runs before others
public class Grid3DGenerator : MonoBehaviour
{
    public static Grid3DGenerator Instance { get; private set; }

    // Updated NoteName enum with correct notes
    public enum NoteName
    {
        Do,
        Re,
        Mi,
        Fa,
        Sol,
        La,
        Si,
        DoSharp,   // Do♯
        ReSharp,   // Re♯
        FaSharp,   // Fa♯
        SolSharp,  // Sol♯
        LaSharp,   // La♯
        ReFlat,    // Re♭
        MiFlat,    // Mi♭
        SolFlat,   // Sol♭
        LaFlat,    // La♭
        SiFlat     // Si♭
    }

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

    [System.Serializable]
    public class OctaveMapping
    {
        public int zValue;
        public int octave;
    }

    [System.Serializable]
    public class NoteMapping
    {
        public int yValue;
        public NoteName noteName;
    }

    [System.Serializable]
    public class LabelSettings
    {
        public float offsetX = -0.5f;
        public float textSize = 1f;
        public Color textColor = Color.white;
    }

    [Title("Grid Settings", fontSize = 14, alignment = TextAlignment.Center)]
    [Min(1)] public int gridSizeX = 5;
    [Min(1)] public int gridSizeY = 17; // Adjusted to fit all notes
    [Min(1)] public int gridSizeZ = 3;
    [DynamicSlider]
    public DynamicSlider cellSize = new DynamicSlider(1f, 0.1f, 5f);

    [HorizontalLine("Prefabs and Materials", 2)]
    [ForceFill] public GameObject cellPrefab;
    [ForceFill] public Material lineMaterial;
    [ForceFill] public GameObject labelPrefab;

    [Header("Label Settings")]
    public LabelSettings labelSettings = new LabelSettings();

    [HorizontalLine("Visual Settings", 2)]
    [ColorPalette] public Color emptyCellColor = Color.gray;
    [ColorPalette] public Color lineColor = Color.white;
    [Range(0.001f, 0.5f)] public float lineWidth = 0.05f;

    [HorizontalLine("Grid Matrix", 2)]
    [ReadOnly] public GameObject[,,] gridMatrix;

    [HorizontalLine("Object List", 2)]
    [ReadOnly] public List<GridEntry> objectList = new List<GridEntry>();

    [Header("Octave Mappings")]
    [SerializeField]
    public List<OctaveMapping> octaveMappings = new List<OctaveMapping>();

    [Header("Default Octave Settings")]
    [Min(1)] public int defaultOctave = 2;

    [Header("Note Mappings")]
    [SerializeField]
    public List<NoteMapping> noteMappings = new List<NoteMapping>();

    private GameObject labelsParent;

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
        labelsParent = new GameObject("Labels") { transform = { parent = this.transform } };
        Vector3 origin = transform.position;

        // Generate grid cells
        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                for (int z = 0; z < gridSizeZ; z++)
                {
                    Vector3 cellCenter = origin + new Vector3(
                        x * cellSize.value + cellSize.value / 2,
                        y * cellSize.value + cellSize.value / 2,
                        z * cellSize.value + cellSize.value / 2
                    );

                    Instantiate(cellPrefab, cellCenter, Quaternion.identity, gridParent.transform);
                    gridMatrix[x, y, z] = null; // Default to empty
                }
            }
        }

        // Generate labels to the left of the grid
        float gridDepth = gridSizeZ * cellSize.value;

        for (int y = 0; y < gridSizeY; y++)
        {
            NoteName noteName = GetNoteNameFromY(y);

            Vector3 labelPosition = origin + new Vector3(
                labelSettings.offsetX * cellSize.value,
                y * cellSize.value + cellSize.value / 2,
                gridDepth / 2
            );

            GameObject labelInstance = Instantiate(labelPrefab, labelPosition, Quaternion.identity, labelsParent.transform);

            TextMeshPro tmp = labelInstance.GetComponentInChildren<TextMeshPro>();
            if (tmp != null)
            {
                tmp.text = FormatNoteName(noteName);
                tmp.fontSize = labelSettings.textSize;
                tmp.color = labelSettings.textColor;
            }
            else
            {
                Debug.LogError("Label prefab does not have a TextMeshPro component in its children.");
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

        if (labelsParent != null)
        {
            DestroyImmediate(labelsParent);
            labelsParent = null;
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
                    origin + new Vector3(x * cellSize.value, y * cellSize.value, 0),
                    origin + new Vector3(x * cellSize.value, y * cellSize.value, gridSizeZ * cellSize.value),
                    parent
                );
            }
        }

        for (int y = 0; y <= gridSizeY; y++)
        {
            for (int z = 0; z <= gridSizeZ; z++)
            {
                DrawLine(
                    origin + new Vector3(0, y * cellSize.value, z * cellSize.value),
                    origin + new Vector3(gridSizeX * cellSize.value, y * cellSize.value, z * cellSize.value),
                    parent
                );
            }
        }

        for (int x = 0; x <= gridSizeX; x++)
        {
            for (int z = 0; z <= gridSizeZ; z++)
            {
                DrawLine(
                    origin + new Vector3(x * cellSize.value, 0, z * cellSize.value),
                    origin + new Vector3(x * cellSize.value, gridSizeY * cellSize.value, z * cellSize.value),
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

    private string FormatNoteName(NoteName noteName)
    {
        return noteName switch
        {
            NoteName.DoSharp => "Do♯",
            NoteName.ReSharp => "Re♯",
            NoteName.FaSharp => "Fa♯",
            NoteName.SolSharp => "Sol♯",
            NoteName.LaSharp => "La♯",
            NoteName.ReFlat => "Reb",
            NoteName.MiFlat => "Mib",
            NoteName.SolFlat => "Solb",
            NoteName.LaFlat => "Lab",
            NoteName.SiFlat => "Sib",
            _ => noteName.ToString()
        };
    }

    public int GetOctaveFromZ(int z)
    {
        foreach (var mapping in octaveMappings)
        {
            if (mapping.zValue == z)
            {
                return mapping.octave;
            }
        }

        Debug.LogWarning($"No octave mapping found for z: {z}. Using default octave {defaultOctave}.");
        return defaultOctave;
    }

    public NoteName GetNoteNameFromY(int y)
    {
        foreach (var mapping in noteMappings)
        {
            if (mapping.yValue == y)
            {
                return mapping.noteName;
            }
        }

        Debug.LogWarning($"No note mapping found for y: {y}. Using default note 'Do'.");
        return NoteName.Do;
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

        // Log for debugging
        Debug.Log($"Updated grid at ({x}, {y}, {z}) with object: {newObject?.name}");
    }
}
