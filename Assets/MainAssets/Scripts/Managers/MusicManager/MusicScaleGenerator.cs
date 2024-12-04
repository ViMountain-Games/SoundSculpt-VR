using System.Collections;
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
        Grid3DGenerator.NoteName.ReFlat,
        Grid3DGenerator.NoteName.Mi,
        Grid3DGenerator.NoteName.MiFlat,
        Grid3DGenerator.NoteName.Fa,
        Grid3DGenerator.NoteName.FaSharp,
        Grid3DGenerator.NoteName.Sol,
        Grid3DGenerator.NoteName.SolSharp,
        Grid3DGenerator.NoteName.SolFlat,
        Grid3DGenerator.NoteName.La,
        Grid3DGenerator.NoteName.LaSharp,
        Grid3DGenerator.NoteName.LaFlat,
        Grid3DGenerator.NoteName.Si,
        Grid3DGenerator.NoteName.SiFlat
    };

    private Grid3DGenerator gridGenerator;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        baseNote = GetComponent<Note>().noteData.audioClip;
    }

    private void Start()
    {
        gridGenerator = Grid3DGenerator.Instance;
        if (gridGenerator == null)
        {
            Debug.LogError("Grid3DGenerator instance not found!");
        }
    }

    public void PlayNoteByPosition(int x, int y, int z, NoteData.NoteDuration duration, float fadeOutTime)
    {
        if (gridGenerator == null)
        {
            Debug.LogError("Grid3DGenerator instance not found!");
            return;
        }

        Grid3DGenerator.NoteName noteName = gridGenerator.GetNoteNameFromY(y);
        int octave = gridGenerator.GetOctaveFromZ(z);

        float frequency = CalculateFrequency(noteName, octave);

        Debug.Log($"Playing note {noteName} in octave {octave} with duration {duration} and fade-out time {fadeOutTime} at position ({x}, {y}, {z})");

        AudioClip newNote = GenerateNote(baseNote, frequency);
        audioSource.clip = newNote;
        audioSource.Play();

        StartCoroutine(StopNoteWithFadeOut(duration, fadeOutTime));
    }

    private IEnumerator StopNoteWithFadeOut(NoteData.NoteDuration duration, float fadeOutTime)
    {
        float durationInSeconds = (float)duration / 4f;
        yield return new WaitForSeconds(durationInSeconds - fadeOutTime); // Attendi prima del fade-out

        float startVolume = audioSource.volume;
        float fadeStep = startVolume / fadeOutTime;
        while (audioSource.volume > 0)
        {
            audioSource.volume -= fadeStep * Time.deltaTime;
            yield return null;
        }

        audioSource.Stop();
        audioSource.volume = startVolume; // Ripristina il volume originale
        Debug.Log("Note stopped with fade-out.");
    }

    private float CalculateFrequency(Grid3DGenerator.NoteName noteName, int octave)
    {
        int noteNumber = (octave + 1) * 12 + GetNoteOffset(noteName);
        return 440f * Mathf.Pow(2f, (noteNumber - 69f) / 12f);
    }

    private int GetNoteOffset(Grid3DGenerator.NoteName noteName)
    {
        return noteName switch
        {
            Grid3DGenerator.NoteName.Do => 0,
            Grid3DGenerator.NoteName.DoSharp => 1,
            Grid3DGenerator.NoteName.ReFlat => 1,
            Grid3DGenerator.NoteName.Re => 2,
            Grid3DGenerator.NoteName.ReSharp => 3,
            Grid3DGenerator.NoteName.MiFlat => 3,
            Grid3DGenerator.NoteName.Mi => 4,
            Grid3DGenerator.NoteName.Fa => 5,
            Grid3DGenerator.NoteName.FaSharp => 6,
            Grid3DGenerator.NoteName.SolFlat => 6,
            Grid3DGenerator.NoteName.Sol => 7,
            Grid3DGenerator.NoteName.SolSharp => 8,
            Grid3DGenerator.NoteName.LaFlat => 8,
            Grid3DGenerator.NoteName.La => 9,
            Grid3DGenerator.NoteName.LaSharp => 10,
            Grid3DGenerator.NoteName.SiFlat => 10,
            Grid3DGenerator.NoteName.Si => 11,
            _ => 0
        };
    }

    private AudioClip GenerateNote(AudioClip originalClip, float targetFrequency)
    {
        float[] originalData = new float[originalClip.samples * originalClip.channels];
        originalClip.GetData(originalData, 0);

        float originalFrequency = 261.63f; // Frequency of base note (Do)
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

        AudioClip newClip = AudioClip.Create("GeneratedNote", newData.Length / originalClip.channels, originalClip.channels, sampleRate, false);
        newClip.SetData(newData, 0);

        return newClip;
    }
}
