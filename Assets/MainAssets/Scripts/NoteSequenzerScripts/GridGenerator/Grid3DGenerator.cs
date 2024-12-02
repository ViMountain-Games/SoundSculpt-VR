using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CustomInspector;
using UnityEditor;

[DefaultExecutionOrder(-100)] // Assicura che questo script venga eseguito prima degli altri
public class Grid3DGenerator : MonoBehaviour
{
    public static Grid3DGenerator Instance { get; private set; }

    // Definizione dell'enum NoteName
    public enum NoteName
    {
        Do,
        DoSharp,
        Re,
        ReSharp,
        Mi,
        Fa,
        FaSharp,
        Sol,
        SolSharp,
        La,
        LaSharp,
        Si
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
        public NoteName noteName; // Utilizza l'enum invece di una stringa
    }

    [Title("Grid Settings", fontSize = 14, alignment = TextAlignment.Center)]
    [Min(1)] public int gridSizeX = 5;
    [Min(1)] public int gridSizeY = 12; // Numero di note
    [Min(1)] public int gridSizeZ = 3;  // Numero di ottave
    [DynamicSlider]
    public DynamicSlider cellSize = new DynamicSlider(1f, 0.1f, 5f);

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

    [Header("Octave Mappings")]
    [SerializeField]
    public List<OctaveMapping> octaveMappings = new List<OctaveMapping>();

    [Header("Default Octave Settings")]
    [Min(1)] public int defaultOctave = 2; // Ottava predefinita se non viene trovata una mappatura

    [Header("Note Mappings")]
    [SerializeField]
    public List<NoteMapping> noteMappings = new List<NoteMapping>();


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
                        x * cellSize.value + cellSize.value / 2,
                        y * cellSize.value + cellSize.value / 2,
                        z * cellSize.value + cellSize.value / 2
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

        // Assegna il nuovo oggetto alla matrice della griglia
        gridMatrix[x, y, z] = newObject;

        // Aggiorna l'objectList
        // Prima, rimuovi eventuali voci esistenti alle stesse coordinate
        objectList.RemoveAll(entry => entry.x == x && entry.y == y && entry.z == z);

        if (newObject != null)
        {
            // Aggiungi una nuova voce
            GridEntry newEntry = new GridEntry(newObject, x, y, z);
            objectList.Add(newEntry);
        }

        // Ordina la lista in base a x, poi z, poi y
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

    public void PlayAssignedNotes()
    {
        if (gridMatrix == null)
        {
            Debug.LogError("GridMatrix is not initialized.");
            return;
        }

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                for (int z = 0; z < gridSizeZ; z++)
                {
                    GameObject cellObject = gridMatrix[x, y, z];
                    if (cellObject != null)
                    {
                        Note noteComponent = cellObject.GetComponent<Note>();
                        if (noteComponent != null)
                        {
                            noteComponent.PlayNote();
                        }
                    }
                }
            }
        }
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
        return defaultOctave; // Usa l'ottava predefinita se non viene trovata una mappatura
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
        return NoteName.Do; // Nota predefinita se non viene trovata una mappatura
    }
}
