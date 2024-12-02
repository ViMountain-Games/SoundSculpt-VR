using System.Collections.Generic;
using UnityEngine;
using CustomInspector;

public class MusicScaleGenerator : MonoBehaviour
{
    [System.Serializable]
    public class OctaveMapping
    {
        public int zValue;
        public int octave;
    }

    [Header("Dynamic Octave Mapping")]
    [SerializeField]
    private List<OctaveMapping> octaveMappings = new List<OctaveMapping>();

    private AudioClip baseNote;

    [SelfFill(hideIfFilled: true), SerializeField]
    private AudioSource audioSource;

    private const int sampleRate = 44100;

    [Header("Octave Settings")]
    public int minOctave = 2;
    public int maxOctave = 4;

    private readonly string[] noteNamesInOctave =
    {
        "Do", "Do#", "Re", "Re#", "Mi", "Fa", "Fa#", "Sol", "Sol#", "La", "La#", "Si"
    };

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

    private List<NoteDataEntry> notes = new List<NoteDataEntry>();

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        baseNote = GetComponent<Note>().noteData.audioClip;

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
                notes.Add(new NoteDataEntry { Name = noteName, Octave = octave, Frequency = frequency });
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

    public void PlayNoteByPosition(int x, int y, int z)
    {
        string noteName = GetNoteNameFromY(y);
        int octave = GetOctaveFromZ(z);

        if (noteName == null)
        {
            Debug.LogError($"Invalid Y coordinate for note: {y}");
            return;
        }

        float frequency = CalculateFrequency(noteName, octave);

        Debug.Log($"Playing note {noteName} in octave {octave} at position ({x}, {y}, {z})");

        AudioClip newNote = GenerateNote(baseNote, frequency);
        audioSource.clip = newNote;
        audioSource.Play();
    }

    private string GetNoteNameFromY(int y)
    {
        if (y >= 0 && y < noteNamesInOctave.Length)
        {
            return noteNamesInOctave[y];
        }
        else
        {
            return null;
        }
    }

    private int GetOctaveFromZ(int z)
    {
        foreach (var mapping in octaveMappings)
        {
            if (mapping.zValue == z)
            {
                return mapping.octave;
            }
        }

        Debug.LogWarning($"No mapping found for z: {z}. Using default octave {minOctave}.");
        return minOctave; // Default octave if no mapping is found
    }

    private AudioClip GenerateNote(AudioClip originalClip, float targetFrequency)
    {
        float[] originalData = new float[originalClip.samples * originalClip.channels];
        originalClip.GetData(originalData, 0);

        float originalFrequency = 261.63f;
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

    public class NoteDataEntry
    {
        public string Name;
        public int Octave;
        public float Frequency;
    }
}
