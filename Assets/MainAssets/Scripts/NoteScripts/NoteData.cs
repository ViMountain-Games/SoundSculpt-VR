using UnityEngine;

[CreateAssetMenu(fileName = "NoteData", menuName = "Notes/NoteData", order = 1)]
public class NoteData : ScriptableObject
{
    public string noteName;
    public AudioClip audioClip;
    public Color color;
}