#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace GridGen
{
#if UNITY_EDITOR
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
#endif
}