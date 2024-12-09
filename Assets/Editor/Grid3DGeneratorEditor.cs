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
        Grid3DGenerator gridGenerator = (Grid3DGenerator)target;

        DrawDefaultInspector();

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Generate Grid")) gridGenerator.GenerateGrid();
        if (GUILayout.Button("Clear Grid")) gridGenerator.ClearGrid();
        EditorGUILayout.EndHorizontal();

        // Visualizzazione GridMatrix
        if (gridGenerator.gridMatrix != null)
        {
            EditorGUILayout.LabelField("Grid Matrix Visualization", EditorStyles.boldLabel);

            for (int x = 0; x < gridGenerator.gridMatrix.GetLength(0); x++)
            {
                if (x > 0)
                {
                    EditorGUILayout.Space();
                    DrawLine(Color.gray, 2);
                    EditorGUILayout.Space();
                }

                EditorGUILayout.LabelField("X = " + x, EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                for (int z = 0; z < gridGenerator.gridMatrix.GetLength(2); z++)
                {
                    EditorGUILayout.BeginVertical();
                    for (int y = gridGenerator.gridMatrix.GetLength(1) - 1; y >= 0; y--)
                    {
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
                                cellColor = Color.white;
                            }
                        }
                        else
                        {
                            cellColor = gridGenerator.emptyCellColor;
                        }

                        DrawColoredBox(cellColor);
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();
        }
        else
        {
            EditorGUILayout.HelpBox("Generate the grid to visualize the matrix.", MessageType.Info);
        }

        // Sezione Solution Grid Configuration
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Solution Grid Configuration", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Qui puoi configurare la combinazione corretta di NoteData per la griglia.\n" +
                                "Se una nota occupa più celle (Half=2, Whole=4), quando la selezioni verranno riempite le celle successive. Rimuovendo la nota dal primo segmento, verranno rimossi anche i segmenti successivi.",
                                MessageType.Info);

        showSolutionGrid = EditorGUILayout.Foldout(showSolutionGrid, "Apri/Chiudi Solution Grid", true);

        if (showSolutionGrid && gridGenerator.gridSizeX > 0 && gridGenerator.gridSizeY > 0 && gridGenerator.gridSizeZ > 0)
        {
            LoadAllNoteDataIfNeeded();

            for (int x = 0; x < gridGenerator.gridSizeX; x++)
            {
                if (x > 0)
                {
                    DrawLine(Color.yellow, 1);
                    EditorGUILayout.Space();
                }

                EditorGUILayout.LabelField("X = " + x + " (Solution Grid)", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                for (int z = 0; z < gridGenerator.gridSizeZ; z++)
                {
                    EditorGUILayout.BeginVertical();
                    for (int y = gridGenerator.gridSizeY - 1; y >= 0; y--)
                    {
                        NoteData current = gridGenerator.GetSolutionCell(x, y, z);
                        Color cellColor = current != null ? current.color : Color.gray;

                        Rect cellRect = DrawColoredBox(cellColor);

                        // Controlla il click sulla cella
                        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
                        {
                            if (cellRect.Contains(Event.current.mousePosition))
                            {
                                Event.current.Use();
                                ShowNoteSelectionMenu(gridGenerator, x, y, z, current, cellRect);
                            }
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndHorizontal();
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
            // Carica tutte le NoteData dal progetto
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

        // Mostra il menu in corrispondenza della cella
        Rect screenRect = EditorGUIUtility.GUIToScreenRect(cellRect);
        menu.DropDown(screenRect);
    }

    private void SetNoteData(Grid3DGenerator grid, int x, int y, int z, NoteData newNote)
    {
        NoteData oldNote = grid.GetSolutionCell(x, y, z); // nota precedente
        Undo.RecordObject(grid, "Change Solution Grid NoteData");
        grid.SetSolutionCell(x, y, z, newNote);
        EditorUtility.SetDirty(grid);

        // Se stiamo rimuovendo una nota (impostandola a null) e la vecchia nota era Half o Whole,
        // rimuoviamo anche le successive celle che erano state riempite.
        if (newNote == null && oldNote != null && oldNote.duration != NoteData.NoteDuration.Quarter)
        {
            int length = oldNote.duration == NoteData.NoteDuration.Half ? 2 : 4;
            for (int i = 1; i < length; i++)
            {
                int fillX = x + i;
                if (fillX < grid.gridSizeX)
                {
                    NoteData cellNote = grid.GetSolutionCell(fillX, y, z);
                    // Rimuoviamo solo se la cella ha la stessa nota, per evitare di cancellare note diverse inserite manualmente
                    if (cellNote == oldNote)
                    {
                        grid.SetSolutionCell(fillX, y, z, null);
                    }
                }
            }
            EditorUtility.SetDirty(grid);
        }

        // Se stiamo aggiungendo una nota Half o Whole, riempiamo le celle successive
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
            normal =
            {
                background = MakeTex(1, 1, cellColor)
            },
            fixedWidth = CellSize,
            fixedHeight = CellSize
        };

        Rect rect = GUILayoutUtility.GetRect(CellSize, CellSize, cellStyle);
        EditorGUI.DrawRect(rect, cellColor);
        GUI.Box(rect, GUIContent.none, cellStyle);

        return rect;
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

    private void DrawLine(Color color, float thickness = 1)
    {
        var rect = EditorGUILayout.GetControlRect(false, thickness);
        EditorGUI.DrawRect(rect, color);
    }
}
#endif
