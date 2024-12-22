using UnityEngine;
using System.Collections.Generic;
using GridGen;
using CustomInspector; // Se nel tuo progetto serve, altrimenti rimuovi.

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
    public GameObject quarterRestPrefab; // gialla => 1/4
    public GameObject halfRestPrefab;    // arancione => 2/4
    public GameObject wholeRestPrefab;   // rossa => 4/4

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
    public float lineLength;

    private Transform pentagramParent;
    private Transform noteParent;

    [Header("Grid Settings")]
    public Grid3DGenerator gridGenerator;

    // ==========================
    //   BAR LINE (prefab)
    // ==========================
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

    // Occupato [z, x]
    private bool[,] occupied;

    // Lista delle posizioni di bar line (colonne) calcolata da gridGenerator.cellsPerBar
    private List<int> barColumns = new List<int>();

    void Start()
    {
        if (gridGenerator == null)
        {
            Debug.LogError("Grid3DGenerator non assegnato!");
            return;
        }

        occupied = new bool[gridGenerator.gridSizeZ, gridGenerator.gridSizeX];

        // Calcoliamo tutte le bar line in base a cellsPerBar
        ComputeBarColumns();

        GenerateScore();
        InitializePentagram();
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

    /// <summary>
    /// Prepara la lista barColumns, ossia le colonne (x) dove cade una bar line.
    /// Esempio: se cellsPerBar=4 e gridSizeX=12, avremo barColumns = [4,8].
    /// </summary>
    private void ComputeBarColumns()
    {
        barColumns.Clear();
        int step = gridGenerator.cellsPerBar;
        if (step < 1) step = 4; // fallback

        for (int x = step; x < gridGenerator.gridSizeX; x += step)
        {
            barColumns.Add(x);
        }
    }

    private void GenerateScore()
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
                        continue; // sfora la griglia

                    // Se [x.. x+neededCols-1] è libero, piazza la nota
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
    }

    /// <summary>
    /// Riempie le colonne libere in riga z con pause.
    /// Se la pausa andrebbe oltre una bar line, la interrompiamo.
    /// </summary>
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

            // Trovato uno spazio libero => calcoliamo la lunghezza di questo "gap"
            int start = c;
            int length = 0;
            while (c < totalCols && !occupied[z, c])
            {
                c++;
                length++;
            }
            int end = start + length - 1;

            // Suddividiamo [start..end] in base alle bar line e riempiamo con FillRestsInRange
            SplitByBarLinesAndFill(z, start, end);
        }
    }

    /// <summary>
    /// Prende il gap [startCol..endCol] e lo spezza in sub-range se c’è una bar line in mezzo.
    /// Esempio: se bar line è a 8 e [start..end] = [5..10], facciamo [5..7] e [8..10].
    /// </summary>
    private void SplitByBarLinesAndFill(int z, int startCol, int endCol)
    {
        // Può esserci più di una bar line dentro [startCol..endCol].
        int currentStart = startCol;

        foreach (int barCol in barColumns)
        {
            // Se la bar line cade entro l'intervallo
            if (barCol > currentStart && barCol <= endCol)
            {
                // Prima sub-range [currentStart..(barCol-1)]
                int subEnd = barCol - 1;
                if (subEnd >= currentStart)
                {
                    FillRestsInRange(z, currentStart, subEnd);
                }
                // poi spostiamo lo start dopo la bar line
                currentStart = barCol;
            }
        }

        // Alla fine, ci resta [currentStart..endCol]
        if (currentStart <= endCol)
        {
            FillRestsInRange(z, currentStart, endCol);
        }
    }

    /// <summary>
    /// Riempi l’intervallo [startCol..endCol] con pause (4/4, 2/4, 1/4),
    /// tenendo conto che qui NON ci sono bar line in mezzo.
    /// </summary>
    private void FillRestsInRange(int z, int startCol, int endCol)
    {
        int gap = (endCol - startCol) + 1;
        int offset = 0;

        while (offset < gap)
        {
            int remain = gap - offset;

            // Se c’è spazio per 4 e hai wholeRestPrefab
            if (remain >= 4 && wholeRestPrefab != null)
            {
                int col = startCol + offset;
                CreateRest(wholeRestPrefab, col, z);
                MarkColumnsOccupied(col, z, 4);
                offset += 4;
            }
            // Se c’è spazio per 2 e hai halfRestPrefab
            else if (remain >= 2 && halfRestPrefab != null)
            {
                int col = startCol + offset;
                CreateRest(halfRestPrefab, col, z);
                MarkColumnsOccupied(col, z, 2);
                offset += 2;
            }
            else
            {
                // Piazziamo 1/4
                if (quarterRestPrefab != null)
                {
                    int col = startCol + offset;
                    CreateRest(quarterRestPrefab, col, z);
                    MarkColumnsOccupied(col, z, 1);
                    offset += 1;
                }
                else
                {
                    Debug.LogWarning("Manca quarterRestPrefab!");
                    break;
                }
            }
        }
    }

    // ============================
    //   BAR LINE VISUAL
    // ============================
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
            Debug.LogError("Staff Line Prefab non assegnato!");
            return;
        }

        // Disegno le 5 linee
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

        // Bar lines
        InitializeBarLines();
    }

    /// <summary>
    /// Disegna le bar line visuali (verticali) ogni cellsPerBar colonne
    /// </summary>
    private void InitializeBarLines()
    {
        if (barLinePrefab == null) return;

        Transform barLinesParent = new GameObject("BarLines").transform;
        barLinesParent.SetParent(transform);
        barLinesParent.localPosition = Vector3.zero;

        // Altezza totale: dalla prima riga (0) all’ultima riga (numberOfLines-1)
        float totalHeight = (numberOfLines - 1) * lineSpacing;

        foreach (int barCol in barColumns)
        {
            float xPos = transform.position.x + (barCol * horizontalSpacing);
            float centerY = transform.position.y + (totalHeight / 2f);
            float zPos = transform.position.z;

            GameObject barLineObj = Instantiate(barLinePrefab, new Vector3(xPos, centerY, zPos), Quaternion.identity, barLinesParent);
            barLineObj.name = $"BarLine_{barCol}";

            Vector3 scale = barLineObj.transform.localScale;
            scale.x = lineWidth;   // spessore
            scale.y = totalHeight; // altezza
            barLineObj.transform.localScale = scale;
        }
    }

    // -----------------------------------------------------
    //       Metodi di supporto per occupazione e note
    // -----------------------------------------------------
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
            case NoteData.NoteDuration.Quarter: return 1; // gialla
            case NoteData.NoteDuration.Half: return 2; // arancione
            case NoteData.NoteDuration.Whole: return 4; // rossa
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
            Debug.LogWarning("Manca il prefab per la nota: " + nd.SelectedNoteType);
            return null;
        }

        GameObject notePrefab = null;
        switch (nd.duration)
        {
            case NoteData.NoteDuration.Quarter:
                notePrefab = chosen.quarterNotePrefab;
                break;
            case NoteData.NoteDuration.Half:
                notePrefab = chosen.halfNotePrefab;
                break;
            case NoteData.NoteDuration.Whole:
                notePrefab = chosen.wholeNotePrefab;
                break;
        }

        if (notePrefab == null)
        {
            Debug.LogWarning($"Prefab non trovato per {nd.SelectedNoteType} - {nd.duration}");
            return null;
        }

        float xPos = transform.position.x + (x * horizontalSpacing) + noteXOffset;

        if (!notePositionMapping.ContainsKey(noteName))
        {
            Debug.LogWarning("Nota non trovata: " + noteName);
            return null;
        }
        float spacingScale = lineSpacing / baseLineSpacing;
        float yPos = transform.position.y
                     + (notePositionMapping[noteName] * spacingScale)
                     + ((octave - 3) * 3.5f * verticalOffset * spacingScale);

        if (!noteZIndex.ContainsKey(noteName))
        {
            Debug.LogWarning("Nota ZIndex mancante: " + noteName);
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
