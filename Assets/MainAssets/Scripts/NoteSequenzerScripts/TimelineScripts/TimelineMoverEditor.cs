using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TimelineMover))]
public class TimelineMoverEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Disegna l'Inspector di base
        DrawDefaultInspector();

        // Ottieni il riferimento allo script
        TimelineMover mover = (TimelineMover)target;

        // Aggiungi il pulsante
        if (GUILayout.Button("Start Movement"))
        {
            mover.StartMovement();
        }
    }
}
