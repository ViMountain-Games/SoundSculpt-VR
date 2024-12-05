using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CustomInspector;
using TMPro; // Per TextMeshPro
#if UNITY_EDITOR
using UnityEditor;
#endif

[DefaultExecutionOrder(-100)] // Assicura che questo script venga eseguito prima degli altri
public class Grid3DGenerator : MonoBehaviour
{
    public static Grid3DGenerator Instance { get; private set; }

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
    [Min(1)] public int gridSizeY = 17;
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

    [HorizontalLine("Bar Line Settings", 2)]
    [Min(1)] public int cellsPerBar = 4;
    [ForceFill] public GameObject barLinePrefab;
    public float yOffset = 0f; // Offset per l'altezza della bar line
    public float zOffset = 0f; // Offset per la profondità della bar line

    [HorizontalLine("Animation Settings", 2)]
    public float lineAnimationDuration = 1f; // Durata dell'animazione delle linee
    public float lineAnimationDelay = 0.05f; // Ritardo tra l'animazione di ogni linea

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
        labelsParent = new GameObject("Labels") { transform = { parent = this.transform } };
        Vector3 origin = transform.position;

        // Genera le celle della griglia
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
                    gridMatrix[x, y, z] = null; // Default a vuoto
                }
            }
        }

        // Genera le etichette a sinistra della griglia
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
                Debug.LogError("Il prefab dell'etichetta non ha un componente TextMeshPro nei suoi figli.");
            }
        }

        // Disegna le linee della griglia con animazione
        StartCoroutine(DrawGridLinesAnimated(origin, gridParent));

        // Genera le bar line
        GenerateBarLines(origin);

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
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

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    private IEnumerator DrawGridLinesAnimated(Vector3 origin, GameObject parent)
    {
        List<LineData> linesToAnimate = new List<LineData>();

        // Raccoglie tutte le linee da disegnare
        for (int x = 0; x <= gridSizeX; x++)
        {
            for (int y = 0; y <= gridSizeY; y++)
            {
                Vector3 start = origin + new Vector3(x * cellSize.value, y * cellSize.value, 0);
                Vector3 end = origin + new Vector3(x * cellSize.value, y * cellSize.value, gridSizeZ * cellSize.value);
                linesToAnimate.Add(new LineData(start, end));
            }
        }

        for (int y = 0; y <= gridSizeY; y++)
        {
            for (int z = 0; z <= gridSizeZ; z++)
            {
                Vector3 start = origin + new Vector3(0, y * cellSize.value, z * cellSize.value);
                Vector3 end = origin + new Vector3(gridSizeX * cellSize.value, y * cellSize.value, z * cellSize.value);
                linesToAnimate.Add(new LineData(start, end));
            }
        }

        for (int x = 0; x <= gridSizeX; x++)
        {
            for (int z = 0; z <= gridSizeZ; z++)
            {
                Vector3 start = origin + new Vector3(x * cellSize.value, 0, z * cellSize.value);
                Vector3 end = origin + new Vector3(x * cellSize.value, gridSizeY * cellSize.value, z * cellSize.value);
                linesToAnimate.Add(new LineData(start, end));
            }
        }

        // Anima ogni linea con un leggero ritardo
        foreach (LineData lineData in linesToAnimate)
        {
            DrawLineAnimated(lineData.start, lineData.end, parent);
            yield return new WaitForSeconds(lineAnimationDelay);
        }
    }

    private void DrawLineAnimated(Vector3 start, Vector3 end, GameObject parent)
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

        // Inizializza la linea al punto di partenza
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, start);

        // Inizia ad animare la linea
        StartCoroutine(AnimateLine(lineRenderer, start, end));
    }

    private IEnumerator AnimateLine(LineRenderer lineRenderer, Vector3 start, Vector3 end)
    {
        float elapsedTime = 0f;

        while (elapsedTime < lineAnimationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / lineAnimationDuration;
            Vector3 currentPosition = Vector3.Lerp(start, end, t);
            lineRenderer.SetPosition(1, currentPosition);
            yield return null;
        }

        // Assicura che la linea raggiunga la posizione finale
        lineRenderer.SetPosition(1, end);
    }

    private void GenerateBarLines(Vector3 origin)
    {
        if (cellsPerBar < 1)
        {
            Debug.LogError("cellsPerBar deve essere almeno 1.");
            return;
        }

        GameObject barLinesParent = new GameObject("BarLines") { transform = { parent = this.transform } };

        for (int x = cellsPerBar; x < gridSizeX; x += cellsPerBar)
        {
            Vector3 barLinePosition = origin + new Vector3(
                x * cellSize.value,
                gridSizeY * cellSize.value / 2,
                gridSizeZ * cellSize.value / 2
            );

            GameObject barLineInstance = Instantiate(barLinePrefab, barLinePosition, Quaternion.identity, barLinesParent.transform);

            // Regola la scala della bar line per adattarla all'altezza e profondità della griglia, con offset
            Vector3 barLineScale = barLineInstance.transform.localScale;
            barLineScale.y = gridSizeY * cellSize.value + yOffset;
            barLineScale.z = gridSizeZ * cellSize.value + zOffset;
            barLineScale.x = barLineScale.x; // Mantiene lo spessore originale o lo regola se necessario
            barLineInstance.transform.localScale = barLineScale;
        }
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

        Debug.LogWarning($"Nessuna mappatura di ottava trovata per z: {z}. Utilizzo l'ottava di default {defaultOctave}.");
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

        Debug.LogWarning($"Nessuna mappatura di nota trovata per y: {y}. Utilizzo la nota di default 'Do'.");
        return NoteName.Do;
    }

    public void UpdateGridMatrix(int x, int y, int z, GameObject newObject)
    {
        if (gridMatrix == null)
        {
            Debug.LogError("GridMatrix non è inizializzata. Assicurati di chiamare GenerateGrid prima di aggiornare la matrice.");
            return;
        }

        if (x < 0 || x >= gridSizeX || y < 0 || y >= gridSizeY || z < 0 || z >= gridSizeZ)
        {
            Debug.LogError($"Indici non validi: ({x}, {y}, {z}). Assicurati che siano all'interno dei limiti della griglia.");
            return;
        }

        // Assegna il nuovo oggetto alla matrice della griglia
        gridMatrix[x, y, z] = newObject;

        // Log per il debugging
        Debug.Log($"Aggiornata la griglia in ({x}, {y}, {z}) con l'oggetto: {newObject?.name}");
    }

    // Classe helper per memorizzare i dati delle linee
    private class LineData
    {
        public Vector3 start;
        public Vector3 end;

        public LineData(Vector3 start, Vector3 end)
        {
            this.start = start;
            this.end = end;
        }
    }
}
