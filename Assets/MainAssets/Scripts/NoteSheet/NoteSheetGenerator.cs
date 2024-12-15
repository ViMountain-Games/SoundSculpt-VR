using UnityEngine;
using System.Collections.Generic;
using GridGen;
using CustomInspector;

public class ScoreGenerator : MonoBehaviour
{
    [Header("Note Prefabs")]
    public GameObject quarterNotePrefab; // 1/4
    public GameObject halfNotePrefab;    // 1/2
    public GameObject wholeNotePrefab;   // 4/4

    [Header("Note Spacing Settings")]
    public float horizontalSpacing = 1.0f;  // Spaziatura tra le note sull'asse X
    public float verticalOffset = 0.5f;     // Distanza verticale base (usata per le ottave)
    public float noteZIncrement = 0.05f;    // Incremento sull'asse Z dal basso verso l'alto
    public float noteXOffset = 0.5f;        // Offset orizzontale per spostare le note a destra
    public float noteZBaseOffset = -0.1f;   // Offset di base sull'asse Z per portare le note davanti al pentagramma

    [Header("Pentagram Settings")]
    public int numberOfLines = 5;           // Numero di linee del pentagramma
    public float lineSpacing = 0.5f;        // Spaziatura verticale tra le linee del pentagramma
    public float lineWidth = 0.1f;          // Spessore (altezza) di ogni linea prefab
    public Color lineColor = Color.black;   // Colore delle linee (se il prefab supporta un colore/mesh renderer)
    public Material lineMaterial;           // (Opzionale) Materiale da assegnare al prefab della linea

    [Header("Staff Line Prefab")]
    public GameObject staffLinePrefab;      // **Prefab** da usare al posto dei LineRenderer per ciascuna riga del pentagramma

    [Header("Staff Length Settings")]
    public float lengthMultiplier = 1.0f;   // (Legacy) Moltiplicatore per la lunghezza di base
    public float staffExtraLength = 1.0f;   // Offset extra per non far terminare lo spartito esattamente sull'ultima nota

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

    private void InitializePentagram()
    {
        if (pentagramParent == null)
        {
            pentagramParent = new GameObject("Pentagram").transform;
            pentagramParent.SetParent(transform);
            pentagramParent.localPosition = Vector3.zero;
        }

        // Se non esiste alcun prefab, usciamo
        if (staffLinePrefab == null)
        {
            Debug.LogError("Staff Line Prefab non assegnato! Impossibile generare il pentagramma.");
            return;
        }

        // Genera le linee del pentagramma usando il prefab anziché il LineRenderer
        for (int i = 0; i < numberOfLines; i++)
        {
            float yPos = i * lineSpacing;

            // Istanziamo il prefab della linea
            GameObject lineObject = Instantiate(staffLinePrefab, pentagramParent);
            lineObject.name = $"PentagramLine_{i}";

            // Posizioniamo la linea in modo che inizi e finisca dove serve
            // Per comodità, la ancoriamo dal centro (dipende da come è configurato il prefab),
            // quindi il "center" sarà a metà della linea
            // => Se vogliamo che la linea parta esattamente da transform.position.x,
            //    il center dev'essere shiftato a metà della lunghezza
            float lineCenterX = transform.position.x + lineLength * 0.5f;
            float lineCenterY = transform.position.y + yPos;
            float lineCenterZ = transform.position.z;

            lineObject.transform.position = new Vector3(lineCenterX, lineCenterY, lineCenterZ);

            // Ridimensioniamo la linea in base alla lunghezza calcolata
            Vector3 localScale = lineObject.transform.localScale;
            localScale.x = lineLength;  // L'asse X rappresenta la lunghezza orizzontale
            localScale.y = lineWidth;   // Lo spessore della linea (asse Y), se il prefab è orientato correttamente
            // Manteniamo localScale.z inalterato oppure lo settiamo a 1f se serve
            lineObject.transform.localScale = localScale;

            // Se vogliamo assegnare un materiale o un colore specifico al prefab
            // (dipende da come è fatto il prefab: MeshRenderer, SpriteRenderer, ecc.)
            MeshRenderer meshRenderer = lineObject.GetComponentInChildren<MeshRenderer>();
            if (meshRenderer != null && lineMaterial != null)
            {
                meshRenderer.material = lineMaterial;
            }
            // Se il prefab supporta un colore (es. MeshRenderer con un materiale standard),
            // si potrebbe tentare di cambiare il colore di emissione o albedo:
            // meshRenderer.material.color = lineColor;
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

        // Calcola la lunghezza del pentagramma fino all'ultima nota + offset extra
        // (Se non esistono note, fallback a gridGenerator.gridSizeX * horizontalSpacing * lengthMultiplier)
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
        GameObject notePrefab = null;
        switch (noteData.duration)
        {
            case NoteData.NoteDuration.Quarter:
                notePrefab = quarterNotePrefab;
                break;
            case NoteData.NoteDuration.Half:
                notePrefab = halfNotePrefab;
                break;
            case NoteData.NoteDuration.Whole:
                notePrefab = wholeNotePrefab;
                break;
        }

        if (notePrefab == null)
        {
            Debug.LogError("Nessun prefab assegnato per la durata nota: " + noteData.duration);
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
