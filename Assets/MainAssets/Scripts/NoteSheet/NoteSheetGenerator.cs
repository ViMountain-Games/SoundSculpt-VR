using UnityEngine;
using System.Collections.Generic;
using GridGen;
using CustomInspector;

[DefaultExecutionOrder(-150)]
public class ScoreGenerator : MonoBehaviour
{
    [Header("Note Data")]
    public NoteData referenceNoteData;

    [System.Serializable]
    public class NoteTypePrefabs
    {
        public string noteType;
        public GameObject quarterNotePrefab;
        public GameObject halfNotePrefab;
        public GameObject wholeNotePrefab;
    }

    public List<NoteTypePrefabs> noteTypePrefabsList = new List<NoteTypePrefabs>();

    [Header("Rest Prefabs (Pause)")]
    public GameObject quarterRestPrefab; 
    public GameObject halfRestPrefab;    
    public GameObject wholeRestPrefab;   

    [Header("Note Spacing Settings")]
    public float horizontalSpacing = 1.0f;
    public float verticalOffset = 0.5f;
    public float noteZIncrement = 0.05f;
    public float noteXOffset = 0.5f;
    public float noteZBaseOffset = -0.1f;
    public float spaceNoteOffset = 0.2f;

    [Header("Pentagram Settings")]
    public int numberOfLines = 5;
    public float lineSpacing = 0.5f;
    public float lineWidth = 0.1f;
    public Color lineColor = Color.black;
    public Material lineMaterial;
    public GameObject staffLinePrefab;

    [Header("Staff Length Settings")]
    public float lengthMultiplier = 1.0f;
    public float staffExtraLength = 1.0f;

    [ReadOnly]
    public float lineLength;  // usato da ScoreTimelineMover

    private Transform pentagramParent;
    private Transform noteParent;

    [Header("Grid Settings")]
    public Grid3DGenerator gridGenerator;

    [Header("Bar Line Settings")]
    public GameObject barLinePrefab;  // Il prefab della bar line

    private float baseLineSpacing = 0.5f; // Spacing di riferimento

    // Mappa delle note -> offset verticale
    private Dictionary<string, float> notePositionMapping = new Dictionary<string, float>
    {
        { "Do",      -0.50f },
        { "DoSharp", -0.50f },
        { "ReFlat",  -0.25f },
        { "Re",      -0.25f },
        { "ReSharp", -0.25f },
        { "MiFlat",   0.00f },
        { "Mi",       0.00f },
        { "Fa",       0.25f },
        { "FaSharp",  0.25f },
        { "SolFlat",  0.50f },
        { "Sol",      0.50f },
        { "SolSharp", 0.50f },
        { "LaFlat",   0.75f },
        { "La",       0.75f },
        { "LaSharp",  0.75f },
        { "SiFlat",   1.00f },
        { "Si",       1.00f }
    };

    // Mappa delle note -> indice per lo spostamento sulla Z
    private Dictionary<string, int> noteZIndex = new Dictionary<string, int>
    {
        { "Do",       0 },
        { "DoSharp",  0 },
        { "ReFlat",   1 },
        { "Re",       1 },
        { "ReSharp",  1 },
        { "MiFlat",   2 },
        { "Mi",       2 },
        { "Fa",       3 },
        { "FaSharp",  3 },
        { "SolFlat",  4 },
        { "Sol",      4 },
        { "SolSharp", 4 },
        { "LaFlat",   5 },
        { "La",       5 },
        { "LaSharp",  5 },
        { "SiFlat",   6 },
        { "Si",       6 }
    };

    private bool[,] occupied; // dimensione [gridSizeZ, gridSizeX]
    private List<int> barColumns = new List<int>();

    private void Awake()
    {
        // Se gridGenerator è già assegnato, inizializziamo qui 'occupied'
        if (gridGenerator != null)
        {
            occupied = new bool[gridGenerator.gridSizeZ, gridGenerator.gridSizeX];
        }
        else
        {
            Debug.LogWarning("[ScoreGenerator] gridGenerator è null in Awake!");
        }
    }

    void Start()
    {
        if (gridGenerator == null)
        {
            Debug.LogError("[ScoreGenerator] Nessun gridGenerator assegnato!");
            return;
        }

        // Se 'occupied' non è stato creato (magari gridGenerator era null in Awake e lo hai assegnato dopo),
        // lo creiamo ora.
        if (occupied == null)
        {
            occupied = new bool[gridGenerator.gridSizeZ, gridGenerator.gridSizeX];
        }

        ComputeBarColumns();
        GenerateScore();         // qui ClearScore() -> no NullRef, poiché 'occupied' esiste
        InitializePentagram();   // disegna linee e bar lines
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        SyncNoteTypes();
    }
#endif

    private void SyncNoteTypes()
    {
        if (referenceNoteData == null) return;
        List<string> currentNoteTypes = referenceNoteData.noteTypes;

        // Rimuove tipi non più presenti
        noteTypePrefabsList.RemoveAll(ntp => !currentNoteTypes.Contains(ntp.noteType));

        // Aggiunge quelli nuovi
        foreach (string newType in currentNoteTypes)
        {
            if (!noteTypePrefabsList.Exists(ntp => ntp.noteType == newType))
            {
                noteTypePrefabsList.Add(new NoteTypePrefabs { noteType = newType });
            }
        }
    }

    private void ComputeBarColumns()
    {
        barColumns.Clear();
        if (gridGenerator == null) return;

        int step = gridGenerator.cellsPerBar;
        if (step < 1) step = 4; // fallback

        for (int x = step; x < gridGenerator.gridSizeX; x += step)
        {
            barColumns.Add(x);
        }
    }

    public void GenerateScore()
    {
        ClearScore();

        noteParent = new GameObject("Notes").transform;
        noteParent.SetParent(transform);
        noteParent.localPosition = Vector3.zero;

        // STEP 1: posizioniamo le NOTE
        for (int x = 0; x < gridGenerator.gridSizeX; x++)
        {
            for (int z = 0; z < gridGenerator.gridSizeZ; z++)
            {
                List<NoteInfo> notesAtPosition = new List<NoteInfo>();
                int octave = gridGenerator.GetOctaveFromZ(z);

                for (int y = 0; y < gridGenerator.gridSizeY; y++)
                {
                    NoteData nd = gridGenerator.GetSolutionCell(x, y, z);
                    if (nd == null) continue;

                    string noteName = gridGenerator.GetNoteNameFromY(y).ToString();
                    int neededCols = GetNeededCols(nd.duration);

                    if (x + neededCols - 1 >= gridGenerator.gridSizeX)
                        continue;

                    if (IsFree(x, z, neededCols))
                    {
                        float xPos, yPos, zPos;
                        GameObject noteInstance = CreateNote(nd, noteName, x, octave, out xPos, out yPos, out zPos);
                        if (noteInstance != null)
                        {
                            notesAtPosition.Add(new NoteInfo(noteInstance, xPos, yPos, zPos));
                            MarkColumnsOccupied(x, z, neededCols);
                        }
                    }
                }

                // Se ci sono più note in (x,z), spostiamo quelle in "space"
                if (notesAtPosition.Count > 1)
                {
                    foreach (var noteInfo in notesAtPosition)
                    {
                        if (IsSpaceNote(noteInfo.yPos))
                        {
                            Vector3 pos = noteInfo.note.transform.position;
                            pos.x += spaceNoteOffset;
                            noteInfo.note.transform.position = pos;
                        }
                    }
                }
            }
        }

        // STEP 2: posizioniamo le PAUSE
        for (int z = 0; z < gridGenerator.gridSizeZ; z++)
        {
            FillRestsForRow(z);
        }

        // STEP 3: Calcolo lunghezza staff
        lineLength = (gridGenerator.gridSizeX * horizontalSpacing) * lengthMultiplier + staffExtraLength;
        // Ora lineLength > 0 (di solito)
    }

    private void FillRestsForRow(int z)
    {
        int totalCols = gridGenerator.gridSizeX;
        int c = 0;

        while (c < totalCols)
        {
            if (occupied[z, c])
            {
                c++;
                continue;
            }

            int start = c;
            int length = 0;
            while (c < totalCols && !occupied[z, c])
            {
                c++;
                length++;
            }
            int end = start + length - 1;
            SplitByBarLinesAndFill(z, start, end);
        }
    }

    private void SplitByBarLinesAndFill(int z, int startCol, int endCol)
    {
        int currentStart = startCol;
        foreach (int barCol in barColumns)
        {
            if (barCol > currentStart && barCol <= endCol)
            {
                int subEnd = barCol - 1;
                if (subEnd >= currentStart)
                {
                    FillRestsInRange(z, currentStart, subEnd);
                }
                currentStart = barCol;
            }
        }
        if (currentStart <= endCol)
        {
            FillRestsInRange(z, currentStart, endCol);
        }
    }

    private void FillRestsInRange(int z, int startCol, int endCol)
    {
        int gap = (endCol - startCol) + 1;
        int offset = 0;

        while (offset < gap)
        {
            int remain = gap - offset;
            if (remain >= 4 && wholeRestPrefab != null)
            {
                int col = startCol + offset;
                CreateRest(wholeRestPrefab, col, z);
                MarkColumnsOccupied(col, z, 4);
                offset += 4;
            }
            else if (remain >= 2 && halfRestPrefab != null)
            {
                int col = startCol + offset;
                CreateRest(halfRestPrefab, col, z);
                MarkColumnsOccupied(col, z, 2);
                offset += 2;
            }
            else
            {
                if (quarterRestPrefab != null)
                {
                    int col = startCol + offset;
                    CreateRest(quarterRestPrefab, col, z);
                    MarkColumnsOccupied(col, z, 1);
                    offset += 1;
                }
                else
                {
                    Debug.LogWarning("[ScoreGenerator] quarterRestPrefab mancante!");
                    break;
                }
            }
        }
    }

    private void InitializePentagram()
    {
        if (pentagramParent == null)
        {
            pentagramParent = new GameObject("Pentagram").transform;
            pentagramParent.SetParent(transform);
            pentagramParent.localPosition = Vector3.zero;
        }

        if (staffLinePrefab == null)
        {
            Debug.LogError("[ScoreGenerator] StaffLinePrefab non assegnato!");
            return;
        }

        for (int i = 0; i < numberOfLines; i++)
        {
            float yPos = i * lineSpacing;
            GameObject lineObject = Instantiate(staffLinePrefab, pentagramParent);
            lineObject.name = $"PentagramLine_{i}";

            float lineCenterX = transform.position.x + lineLength * 0.5f;
            float lineCenterY = transform.position.y + yPos;
            float lineCenterZ = transform.position.z;

            lineObject.transform.position = new Vector3(lineCenterX, lineCenterY, lineCenterZ);

            Vector3 localScale = lineObject.transform.localScale;
            localScale.x = lineLength;
            localScale.y = lineWidth;
            lineObject.transform.localScale = localScale;

            var meshRenderer = lineObject.GetComponentInChildren<MeshRenderer>();
            if (meshRenderer != null && lineMaterial != null)
            {
                meshRenderer.material = lineMaterial;
            }
        }

        InitializeBarLines();
    }

    private void InitializeBarLines()
    {
        if (barLinePrefab == null) return;
        Transform barLinesParent = new GameObject("BarLines").transform;
        barLinesParent.SetParent(transform);
        barLinesParent.localPosition = Vector3.zero;

        float totalHeight = (numberOfLines - 1) * lineSpacing;
        foreach (int barCol in barColumns)
        {
            float xPos = transform.position.x + (barCol * horizontalSpacing);
            float centerY = transform.position.y + (totalHeight / 2f);
            float zPos = transform.position.z;

            GameObject barLineObj = Instantiate(barLinePrefab, new Vector3(xPos, centerY, zPos), Quaternion.identity, barLinesParent);
            barLineObj.name = $"BarLine_{barCol}";

            Vector3 scale = barLineObj.transform.localScale;
            scale.x = lineWidth;
            scale.y = totalHeight;
            barLineObj.transform.localScale = scale;
        }
    }

    private bool IsFree(int x, int z, int neededCols)
    {
        for (int col = x; col < x + neededCols; col++)
        {
            if (occupied[z, col]) return false;
        }
        return true;
    }

    private void MarkColumnsOccupied(int x, int z, int count)
    {
        int max = Mathf.Min(x + count, gridGenerator.gridSizeX);
        for (int col = x; col < max; col++)
        {
            occupied[z, col] = true;
        }
    }

    private int GetNeededCols(NoteData.NoteDuration duration)
    {
        switch (duration)
        {
            case NoteData.NoteDuration.Quarter: return 1;
            case NoteData.NoteDuration.Half:    return 2;
            case NoteData.NoteDuration.Whole:   return 4;
        }
        return 1;
    }

    private GameObject CreateRest(GameObject restPrefab, int x, int z)
    {
        if (restPrefab == null) return null;
        int middleLine = numberOfLines / 2;
        float yPos = transform.position.y + (middleLine * lineSpacing);
        float xPos = transform.position.x + (x * horizontalSpacing) + noteXOffset;
        float zPos = transform.position.z + noteZBaseOffset + (z * 0.01f);

        GameObject rest = Instantiate(restPrefab, new Vector3(xPos, yPos, zPos), Quaternion.identity, noteParent);
        rest.name = $"Rest_{x}_{z}";
        return rest;
    }

    private GameObject CreateNote(NoteData nd, string noteName, int x, int octave,
                                  out float finalX, out float finalY, out float finalZ)
    {
        finalX = finalY = finalZ = 0f;
        if (nd == null) return null;

        NoteTypePrefabs chosen = noteTypePrefabsList.Find(ntp => ntp.noteType == nd.SelectedNoteType);
        if (chosen == null)
        {
            Debug.LogWarning("[ScoreGenerator] Manca un prefab per la nota: " + nd.SelectedNoteType);
            return null;
        }

        GameObject notePrefab = null;
        switch (nd.duration)
        {
            case NoteData.NoteDuration.Quarter: notePrefab = chosen.quarterNotePrefab; break;
            case NoteData.NoteDuration.Half:    notePrefab = chosen.halfNotePrefab;    break;
            case NoteData.NoteDuration.Whole:   notePrefab = chosen.wholeNotePrefab;   break;
        }

        if (notePrefab == null)
        {
            Debug.LogWarning($"[ScoreGenerator] Prefab non trovato per {nd.SelectedNoteType} - {nd.duration}");
            return null;
        }

        float xPos = transform.position.x + (x * horizontalSpacing) + noteXOffset;

        if (!notePositionMapping.ContainsKey(noteName))
        {
            Debug.LogWarning("[ScoreGenerator] Nota non trovata: " + noteName);
            return null;
        }
        float spacingScale = lineSpacing / baseLineSpacing;
        float yPos = transform.position.y
                     + (notePositionMapping[noteName] * spacingScale)
                     + ((octave - 3) * 3.5f * verticalOffset * spacingScale);

        if (!noteZIndex.ContainsKey(noteName))
        {
            Debug.LogWarning("[ScoreGenerator] Nota ZIndex mancante: " + noteName);
            return null;
        }
        int zIndex = noteZIndex[noteName];
        float zPos = transform.position.z + noteZBaseOffset + (zIndex * noteZIncrement);

        GameObject instance = Instantiate(notePrefab, new Vector3(xPos, yPos, zPos), Quaternion.identity, noteParent);

        SpriteRenderer sr = instance.GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.color = nd.color;

        Transform noteModel = instance.transform.Find("NoteModel");
        if (noteModel != null)
        {
            Transform diesisObj = noteModel.Find("Diesis");
            Transform bemolleObj = noteModel.Find("Bemolle");

            if (diesisObj != null) diesisObj.gameObject.SetActive(false);
            if (bemolleObj != null) bemolleObj.gameObject.SetActive(false);

            if (noteName.Contains("Sharp") && diesisObj != null) diesisObj.gameObject.SetActive(true);
            if (noteName.Contains("Flat") && bemolleObj != null) bemolleObj.gameObject.SetActive(true);
        }

        finalX = xPos;
        finalY = yPos;
        finalZ = zPos;
        return instance;
    }

    private bool IsSpaceNote(float yPos)
    {
        float baseY = transform.position.y;
        float normalized = (yPos - baseY) / lineSpacing;
        float nearest = Mathf.Round(normalized);
        return (Mathf.Abs(normalized - nearest) > 0.01f);
    }

    private void ClearScore()
    {
        // Se non esiste, esci
        if (occupied == null)
        {
            Debug.LogWarning("[ScoreGenerator] 'occupied' è null in ClearScore(), skip.");
            return;
        }

        if (noteParent != null) DestroyImmediate(noteParent.gameObject);
        if (pentagramParent != null) DestroyImmediate(pentagramParent.gameObject);

        for (int z = 0; z < gridGenerator.gridSizeZ; z++)
        {
            for (int x = 0; x < gridGenerator.gridSizeX; x++)
            {
                occupied[z, x] = false;
            }
        }
    }

    private class NoteInfo
    {
        public GameObject note;
        public float xPos;
        public float yPos;
        public float zPos;

        public NoteInfo(GameObject note, float x, float y, float z)
        {
            this.note = note;
            this.xPos = x;
            this.yPos = y;
            this.zPos = z;
        }
    }
}
