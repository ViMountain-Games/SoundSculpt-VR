using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CustomInspector;

public class MusicScaleGenerator : MonoBehaviour
{
    private AudioClip baseNote;

    [SelfFill(hideIfFilled: true), SerializeField]
    private AudioSource audioSource;

    private const int sampleRate = 44100;

    [Header("Octave Settings")]
    public int minOctave = 2;
    public int maxOctave = 4;

    private readonly Grid3DGenerator.NoteName[] noteNamesInOctave =
    {
        Grid3DGenerator.NoteName.Do,
        Grid3DGenerator.NoteName.DoSharp,
        Grid3DGenerator.NoteName.Re,
        Grid3DGenerator.NoteName.ReSharp,
        Grid3DGenerator.NoteName.Mi,
        Grid3DGenerator.NoteName.Fa,
        Grid3DGenerator.NoteName.FaSharp,
        Grid3DGenerator.NoteName.Sol,
        Grid3DGenerator.NoteName.SolSharp,
        Grid3DGenerator.NoteName.La,
        Grid3DGenerator.NoteName.LaSharp,
        Grid3DGenerator.NoteName.Si
    };

    private Dictionary<Grid3DGenerator.NoteName, int> noteOffsets = new Dictionary<Grid3DGenerator.NoteName, int>
    {
        {Grid3DGenerator.NoteName.Do, 0},
        {Grid3DGenerator.NoteName.DoSharp, 1},
        {Grid3DGenerator.NoteName.Re, 2},
        {Grid3DGenerator.NoteName.ReSharp, 3},
        {Grid3DGenerator.NoteName.Mi, 4},
        {Grid3DGenerator.NoteName.Fa, 5},
        {Grid3DGenerator.NoteName.FaSharp, 6},
        {Grid3DGenerator.NoteName.Sol, 7},
        {Grid3DGenerator.NoteName.SolSharp, 8},
        {Grid3DGenerator.NoteName.La, 9},
        {Grid3DGenerator.NoteName.LaSharp, 10},
        {Grid3DGenerator.NoteName.Si, 11}
    };

    private List<NoteDataEntry> notes = new List<NoteDataEntry>();

    private Grid3DGenerator gridGenerator;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        baseNote = GetComponent<Note>().noteData.audioClip;

        GenerateNotes();
    }

    private void Start()
    {
        gridGenerator = Grid3DGenerator.Instance;
        if (gridGenerator == null)
        {
            Debug.LogError("Grid3DGenerator instance not found!");
        }
    }

    private void GenerateNotes()
    {
        notes.Clear();
        for (int octave = minOctave; octave <= maxOctave; octave++)
        {
            foreach (Grid3DGenerator.NoteName noteName in noteNamesInOctave)
            {
                float frequency = CalculateFrequency(noteName, octave);
                notes.Add(new NoteDataEntry { Name = noteName, Octave = octave, Frequency = frequency });
            }
        }
    }

    private int GetMIDINoteNumber(Grid3DGenerator.NoteName noteName, int octave)
    {
        return (octave + 1) * 12 + noteOffsets[noteName];
    }

    private float CalculateFrequency(Grid3DGenerator.NoteName noteName, int octave)
    {
        int noteNumber = GetMIDINoteNumber(noteName, octave);
        return 440f * Mathf.Pow(2f, (noteNumber - 69f) / 12f);
    }

    public void PlayNoteByPosition(int x, int y, int z)
    {
        if (gridGenerator == null)
        {
            Debug.LogError("Grid3DGenerator instance not found!");
            return;
        }

        Grid3DGenerator.NoteName noteName = gridGenerator.GetNoteNameFromY(y);
        int octave = gridGenerator.GetOctaveFromZ(z);

        float frequency = CalculateFrequency(noteName, octave);

        Debug.Log($"Playing note {noteName} in octave {octave} at position ({x}, {y}, {z})");

        AudioClip newNote = GenerateNote(baseNote, frequency);
        audioSource.clip = newNote;
        audioSource.Play();
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

    public class NoteDataEntry
    {
        public Grid3DGenerator.NoteName Name;
        public int Octave;
        public float Frequency;
    }
}
