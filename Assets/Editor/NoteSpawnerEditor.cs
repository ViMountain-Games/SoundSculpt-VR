#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomEditor(typeof(NoteSpawner))]
public class NoteSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        NoteSpawner spawner = (NoteSpawner)target;

        DrawDefaultInspector(); // Disegna l'Inspector standard

        // Sezione personalizzata per il mapping
        GUILayout.Space(10);
        EditorGUILayout.LabelField("Note Mappings", EditorStyles.boldLabel);

        if (spawner.noteMappings == null)
        {
            spawner.noteMappings = new List<NotePrefabMapping>();
        }

        // Mostra la lista di mapping
        for (int i = 0; i < spawner.noteMappings.Count; i++)
        {
            GUILayout.BeginHorizontal();

            // Campo NoteData
            spawner.noteMappings[i].noteData = (NoteData)EditorGUILayout.ObjectField(
                spawner.noteMappings[i].noteData, typeof(NoteData), false);

            // Campo Prefab
            spawner.noteMappings[i].prefab = (GameObject)EditorGUILayout.ObjectField(
                spawner.noteMappings[i].prefab, typeof(GameObject), false);

            // Bottone per rimuovere la riga
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                spawner.noteMappings.RemoveAt(i);
            }

            GUILayout.EndHorizontal();
        }

        // Bottone per aggiungere una nuova riga
        if (GUILayout.Button("Aggiungi Mapping"))
        {
            spawner.noteMappings.Add(new NotePrefabMapping());
        }

        GUILayout.Space(10);
        if (GUI.changed)
        {
            EditorUtility.SetDirty(spawner);
        }
    }
}
#endif
