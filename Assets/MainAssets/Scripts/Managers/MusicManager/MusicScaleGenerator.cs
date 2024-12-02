using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CustomInspector;

public class MusicScaleGenerator : MonoBehaviour
{
    [ForceFill, SerializeField]
    private AudioClip baseNote; // L'audioclip della tua nota di base (es. Do)

    [SelfFill(hideIfFilled: true), SerializeField]
    private AudioSource audioSource; // AudioSource da cui verranno riprodotte le note

    private const int sampleRate = 44100; // Frequenza di campionamento standard

    [Header("Octave Settings")]
    public int minOctave = 2;
    public int maxOctave = 4;

    // Nomi delle note all'interno di un'ottava
    private readonly string[] noteNamesInOctave =
    {
        "Do", "Do#", "Re", "Re#", "Mi", "Fa", "Fa#", "Sol", "Sol#", "La", "La#", "Si"
    };

    // Mappatura dei nomi delle note ai semitoni
    private Dictionary<string, int> noteOffsets = new Dictionary<string, int>
    {
        {"Do", 0},
        {"Do#", 1},
        {"Re", 2},
        {"Re#", 3},
        {"Mi", 4},
        {"Fa", 5},
        {"Fa#", 6},
        {"Sol", 7},
        {"Sol#", 8},
        {"La", 9},
        {"La#", 10},
        {"Si", 11}
    };

    private List<Note> notes = new List<Note>();

    [HorizontalLine("Play Notes - Octave 2")]
    [Button(nameof(PlayDoOctave2), label = "Do (Octave 2)")]
    public bool playDoOctave2;

    [Button(nameof(PlayDoSharpOctave2), label = "Do# (Octave 2)")]
    public bool playDoSharpOctave2;

    [Button(nameof(PlayReOctave2), label = "Re (Octave 2)")]
    public bool playReOctave2;

    [Button(nameof(PlayReSharpOctave2), label = "Re# (Octave 2)")]
    public bool playReSharpOctave2;

    [Button(nameof(PlayMiOctave2), label = "Mi (Octave 2)")]
    public bool playMiOctave2;

    [Button(nameof(PlayFaOctave2), label = "Fa (Octave 2)")]
    public bool playFaOctave2;

    [Button(nameof(PlayFaSharpOctave2), label = "Fa# (Octave 2)")]
    public bool playFaSharpOctave2;

    [Button(nameof(PlaySolOctave2), label = "Sol (Octave 2)")]
    public bool playSolOctave2;

    [Button(nameof(PlaySolSharpOctave2), label = "Sol# (Octave 2)")]
    public bool playSolSharpOctave2;

    [Button(nameof(PlayLaOctave2), label = "La (Octave 2)")]
    public bool playLaOctave2;

    [Button(nameof(PlayLaSharpOctave2), label = "La# (Octave 2)")]
    public bool playLaSharpOctave2;

    [Button(nameof(PlaySiOctave2), label = "Si (Octave 2)")]
    public bool playSiOctave2;

    [HorizontalLine("Play Notes - Octave 3")]
    [Button(nameof(PlayDoOctave3), label = "Do (Octave 3)")]
    public bool playDoOctave3;

    [Button(nameof(PlayDoSharpOctave3), label = "Do# (Octave 3)")]
    public bool playDoSharpOctave3;

    [Button(nameof(PlayReOctave3), label = "Re (Octave 3)")]
    public bool playReOctave3;

    [Button(nameof(PlayReSharpOctave3), label = "Re# (Octave 3)")]
    public bool playReSharpOctave3;

    [Button(nameof(PlayMiOctave3), label = "Mi (Octave 3)")]
    public bool playMiOctave3;

    [Button(nameof(PlayFaOctave3), label = "Fa (Octave 3)")]
    public bool playFaOctave3;

    [Button(nameof(PlayFaSharpOctave3), label = "Fa# (Octave 3)")]
    public bool playFaSharpOctave3;

    [Button(nameof(PlaySolOctave3), label = "Sol (Octave 3)")]
    public bool playSolOctave3;

    [Button(nameof(PlaySolSharpOctave3), label = "Sol# (Octave 3)")]
    public bool playSolSharpOctave3;

    [Button(nameof(PlayLaOctave3), label = "La (Octave 3)")]
    public bool playLaOctave3;

    [Button(nameof(PlayLaSharpOctave3), label = "La# (Octave 3)")]
    public bool playLaSharpOctave3;

    [Button(nameof(PlaySiOctave3), label = "Si (Octave 3)")]
    public bool playSiOctave3;

    [HorizontalLine("Play Notes - Octave 4")]
    [Button(nameof(PlayDoOctave4), label = "Do (Octave 4)")]
    public bool playDoOctave4;

    [Button(nameof(PlayDoSharpOctave4), label = "Do# (Octave 4)")]
    public bool playDoSharpOctave4;

    [Button(nameof(PlayReOctave4), label = "Re (Octave 4)")]
    public bool playReOctave4;

    [Button(nameof(PlayReSharpOctave4), label = "Re# (Octave 4)")]
    public bool playReSharpOctave4;

    [Button(nameof(PlayMiOctave4), label = "Mi (Octave 4)")]
    public bool playMiOctave4;

    [Button(nameof(PlayFaOctave4), label = "Fa (Octave 4)")]
    public bool playFaOctave4;

    [Button(nameof(PlayFaSharpOctave4), label = "Fa# (Octave 4)")]
    public bool playFaSharpOctave4;

    [Button(nameof(PlaySolOctave4), label = "Sol (Octave 4)")]
    public bool playSolOctave4;

    [Button(nameof(PlaySolSharpOctave4), label = "Sol# (Octave 4)")]
    public bool playSolSharpOctave4;

    [Button(nameof(PlayLaOctave4), label = "La (Octave 4)")]
    public bool playLaOctave4;

    [Button(nameof(PlayLaSharpOctave4), label = "La# (Octave 4)")]
    public bool playLaSharpOctave4;

    [Button(nameof(PlaySiOctave4), label = "Si (Octave 4)")]
    public bool playSiOctave4;

    [HorizontalLine("Play All Notes")]
    [Button(nameof(PlayAllNotes), tooltip = "Riproduce tutte le note in sequenza.")]
    public bool playAllNotesButton;

    private void Awake()
    {
        GenerateNotes();
    }

    private void GenerateNotes()
    {
        notes.Clear();
        for (int octave = minOctave; octave <= maxOctave; octave++)
        {
            foreach (string noteName in noteNamesInOctave)
            {
                float frequency = CalculateFrequency(noteName, octave);
                notes.Add(new Note { Name = noteName, Octave = octave, Frequency = frequency });
            }
        }
    }

    private int GetMIDINoteNumber(string noteName, int octave)
    {
        return (octave + 1) * 12 + noteOffsets[noteName];
    }

    private float CalculateFrequency(string noteName, int octave)
    {
        int noteNumber = GetMIDINoteNumber(noteName, octave);
        return 440f * Mathf.Pow(2f, (noteNumber - 69f) / 12f);
    }

    public void PlayNoteByName(string noteName, int octave)
    {
        if (notes == null || notes.Count == 0)
        {
            GenerateNotes();
        }

        Note note = notes.Find(n => n.Name == noteName && n.Octave == octave);
        if (note != null)
        {
            AudioClip newNote = GenerateNote(baseNote, note.Frequency);
            audioSource.clip = newNote;
            audioSource.Play();
        }
        else
        {
            Debug.LogWarning($"Nota '{noteName}' nell'ottava {octave} non trovata!");
        }
    }

    private void PlayAllNotes()
    {
        StartCoroutine(PlayAllGeneratedNotes());
    }

    private IEnumerator PlayAllGeneratedNotes()
    {
        foreach (Note note in notes)
        {
            AudioClip newNote = GenerateNote(baseNote, note.Frequency);
            audioSource.clip = newNote;
            audioSource.Play();
            yield return new WaitForSeconds(0.5f);
        }
    }

    private AudioClip GenerateNote(AudioClip originalClip, float targetFrequency)
    {
        float[] originalData = new float[originalClip.samples * originalClip.channels];
        originalClip.GetData(originalData, 0);

        float originalFrequency = 261.63f; // Frequenza della nota di base (Do)
        float frequencyRatio = targetFrequency / originalFrequency;

        int newSampleCount = Mathf.CeilToInt(originalData.Length / frequencyRatio);
        float[] newData = new float[newSampleCount];

        for (int i = 0; i < newSampleCount; i++)
        {
            float index = i * frequencyRatio;
            int i1 = Mathf.FloorToInt(index);
            int i2 = Mathf.Min(i1 + 1, originalData.Length - 1);

            float t = index - i1;
            newData[i] = Mathf.Lerp(originalData[i1], originalData[i2], t);
        }

        AudioClip newClip = AudioClip.Create("Note", newData.Length / originalClip.channels, originalClip.channels, sampleRate, false);
        newClip.SetData(newData, 0);

        return newClip;
    }

    public class Note
    {
        public string Name;
        public int Octave;
        public float Frequency;
    }

    // Metodi per riprodurre le note nell'ottava 2
    private void PlayDoOctave2() => PlayNoteByName("Do", 2);
    private void PlayDoSharpOctave2() => PlayNoteByName("Do#", 2);
    private void PlayReOctave2() => PlayNoteByName("Re", 2);
    private void PlayReSharpOctave2() => PlayNoteByName("Re#", 2);
    private void PlayMiOctave2() => PlayNoteByName("Mi", 2);
    private void PlayFaOctave2() => PlayNoteByName("Fa", 2);
    private void PlayFaSharpOctave2() => PlayNoteByName("Fa#", 2);
    private void PlaySolOctave2() => PlayNoteByName("Sol", 2);
    private void PlaySolSharpOctave2() => PlayNoteByName("Sol#", 2);
    private void PlayLaOctave2() => PlayNoteByName("La", 2);
    private void PlayLaSharpOctave2() => PlayNoteByName("La#", 2);
    private void PlaySiOctave2() => PlayNoteByName("Si", 2);

    // Metodi per riprodurre le note nell'ottava 3
    private void PlayDoOctave3() => PlayNoteByName("Do", 3);
    private void PlayDoSharpOctave3() => PlayNoteByName("Do#", 3);
    private void PlayReOctave3() => PlayNoteByName("Re", 3);
    private void PlayReSharpOctave3() => PlayNoteByName("Re#", 3);
    private void PlayMiOctave3() => PlayNoteByName("Mi", 3);
    private void PlayFaOctave3() => PlayNoteByName("Fa", 3);
    private void PlayFaSharpOctave3() => PlayNoteByName("Fa#", 3);
    private void PlaySolOctave3() => PlayNoteByName("Sol", 3);
    private void PlaySolSharpOctave3() => PlayNoteByName("Sol#", 3);
    private void PlayLaOctave3() => PlayNoteByName("La", 3);
    private void PlayLaSharpOctave3() => PlayNoteByName("La#", 3);
    private void PlaySiOctave3() => PlayNoteByName("Si", 3);

    // Metodi per riprodurre le note nell'ottava 4
    private void PlayDoOctave4() => PlayNoteByName("Do", 4);
    private void PlayDoSharpOctave4() => PlayNoteByName("Do#", 4);
    private void PlayReOctave4() => PlayNoteByName("Re", 4);
    private void PlayReSharpOctave4() => PlayNoteByName("Re#", 4);
    private void PlayMiOctave4() => PlayNoteByName("Mi", 4);
    private void PlayFaOctave4() => PlayNoteByName("Fa", 4);
    private void PlayFaSharpOctave4() => PlayNoteByName("Fa#", 4);
    private void PlaySolOctave4() => PlayNoteByName("Sol", 4);
    private void PlaySolSharpOctave4() => PlayNoteByName("Sol#", 4);
    private void PlayLaOctave4() => PlayNoteByName("La", 4);
    private void PlayLaSharpOctave4() => PlayNoteByName("La#", 4);
    private void PlaySiOctave4() => PlayNoteByName("Si", 4);
}
