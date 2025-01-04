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

    // ========== BAR LINE ==========
    [Header("Bar Line Settings")]
    public GameObject barLinePrefab;

    // Usiamo ‘occupied[z,x]’ per le *pause*
    private bool[,] occupied;  // dimensione [gridSizeZ, gridSizeX]
    private List<int> barColumns = new List<int>();

    private float baseLineSpacing = 0.5f;

    private void Awake()
    {
        // Se gridGenerator esiste già in Awake, creiamo subito ‘occupied’
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

        // Se ‘occupied’ non era stato creato in Awake, lo creiamo ora
        if (occupied == null)
        {
            occupied = new bool[gridGenerator.gridSizeZ, gridGenerator.gridSizeX];
        }

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

        noteTypePrefabsList.RemoveAll(ntp => !currentNoteTypes.Contains(ntp.noteType));

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
        if (step < 1) step = 4;

        for (int x = step; x < gridGenerator.gridSizeX; x += step)
        {
            barColumns.Add(x);
        }
    }

    /// <summary>
    /// Genera le NOTE e le PAUSE sul pentagramma
    /// </summary>
    public void GenerateScore()
    {
        ClearScore();

        // Creiamo un parent per le note
        noteParent = new GameObject("Notes").transform;
        noteParent.SetParent(transform);
        noteParent.localPosition = Vector3.zero;

        // PASSO 1: posizioniamo TUTTE le NOTE (non blocchiamo più la seconda)
        for (int x = 0; x < gridGenerator.gridSizeX; x++)
        {
            for (int z = 0; z < gridGenerator.gridSizeZ; z++)
            {
                int octave = gridGenerator.GetOctaveFromZ(z);

                // Raccogliamo TUTTE le note in [x,*,z]
                List<NoteInfo> notesAtPosition = new List<NoteInfo>();

                for (int y = 0; y < gridGenerator.gridSizeY; y++)
                {
                    NoteData nd = gridGenerator.GetSolutionCell(x, y, z);
                    if (nd == null) 
                        continue;

                    string noteName = gridGenerator.GetNoteNameFromY(y).ToString();
                    // [NOVITÀ] Piazziamo la nota SEMPRE, senza "IsFree"
                    float xPos, yPos, zPos;
                    GameObject noteInstance = CreateNote(nd, noteName, x, octave, out xPos, out yPos, out zPos);
                    if (noteInstance != null)
                    {
                        notesAtPosition.Add(new NoteInfo(noteInstance, xPos, yPos, zPos));
                    }

                    // [IMPORTANTE] segniamo la colonna come occupata, così NIENTE *pause* in (x.. x+duration)
                    // Calcoliamo lunghezza (ad es. quarter=1, half=2, whole=4)
                    int neededCols = GetNeededCols(nd.duration);
                    MarkColumnsOccupied(x, z, neededCols);
                }

                // Se ci sono più note (x,z), spostiamo la seconda se "IsSpaceNote"
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

        // PASSO 2: posizioniamo LE PAUSE nei rimanenti spazi
        for (int z = 0; z < gridGenerator.gridSizeZ; z++)
        {
            FillRestsForRow(z);
        }

        // PASSO 3: Calcolo lunghezza staff
        lineLength = (gridGenerator.gridSizeX * horizontalSpacing) * lengthMultiplier + staffExtraLength;
    }

    /// <summary>
    /// Inizializzazione della pentagramma: linee orizzontali e bar line.
    /// </summary>
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

    /// <summary>
    /// Disegno bar lines verticali ogni 'cellsPerBar' colonne
    /// </summary>
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

    /// <summary>
    /// Riempi le colonne libere (non occupate) con PAUSE
    /// </summary>
    private void FillRestsForRow(int z)
    {
        int totalCols = gridGenerator.gridSizeX;
        int c = 0;

        while (c < totalCols)
        {
            // Se questa colonna X è occupata, skip
            if (occupied[z, c])
            {
                c++;
                continue;
            }

            // TROVIAMO LO SPAZIO LIBERO
            int start = c;
            int length = 0;
            while (c < totalCols && !occupied[z, c])
            {
                c++;
                length++;
            }
            int end = start + length - 1;

            // Suddividiamo in base alle bar line
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

    /// <summary>
    /// Riempi [startCol..endCol] di PAUSE (4/4, 2/4, 1/4)
    /// </summary>
    private void FillRestsInRange(int z, int startCol, int endCol)
    {
        int gap = (endCol - startCol) + 1;
        int offset = 0;

        while (offset < gap)
        {
            int remain = gap - offset;
            // Se c’è spazio per 4 e ho wholeRest
            if (remain >= 4 && wholeRestPrefab != null)
            {
                int col = startCol + offset;
                CreateRest(wholeRestPrefab, col, z);
                MarkColumnsOccupied(col, z, 4);
                offset += 4;
            }
            // Se c’è spazio per 2 e ho halfRest
            else if (remain >= 2 && halfRestPrefab != null)
            {
                int col = startCol + offset;
                CreateRest(halfRestPrefab, col, z);
                MarkColumnsOccupied(col, z, 2);
                offset += 2;
            }
            else
            {
                // Mettiamo 1/4
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

    /// <summary>
    /// Distrugge le note/pentagramma precedenti e resetta ‘occupied’.
    /// </summary>
    private void ClearScore()
    {
        if (occupied != null && gridGenerator != null)
        {
            for (int z = 0; z < gridGenerator.gridSizeZ; z++)
            {
                for (int x = 0; x < gridGenerator.gridSizeX; x++)
                {
                    occupied[z, x] = false;
                }
            }
        }

        if (noteParent != null) DestroyImmediate(noteParent.gameObject);
        if (pentagramParent != null) DestroyImmediate(pentagramParent.gameObject);
    }

    /// <summary>
    /// Crea effettivamente la nota (prefab) sul pentagramma
    /// </summary>
    private GameObject CreateNote(NoteData nd, string noteName, int x, int octave,
                                  out float finalX, out float finalY, out float finalZ)
    {
        finalX = finalY = finalZ = 0f;
        if (nd == null) return null;

        var chosen = noteTypePrefabsList.Find(ntp => ntp.noteType == nd.SelectedNoteType);
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

        // offset Y
        float spacingScale = lineSpacing / baseLineSpacing;
        float yPos = transform.position.y
                     + (GetNoteVerticalOffset(noteName) * spacingScale)
                     + ((octave - 3) * 3.5f * verticalOffset * spacingScale);

        float zPos = transform.position.z + noteZBaseOffset + (GetNoteZIndex(noteName) * noteZIncrement);

        GameObject instance = Instantiate(notePrefab, new Vector3(xPos, yPos, zPos), Quaternion.identity, noteParent);

        var sr = instance.GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.color = nd.color;

        // Gestione diesis/bemolle
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

    /// <summary>
    /// Crea la *rest* (pausa) su (x,z)
    /// </summary>
    private GameObject CreateRest(GameObject restPrefab, int x, int z)
    {
        if (restPrefab == null) return null;
        int midLine = numberOfLines / 2;
        float yPos = transform.position.y + (midLine * lineSpacing);
        float xPos = transform.position.x + (x * horizontalSpacing) + noteXOffset;
        float zPos = transform.position.z + noteZBaseOffset + (z * 0.01f);

        GameObject rest = Instantiate(restPrefab, new Vector3(xPos, yPos, zPos), Quaternion.identity, noteParent);
        rest.name = $"Rest_{x}_{z}";
        return rest;
    }

    /// <summary>
    /// Se Y è in mezzo allo spazio e non su una linea, la spostiamo un po’ a destra
    /// </summary>
    private bool IsSpaceNote(float yPos)
    {
        float baseY = transform.position.y;
        float normalized = (yPos - baseY) / lineSpacing;
        float nearest = Mathf.Round(normalized);
        return (Mathf.Abs(normalized - nearest) > 0.01f);
    }

    /// <summary>
    /// Indica quante "colonne" X occupa una nota/pause: 1, 2 o 4
    /// </summary>
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

    /// <summary>
    /// Marca come occupate le colonne [x.. x+count-1] per la riga z
    /// => serve a non piazzare *pause* lì
    /// </summary>
    private void MarkColumnsOccupied(int x, int z, int count)
    {
        if (occupied == null || gridGenerator == null) return;

        int max = Mathf.Min(x + count, gridGenerator.gridSizeX);
        for (int col = x; col < max; col++)
        {
            occupied[z, col] = true;
        }
    }

    // Calcola offset verticale
    private float GetNoteVerticalOffset(string noteName)
    {
        switch(noteName)
        {
            case "Do":      return -0.50f;
            case "DoSharp": return -0.50f;
            case "ReFlat":  return -0.25f;
            case "Re":      return -0.25f;
            case "ReSharp": return -0.25f;
            case "MiFlat":  return 0.0f;
            case "Mi":      return 0.0f;
            case "Fa":      return 0.25f;
            case "FaSharp": return 0.25f;
            case "SolFlat": return 0.50f;
            case "Sol":     return 0.50f;
            case "SolSharp":return 0.50f;
            case "LaFlat":  return 0.75f;
            case "La":      return 0.75f;
            case "LaSharp": return 0.75f;
            case "SiFlat":  return 1.00f;
            case "Si":      return 1.00f;
        }
        return 0f;
    }

    // Calcola offset Z
    private int GetNoteZIndex(string noteName)
    {
        switch(noteName)
        {
            case "Do":       return 0;
            case "DoSharp":  return 0;
            case "ReFlat":   return 1;
            case "Re":       return 1;
            case "ReSharp":  return 1;
            case "MiFlat":   return 2;
            case "Mi":       return 2;
            case "Fa":       return 3;
            case "FaSharp":  return 3;
            case "SolFlat":  return 4;
            case "Sol":      return 4;
            case "SolSharp": return 4;
            case "LaFlat":   return 5;
            case "La":       return 5;
            case "LaSharp":  return 5;
            case "SiFlat":   return 6;
            case "Si":       return 6;
        }
        return 0;
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
