using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NoteData", menuName = "Notes/NoteData", order = 1)]
public class NoteData : ScriptableObject
{
    public string noteName;
    public AudioClip audioClip;
    public Color color;

    [Tooltip("Gradient usato per il Particle System")]
    public Gradient colorGradient;

    [Tooltip("Gradient usato per la Trail Renderer")]
    public Gradient trailGradient;

    [Tooltip("Durata della nota (es: 1/4, 2/4, 4/4)")]
    public NoteDuration duration;

    [Tooltip("Tempo di fade-out in secondi")]
    [Range(0f, 2f)]
    public float fadeOutTime = 0.5f;

    [Tooltip("Lista configurabile dei tipi di note disponibili")]
    public List<string> noteTypes = new List<string> { "Melody", "Harmony", "Percussion", "FX" };

    [Tooltip("Indice del tipo selezionato dalla lista")]
    public int selectedNoteTypeIndex;

    [Tooltip("Materiale associato alla nota")]
    public Material noteMaterial;

    [Tooltip("Colore utilizzato per le luci associate alla nota")]
    public Color lightColor = Color.white;

    public string SelectedNoteType => noteTypes != null
        && selectedNoteTypeIndex >= 0
        && selectedNoteTypeIndex < noteTypes.Count
        ? noteTypes[selectedNoteTypeIndex]
        : "Nessuna Selezione";

    public enum NoteDuration
    {
        Quarter = 1, // 1/4
        Half = 2,    // 2/4
        Whole = 4    // 4/4
    }
}
