using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(NoteData))]
public class NoteDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Ottieni il riferimento all'oggetto corrente
        NoteData noteData = (NoteData)target;

        // Disegna il resto dell'inspector normalmente
        DrawDefaultInspector();

        // Dropdown dinamico per "selectedNoteTypeIndex"
        if (noteData.noteTypes != null && noteData.noteTypes.Count > 0)
        {
            string[] options = noteData.noteTypes.ToArray();
            noteData.selectedNoteTypeIndex = EditorGUILayout.Popup("Selected Note Type", noteData.selectedNoteTypeIndex, options);
        }
        else
        {
            EditorGUILayout.HelpBox("La lista dei tipi di note è vuota. Aggiungi almeno un elemento.", MessageType.Warning);
        }

        // Salva le modifiche
        if (GUI.changed)
        {
            EditorUtility.SetDirty(noteData);
        }
    }
}
