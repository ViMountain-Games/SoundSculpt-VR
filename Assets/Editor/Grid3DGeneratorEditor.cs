#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using GridGen;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
[CustomEditor(typeof(Grid3DGenerator))]
public class Grid3DGeneratorEditor : Editor
{
    private const float CellSize = 20f;
    private bool showSolutionGrid = true; // Foldout per la solution grid
    private static NoteData[] allNotes;

    public override void OnInspectorGUI()
    {
        EditorGUI.indentLevel = 0; // Nessun rientro
        Grid3DGenerator gridGenerator = (Grid3DGenerator)target;

        DrawDefaultInspector();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Generate Grid")) gridGenerator.GenerateGrid();
        if (GUILayout.Button("Clear Grid")) gridGenerator.ClearGrid();
        EditorGUILayout.EndHorizontal();

        // Visualizzazione GridMatrix (posizione attuale delle note)
        // Adesso la rendiamo coerente con la visualizzazione della solution grid.
        // Ciò significa che raggruppiamo anche la gridMatrix per ottave (Z).
        // Per ogni Z (ottava), una tabella:
        // - Riga di header: cella vuota + X in orizzontale
        // - Righe: per ogni Y, la nota a sinistra + celle per ogni X

        if (gridGenerator.gridMatrix != null)
        {
            EditorGUILayout.LabelField("Grid Matrix Visualization (Grouped by Octave)", EditorStyles.boldLabel);

            for (int z = 0; z < gridGenerator.gridSizeZ; z++)
            {
                int octave = gridGenerator.GetOctaveFromZ(z);
                EditorGUILayout.LabelField("Ottava: O" + octave, EditorStyles.boldLabel);

                // Header per l'asse X
                EditorGUILayout.BeginHorizontal();
                DrawLeftAlignedLabel("", CellSize); // cella vuota a sinistra
                for (int x = 0; x < gridGenerator.gridSizeX; x++)
                {
                    DrawLeftAlignedLabel("X" + x, CellSize);
                }
                EditorGUILayout.EndHorizontal();

                // Righe per le note (Y in verticale)
                for (int y = gridGenerator.gridSizeY - 1; y >= 0; y--)
                {
                    EditorGUILayout.BeginHorizontal();

                    var noteName = gridGenerator.GetNoteNameFromY(y);
                    DrawLeftAlignedLabel(gridGenerator.FormatNoteName(noteName), CellSize);

                    for (int x = 0; x < gridGenerator.gridSizeX; x++)
                    {
                        // Colore della cella in base all'oggetto presente nella gridMatrix
                        GameObject cell = gridGenerator.gridMatrix[x, y, z];
                        Color cellColor = gridGenerator.emptyCellColor;

                        if (cell != null)
                        {
                            Note noteComponent = cell.GetComponent<Note>();
                            if (noteComponent != null && noteComponent.noteData != null)
                            {
                                cellColor = noteComponent.noteData.color;
                            }
                            else
                            {
                                // Presenza di un oggetto non riconosciuto come nota
                                cellColor = Color.white;
                            }
                        }

                        DrawColoredBox(cellColor);
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.Space();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Generate the grid to visualize the matrix.", MessageType.Info);
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        // Sezione Solution Grid Configuration (Grouped by Octave)
        EditorGUILayout.LabelField("Solution Grid Configuration (Grouped by Octave)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Le ottave (Z) formano tabelle separate, con note (Y) verticali e X orizzontale, come nella grid matrix.",
                                MessageType.Info);

        showSolutionGrid = EditorGUILayout.Foldout(showSolutionGrid, "Apri/Chiudi Solution Grid", true);

        if (showSolutionGrid && gridGenerator.gridSizeX > 0 && gridGenerator.gridSizeY > 0 && gridGenerator.gridSizeZ > 0)
        {
            LoadAllNoteDataIfNeeded();

            for (int z = 0; z < gridGenerator.gridSizeZ; z++)
            {
                int octave = gridGenerator.GetOctaveFromZ(z);
                EditorGUILayout.LabelField("Ottava: O" + octave, EditorStyles.boldLabel);

                // Riga di header per X
                EditorGUILayout.BeginHorizontal();
                DrawLeftAlignedLabel("", CellSize);
                for (int x = 0; x < gridGenerator.gridSizeX; x++)
                {
                    DrawLeftAlignedLabel("X" + x, CellSize);
                }
                EditorGUILayout.EndHorizontal();

                // Righe per le note (Y)
                for (int y = gridGenerator.gridSizeY - 1; y >= 0; y--)
                {
                    EditorGUILayout.BeginHorizontal();

                    var noteName = gridGenerator.GetNoteNameFromY(y);
                    DrawLeftAlignedLabel(gridGenerator.FormatNoteName(noteName), CellSize);

                    for (int x = 0; x < gridGenerator.gridSizeX; x++)
                    {
                        NoteData current = gridGenerator.GetSolutionCell(x, y, z);
                        Color cellColor = current != null ? current.color : Color.gray;

                        Rect cellRect = DrawColoredBox(cellColor);

                        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
                        {
                            if (cellRect.Contains(Event.current.mousePosition))
                            {
                                Event.current.Use();
                                ShowNoteSelectionMenu(gridGenerator, x, y, z, current, cellRect);
                            }
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.Space();
            }
        }
        else if (showSolutionGrid)
        {
            EditorGUILayout.HelpBox("Invalid grid size.", MessageType.Info);
        }
    }

    private void LoadAllNoteDataIfNeeded()
    {
        if (allNotes == null || allNotes.Length == 0)
        {
            allNotes = Resources.FindObjectsOfTypeAll<NoteData>()
                                 .OrderBy(n => n.name).ToArray();
        }
    }

    private void ShowNoteSelectionMenu(Grid3DGenerator grid, int x, int y, int z, NoteData currentNote, Rect cellRect)
    {
        GenericMenu menu = new GenericMenu();

        // Opzione None
        menu.AddItem(new GUIContent("None"), currentNote == null, () => SetNoteData(grid, x, y, z, null));

        // Opzioni per tutte le NoteData
        if (allNotes != null)
        {
            foreach (var note in allNotes)
            {
                string noteName = note.name;
                menu.AddItem(new GUIContent(noteName), note == currentNote, () => SetNoteData(grid, x, y, z, note));
            }
        }

        Rect screenRect = EditorGUIUtility.GUIToScreenRect(cellRect);
        menu.DropDown(screenRect);
    }

    private void SetNoteData(Grid3DGenerator grid, int x, int y, int z, NoteData newNote)
    {
        NoteData oldNote = grid.GetSolutionCell(x, y, z);
        Undo.RecordObject(grid, "Change Solution Grid NoteData");
        grid.SetSolutionCell(x, y, z, newNote);
        EditorUtility.SetDirty(grid);

        if (newNote == null && oldNote != null && oldNote.duration != NoteData.NoteDuration.Quarter)
        {
            int length = oldNote.duration == NoteData.NoteDuration.Half ? 2 : 4;
            for (int i = 1; i < length; i++)
            {
                int fillX = x + i;
                if (fillX < grid.gridSizeX)
                {
                    NoteData cellNote = grid.GetSolutionCell(fillX, y, z);
                    if (cellNote == oldNote)
                    {
                        grid.SetSolutionCell(fillX, y, z, null);
                    }
                }
            }
            EditorUtility.SetDirty(grid);
        }

        if (newNote != null && newNote.duration != NoteData.NoteDuration.Quarter)
        {
            int length = (newNote.duration == NoteData.NoteDuration.Half) ? 2 :
                         (newNote.duration == NoteData.NoteDuration.Whole) ? 4 : 1;

            for (int i = 1; i < length; i++)
            {
                int fillX = x + i;
                if (fillX < grid.gridSizeX)
                {
                    grid.SetSolutionCell(fillX, y, z, newNote);
                }
            }
            EditorUtility.SetDirty(grid);
        }
    }

    private Rect DrawColoredBox(Color cellColor)
    {
        GUIStyle cellStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = MakeTex(1, 1, cellColor) },
            fixedWidth = CellSize,
            fixedHeight = CellSize
        };

        Rect rect = GUILayoutUtility.GetRect(CellSize, CellSize, cellStyle);
        EditorGUI.DrawRect(rect, cellColor);
        GUI.Box(rect, GUIContent.none, cellStyle);

        return rect;
    }

    private void DrawLeftAlignedLabel(string text, float width)
    {
        GUILayoutOption[] options = { GUILayout.Width(width), GUILayout.Height(CellSize) };
        var style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            wordWrap = false
        };
        EditorGUILayout.LabelField(text, style, options);
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
        {
            pix[i] = col;
        }

        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
}
#endif
