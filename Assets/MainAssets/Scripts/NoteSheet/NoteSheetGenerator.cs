using UnityEngine;
using System.Collections.Generic;
using GridGen;
using CustomInspector; // Non so se necessario, ma lo mantengo dal tuo codice originale.

public class ScoreGenerator : MonoBehaviour
{
    [Header("Note Type & Duration Prefabs")]
    [Tooltip("Riferimento al NoteData per estrarre dinamicamente i tipi di nota disponibili.")]
    public NoteData referenceNoteData;

    [System.Serializable]
    public class NoteTypePrefabs
    {
        public string noteType;
        [Tooltip("Prefab della nota da 1/4 per questo tipo")]
        public GameObject quarterNotePrefab;
        [Tooltip("Prefab della nota da 2/4 per questo tipo")]
        public GameObject halfNotePrefab;
        [Tooltip("Prefab della nota da 4/4 per questo tipo")]
        public GameObject wholeNotePrefab;
    }

    [Tooltip("Lista dinamica dei prefab per ciascun tipo di nota. Questa lista viene sincronizzata con i tipi presenti in NoteData.")]
    public List<NoteTypePrefabs> noteTypePrefabsList = new List<NoteTypePrefabs>();

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

    [Header("Staff Line Prefab")]
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

    private float baseLineSpacing = 0.5f; // Spacing di riferimento originale

    // Mappatura note -> posizione verticale
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

    // Mappatura note -> indice per l'incremento sulla Z
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

    void Start()
    {
        if (gridGenerator == null)
        {
            Debug.LogError("Grid3DGenerator non assegnato!");
            return;
        }

        // Prima generiamo le note per calcolare la lunghezza massima dello spartito
        GenerateScore();
        // Poi disegniamo il pentagramma in base alla nuova lunghezza
        InitializePentagram();
    }

    /// <summary>
    /// Sincronizza i tipi di nota da NoteData con la lista di prefab.
    /// Questo metodo viene chiamato in OnValidate() per avere sempre l'inspector aggiornato.
    /// </summary>
    private void SyncNoteTypes()
    {
        if (referenceNoteData == null) return;

        List<string> currentNoteTypes = referenceNoteData.noteTypes;

        noteTypePrefabsList.RemoveAll(ntp => !currentNoteTypes.Contains(ntp.noteType));

        foreach (string newType in currentNoteTypes)
        {
            if (!noteTypePrefabsList.Exists(ntp => ntp.noteType == newType))
            {
                NoteTypePrefabs newEntry = new NoteTypePrefabs();
                newEntry.noteType = newType;
                noteTypePrefabsList.Add(newEntry);
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        SyncNoteTypes();
    }
#endif

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
            Debug.LogError("Staff Line Prefab non assegnato! Impossibile generare il pentagramma.");
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

            MeshRenderer meshRenderer = lineObject.GetComponentInChildren<MeshRenderer>();
            if (meshRenderer != null && lineMaterial != null)
            {
                meshRenderer.material = lineMaterial;
            }
        }
    }

    private void GenerateScore()
    {
        ClearScore();

        if (noteParent == null)
        {
            noteParent = new GameObject("Notes").transform;
            noteParent.SetParent(transform);
            noteParent.localPosition = Vector3.zero;
        }

        float maxNoteX = 0f;

        // Struttura di supporto per note alla stessa (x,z)
        // Per ogni (x,z) raccogliamo le note e poi applichiamo offset
        for (int x = 0; x < gridGenerator.gridSizeX; x++)
        {
            for (int z = 0; z < gridGenerator.gridSizeZ; z++)
            {
                List<NoteInfo> notesAtPosition = new List<NoteInfo>();

                int octave = gridGenerator.GetOctaveFromZ(z);
                for (int y = 0; y < gridGenerator.gridSizeY; y++)
                {
                    NoteData noteData = gridGenerator.GetSolutionCell(x, y, z);
                    if (noteData != null)
                    {
                        string noteName = gridGenerator.GetNoteNameFromY(y).ToString();

                        // Per note Half e Whole, solo la prima colonna
                        if ((noteData.duration == NoteData.NoteDuration.Half || noteData.duration == NoteData.NoteDuration.Whole) && x > 0)
                            continue;

                        float actualXPos, actualYPos, actualZPos;
                        GameObject noteInstance = CreateNote(noteData, noteName, x, octave, out actualXPos, out actualYPos, out actualZPos);

                        notesAtPosition.Add(new NoteInfo(noteInstance, actualXPos, actualYPos, actualZPos));

                        if (actualXPos > maxNoteX) maxNoteX = actualXPos;
                    }
                }

                // Se ci sono più note sovrapposte (accordi verticali), offset per quelle negli spazi
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

        if (maxNoteX > 0f)
        {
            float staffStartX = transform.position.x;
            lineLength = (maxNoteX - staffStartX) + staffExtraLength;
        }
        else
        {
            lineLength = gridGenerator.gridSizeX * horizontalSpacing * lengthMultiplier;
        }
    }

    private GameObject CreateNote(NoteData noteData, string noteName, int x, int octave, out float finalX, out float finalY, out float finalZ)
    {
        // Troviamo il prefab adatto
        NoteTypePrefabs chosenTypePrefabs = noteTypePrefabsList.Find(ntp => ntp.noteType == noteData.SelectedNoteType);
        if (chosenTypePrefabs == null)
        {
            Debug.LogError("Nessun prefab configurato per il tipo di nota: " + noteData.SelectedNoteType);
            finalX = finalY = finalZ = 0f;
            return null;
        }

        GameObject notePrefab = null;
        switch (noteData.duration)
        {
            case NoteData.NoteDuration.Quarter:
                notePrefab = chosenTypePrefabs.quarterNotePrefab;
                break;
            case NoteData.NoteDuration.Half:
                notePrefab = chosenTypePrefabs.halfNotePrefab;
                break;
            case NoteData.NoteDuration.Whole:
                notePrefab = chosenTypePrefabs.wholeNotePrefab;
                break;
        }

        if (notePrefab == null)
        {
            Debug.LogError("Nessun prefab assegnato per la durata: " + noteData.duration + " del tipo di nota: " + noteData.SelectedNoteType);
            finalX = finalY = finalZ = 0f;
            return null;
        }

        float xPos = transform.position.x + (x * horizontalSpacing) + noteXOffset;

        if (!notePositionMapping.ContainsKey(noteName))
        {
            Debug.LogError("Nota non trovata nella mappatura: " + noteName);
            finalX = finalY = finalZ = 0f;
            return null;
        }

        float spacingScale = lineSpacing / baseLineSpacing;
        float yPos = transform.position.y
                     + (notePositionMapping[noteName] * spacingScale)
                     + (octave - 3) * 3.5f * verticalOffset * spacingScale;

        if (!noteZIndex.ContainsKey(noteName))
        {
            Debug.LogError("Nota non trovata nella mappatura Z: " + noteName);
            finalX = finalY = finalZ = 0f;
            return null;
        }

        int zIndex = noteZIndex[noteName];
        float zPos = transform.position.z + noteZBaseOffset + (zIndex * noteZIncrement);

        GameObject noteInstance = Instantiate(notePrefab, new Vector3(xPos, yPos, zPos), Quaternion.identity, noteParent);

        SpriteRenderer spriteRenderer = noteInstance.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = noteData.color;
        }
        else
        {
            Debug.LogError("SpriteRenderer non trovato nel child del prefab della nota!");
        }

        Transform noteModel = noteInstance.transform.Find("NoteModel");
        if (noteModel != null)
        {
            Transform diesisObj = noteModel.Find("Diesis");
            Transform bemolleObj = noteModel.Find("Bemolle");

            if (diesisObj != null) diesisObj.gameObject.SetActive(false);
            if (bemolleObj != null) bemolleObj.gameObject.SetActive(false);

            if (noteName.Contains("Sharp") && diesisObj != null)
            {
                diesisObj.gameObject.SetActive(true);
            }

            if (noteName.Contains("Flat") && bemolleObj != null)
            {
                bemolleObj.gameObject.SetActive(true);
            }
        }
        else
        {
            Debug.LogWarning("NoteModel non trovato all'interno del prefab della nota. Impossibile attivare Diesis/Bemolle.");
        }

        finalX = xPos;
        finalY = yPos;
        finalZ = zPos;

        return noteInstance;
    }

    private void ClearScore()
    {
        if (noteParent != null)
        {
            foreach (Transform child in noteParent)
            {
                Destroy(child.gameObject);
            }
        }

        if (pentagramParent != null)
        {
            foreach (Transform child in pentagramParent)
            {
                Destroy(child.gameObject);
            }
        }
    }

    // Classe di appoggio per salvare info sulle note create
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

    // Determina se la nota è in uno spazio o su una linea
    // Se (yPos - baseY) / lineSpacing è vicino ad un intero => linea, altrimenti spazio
    private bool IsSpaceNote(float yPos)
    {
        float baseY = transform.position.y;
        float normalized = (yPos - baseY) / lineSpacing;

        // Controlliamo la vicinanza all'intero
        float nearestInt = Mathf.Round(normalized);
        float diff = Mathf.Abs(normalized - nearestInt);

        // Se differenza è molto piccola (es < 0.01), consideriamo che sia su linea
        // Altrimenti è uno spazio
        return diff > 0.01f;
    }
}
