using UnityEngine;

[CreateAssetMenu(fileName = "NoteData", menuName = "Notes/NoteData", order = 1)]
public class NoteData : ScriptableObject
{
    public string noteName;
    public AudioClip audioClip;
    public Color color;

    [Tooltip("Durata della nota (es: 1/4, 2/4, 4/4)")]
    public NoteDuration duration;

    [Tooltip("Tempo di fade-out in secondi")]
    [Range(0f, 2f)]
    public float fadeOutTime = 0.5f;

    public enum NoteDuration
    {
        Quarter = 1, // 1/4
        Half = 2,    // 2/4
        Whole = 4    // 4/4
    }
}
