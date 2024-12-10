using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace GridGen
{
    [RequireComponent(typeof(AudioSource))]
    public class MusicScaleGenerator : MonoBehaviour
    {
        private AudioSource audioSource;

        private const int sampleRate = 44100;

        [Header("Octave Settings")]
        public int minOctave = 2;
        public int maxOctave = 4;

        // Statici per evitare rigenerazioni continue
        private static bool notesPreGenerated = false;
        private static Dictionary<string, AudioClip> noteCache = new Dictionary<string, AudioClip>();
        private static AudioClip baseNoteClip;
        private static float baseFrequency = 261.63f; // Frequenza del Do base (C4 circa)

        // Lista di note considerata
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
            audioSource = GetComponent<AudioSource>();

            // Tenta di reperire la Grid una sola volta (se necessario)
            if (Grid3DGenerator.Instance != null)
                gridGenerator = Grid3DGenerator.Instance;

            // Se non abbiamo ancora fatto il pre-caricamento, lo facciamo ora.
            if (!notesPreGenerated)
            {
                PreInitializeBaseNote();
                PreGenerateNotes();
                notesPreGenerated = true;
            }
        }

        private void PreInitializeBaseNote()
        {
            // Cerca il componente Note e recupera il suo NoteData solo se non abbiamo ancora baseNoteClip
            if (baseNoteClip == null)
            {
                Note noteComponent = GetComponent<Note>();
                if (noteComponent != null && noteComponent.noteData != null && noteComponent.noteData.audioClip != null)
                {
                    baseNoteClip = noteComponent.noteData.audioClip;
                }
                else
                {
                    // Se non esiste la nota base, occorre assicurarsi di averne una di fallback.
                    // In caso di assenza, loggare un avviso. L'utente deve assicurarsi di avere una clip.
                    Debug.LogWarning("Base note clip not found on this Note. Assign a NoteData with an audioClip.");
                }
            }
        }

        private void PreGenerateNotes()
        {
            if (baseNoteClip == null)
            {
                // Non possiamo generare le note senza una nota base
                return;
            }

            // Genera tutte le note necessarie e salva nella cache statica
            for (int octave = minOctave; octave <= maxOctave; octave++)
            {
                foreach (Grid3DGenerator.NoteName noteName in noteNamesInOctave)
                {
                    string noteKey = GetNoteKey(noteName, octave);
                    if (!noteCache.ContainsKey(noteKey))
                    {
                        float frequency = CalculateFrequency(noteName, octave);
                        AudioClip newNote = GenerateNote(baseNoteClip, frequency);
                        noteCache[noteKey] = newNote;
                    }
                }
            }
        }

        private string GetNoteKey(Grid3DGenerator.NoteName noteName, int octave)
        {
            return noteName.ToString() + "_" + octave;
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

            string noteKey = GetNoteKey(noteName, octave);

            if (noteCache.TryGetValue(noteKey, out AudioClip newNote))
            {
                audioSource.clip = newNote;
                audioSource.volume = 1.0f; // Assicurarsi che il volume sia pieno all'inizio
                audioSource.Play();

                StartCoroutine(StopNoteWithFadeOut(duration, fadeOutTime));
            }
            else
            {
                Debug.LogError($"Note {noteName} in octave {octave} not found in cache.");
            }
        }

        private IEnumerator StopNoteWithFadeOut(NoteData.NoteDuration duration, float fadeOutTime)
        {
            float durationInSeconds = (float)duration / 4f;
            yield return new WaitForSeconds(durationInSeconds - fadeOutTime); // Attendere la durata prima del fade

            float startVolume = audioSource.volume;
            float fadeStep = startVolume / fadeOutTime;
            while (audioSource.volume > 0)
            {
                audioSource.volume -= fadeStep * Time.deltaTime;
                yield return null;
            }

            audioSource.Stop();
            audioSource.volume = startVolume; // Ripristina il volume
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
                Grid3DGenerator.NoteName.La => 9,
                Grid3DGenerator.NoteName.LaSharp => 10,
                Grid3DGenerator.NoteName.LaFlat => 8, // attento a note doppie
                Grid3DGenerator.NoteName.SiFlat => 10,
                Grid3DGenerator.NoteName.Si => 11,
                _ => 0
            };
        }

        private AudioClip GenerateNote(AudioClip originalClip, float targetFrequency)
        {
            // Se la base è nulla, ritorna subito
            if (originalClip == null) return null;

            float[] originalData = new float[originalClip.samples * originalClip.channels];
            originalClip.GetData(originalData, 0);

            float frequencyRatio = targetFrequency / baseFrequency;
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
}
