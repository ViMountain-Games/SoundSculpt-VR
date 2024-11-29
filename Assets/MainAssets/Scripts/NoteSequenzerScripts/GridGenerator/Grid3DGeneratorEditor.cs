using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Grid3DGenerator))]
public class Grid3DGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Disegna il normale inspector
        DrawDefaultInspector();

        // Ottieni riferimento allo script
        Grid3DGenerator gridGenerator = (Grid3DGenerator)target;

        EditorGUILayout.Space();

        // Pulsante per generare la griglia
        if (GUILayout.Button("Generate Grid"))
        {
            gridGenerator.GenerateGrid();
        }

        // Pulsante per cancellare la griglia
        if (GUILayout.Button("Clear Grid"))
        {
            gridGenerator.ClearGrid();
        }

        EditorGUILayout.Space();

        // Mostra la matrice 3D se esiste
        if (gridGenerator.gridMatrix != null)
        {
            EditorGUILayout.LabelField("Grid Matrix", EditorStyles.boldLabel);

            for (int x = 0; x < gridGenerator.gridMatrix.GetLength(0); x++)
            {
                EditorGUILayout.LabelField($"Layer X: {x}", EditorStyles.miniBoldLabel);

                for (int y = 0; y < gridGenerator.gridMatrix.GetLength(1); y++)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"Row Y: {y}", GUILayout.Width(70));

                    for (int z = 0; z < gridGenerator.gridMatrix.GetLength(2); z++)
                    {
                        gridGenerator.gridMatrix[x, y, z] = (GameObject)EditorGUILayout.ObjectField(
                            gridGenerator.gridMatrix[x, y, z],
                            typeof(GameObject),
                            allowSceneObjects: true,
                            GUILayout.Width(50)
                        );
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Generate the grid to view the matrix.", MessageType.Info);
        }
    }
}
