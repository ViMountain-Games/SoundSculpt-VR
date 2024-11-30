using UnityEditor;
using UnityEngine;
using CustomInspector;

[CustomEditor(typeof(Grid3DGenerator))]
public class Grid3DGeneratorEditor : Editor
{
    private const float CellSize = 20f;

    public override void OnInspectorGUI()
    {
        Grid3DGenerator gridGenerator = (Grid3DGenerator)target;

        DrawDefaultInspector();

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Generate Grid")) gridGenerator.GenerateGrid();
        if (GUILayout.Button("Clear Grid")) gridGenerator.ClearGrid();
        EditorGUILayout.EndHorizontal();

        if (gridGenerator.gridMatrix != null)
        {
            EditorGUILayout.LabelField("Grid Matrix Visualization", EditorStyles.boldLabel);

            for (int x = 0; x < gridGenerator.gridMatrix.GetLength(0); x++)
            {
                // Aggiungi linea divisoria per separare i layer
                if (x > 0)
                {
                    EditorGUILayout.Space();
                    GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(2)); // Linea divisoria
                    EditorGUILayout.Space();
                }

                EditorGUILayout.BeginHorizontal();

                for (int z = 0; z < gridGenerator.gridMatrix.GetLength(2); z++)
                {
                    EditorGUILayout.BeginVertical();

                    for (int y = 0; y < gridGenerator.gridMatrix.GetLength(1); y++)
                    {
                        GameObject cell = gridGenerator.gridMatrix[x, y, z];
                        GUIStyle cellStyle = new GUIStyle(GUI.skin.box)
                        {
                            normal =
                            {
                                background = MakeTex(
                                    1,
                                    1,
                                    cell == null ? gridGenerator.emptyCellColor : gridGenerator.filledCellColor
                                )
                            },
                            fixedWidth = CellSize,
                            fixedHeight = CellSize
                        };

                        GUILayout.Box("", cellStyle);
                    }

                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.EndHorizontal();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Generate the grid to visualize the matrix.", MessageType.Info);
        }
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
