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
        { "Do", -0.50f },
        { "Re", -0.25f },
        { "Mi",  0.00f },
        { "Fa",  0.25f },
        { "Sol", 0.50f },
        { "La",  0.75f },
        { "Si",  1.00f }
    };

    // Mappatura note -> indice per l'incremento sulla Z
    private Dictionary<string, int> noteZIndex = new Dictionary<string, int>
    {
        { "Do", 0 },
        { "Re", 1 },
        { "Mi", 2 },
        { "Fa", 3 },
        { "Sol",4 },
        { "La", 5 },
        { "Si", 6 }
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

        // Creiamo una lista temporanea per gli attuali tipi di nota
        List<string> currentNoteTypes = referenceNoteData.noteTypes;

        // Rimuoviamo gli elementi che non esistono più
        noteTypePrefabsList.RemoveAll(ntp => !currentNoteTypes.Contains(ntp.noteType));

        // Aggiungiamo gli elementi nuovi
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

        // Se non esiste alcun prefab per la linea del pentagramma, usciamo
        if (staffLinePrefab == null)
        {
            Debug.LogError("Staff Line Prefab non assegnato! Impossibile generare il pentagramma.");
            return;
        }

        // Genera le linee del pentagramma usando il prefab
        for (int i = 0; i < numberOfLines; i++)
        {
            float yPos = i * lineSpacing;

            // Istanziamo il prefab della linea
            GameObject lineObject = Instantiate(staffLinePrefab, pentagramParent);
            lineObject.name = $"PentagramLine_{i}";

            float lineCenterX = transform.position.x + lineLength * 0.5f;
            float lineCenterY = transform.position.y + yPos;
            float lineCenterZ = transform.position.z;

            lineObject.transform.position = new Vector3(lineCenterX, lineCenterY, lineCenterZ);

            // Ridimensioniamo la linea in base alla lunghezza calcolata
            Vector3 localScale = lineObject.transform.localScale;
            localScale.x = lineLength;  // L'asse X rappresenta la lunghezza orizzontale
            localScale.y = lineWidth;   // Lo spessore della linea (asse Y)
            lineObject.transform.localScale = localScale;

            // Se vogliamo assegnare un materiale o un colore specifico al prefab
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

        // Crea un parent per le note
        if (noteParent == null)
        {
            noteParent = new GameObject("Notes").transform;
            noteParent.SetParent(transform);
            noteParent.localPosition = Vector3.zero;
        }

        float maxNoteX = 0f;  // Per tracciare la nota più a destra

        // Scorre la configurazione della griglia di soluzione
        for (int x = 0; x < gridGenerator.gridSizeX; x++)
        {
            for (int z = 0; z < gridGenerator.gridSizeZ; z++)
            {
                int octave = gridGenerator.GetOctaveFromZ(z);
                for (int y = 0; y < gridGenerator.gridSizeY; y++)
                {
                    NoteData noteData = gridGenerator.GetSolutionCell(x, y, z);
                    if (noteData != null)
                    {
                        string noteName = gridGenerator.GetNoteNameFromY(y).ToString();

                        // Per le note di durata Half e Whole, creiamo solo la prima istanza orizzontale (x == 0)
                        if ((noteData.duration == NoteData.NoteDuration.Half || noteData.duration == NoteData.NoteDuration.Whole) && x > 0)
                            continue;

                        // Istanzia la nota e aggiorna maxNoteX
                        float actualXPos = CreateNote(noteData, noteName, x, octave);
                        if (actualXPos > maxNoteX) maxNoteX = actualXPos;
                    }
                }
            }
        }

        // Calcola la lunghezza del pentagramma
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

    private float CreateNote(NoteData noteData, string noteName, int x, int octave)
    {
        // Troviamo il set di prefab per il tipo di nota selezionato in noteData
        NoteTypePrefabs chosenTypePrefabs = noteTypePrefabsList.Find(ntp => ntp.noteType == noteData.SelectedNoteType);
        if (chosenTypePrefabs == null)
        {
            Debug.LogError("Nessun prefab configurato per il tipo di nota: " + noteData.SelectedNoteType);
            return 0f;
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
            return 0f;
        }

        float xPos = transform.position.x + (x * horizontalSpacing) + noteXOffset;

        float spacingScale = lineSpacing / baseLineSpacing;
        float yPos = transform.position.y
                     + (notePositionMapping[noteName] * spacingScale)
                     + (octave - 3) * 3.5f * verticalOffset * spacingScale;

        int zIndex = noteZIndex[noteName];
        float zPos = transform.position.z + noteZBaseOffset + (zIndex * noteZIncrement);

        Vector3 notePosition = new Vector3(xPos, yPos, zPos);

        GameObject noteInstance = Instantiate(notePrefab, notePosition, Quaternion.identity, noteParent);

        SpriteRenderer spriteRenderer = noteInstance.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = noteData.color;
        }
        else
        {
            Debug.LogError("SpriteRenderer non trovato nel child del prefab della nota!");
        }

        return xPos;
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

        // Rimuove eventuali linee già disegnate
        if (pentagramParent != null)
        {
            foreach (Transform child in pentagramParent)
            {
                Destroy(child.gameObject);
            }
        }
    }
}
