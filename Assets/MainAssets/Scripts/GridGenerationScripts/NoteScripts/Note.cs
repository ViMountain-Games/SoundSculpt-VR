using UnityEngine;
using NaughtyAttributes;
using UnityEngine.Events;

namespace GridGen
{
    [RequireComponent(typeof(MusicScaleGenerator))]
    public class Note : MonoBehaviour
    {
        [Expandable]
        public NoteData noteData;
        public int gridX, gridY, gridZ;

        [Tooltip("Il GameObject figlio a cui assegnare il materiale")]
        public GameObject materialTarget;

        [Tooltip("Controller del Particle System associato a questa nota")]
        public ParticleSystemController particleSystemController;

        [Header("Eventi")]
        public UnityEvent onNotePlayed;

        /// <summary>
        /// Indica se la nota è piazzata (true) o no (false).
        /// Viene gestito dal NotePicker quando questa nota viene assegnata come selectedObject.
        /// </summary>
        [Header("Stato della nota")]
        public bool isPlaced;

        [Header("Luci associate alla nota")]
        [Tooltip("Lista di Point Light a cui assegnare il colore della nota")]
        public Light[] pointLights;

        private MusicScaleGenerator musicScaleGenerator;
        private Renderer objectRenderer;

        private void Awake()
        {
            // Recupera il MusicScaleGenerator già presente sul GameObject.
            musicScaleGenerator = GetComponent<MusicScaleGenerator>();

            // Assicura che il target per il materiale sia impostato.
            if (materialTarget != null)
            {
                // Recupera o aggiunge un Renderer al GameObject figlio
                objectRenderer = materialTarget.GetComponent<Renderer>();
                if (objectRenderer == null)
                {
                    objectRenderer = materialTarget.AddComponent<Renderer>();
                }

                // Se c'è già un NoteData, lo applichiamo
                if (noteData != null)
                {
                    ApplyNoteDataAndMaterial(noteData);
                }
            }
            else
            {
                Debug.LogWarning("[Note] MaterialTarget non è impostato. Il materiale non sarà assegnato.");
            }
        }

        public void SetGridPosition(int x, int y, int z)
        {
            gridX = x;
            gridY = y;
            gridZ = z;
        }

        public void PlayNote()
        {
            if (musicScaleGenerator != null && noteData != null)
            {
                // Riproduce la nota basandosi sui dati del NoteData.
                musicScaleGenerator.PlayNoteByPosition(gridX, gridY, gridZ, noteData.duration, noteData.fadeOutTime);
                onNotePlayed.Invoke();
            }
        }

        /// <summary>
        /// Aggiorna il NoteData dell'oggetto e ri-applica il materiale associato,
        /// inclusa l'applicazione del gradient al ParticleSystemController (se assegnato),
        /// oltre ad aggiornare il colore delle luci.
        /// </summary>
        public void ApplyNoteDataAndMaterial(NoteData newNoteData)
        {
            // Aggiorna il riferimento al NoteData
            noteData = newNoteData;

            if (noteData == null)
            {
                Debug.LogWarning("[Note] Il nuovo NoteData è null, impossibile applicare materiale o gradient.");
                return;
            }

            // Se abbiamo un target per il materiale, assicuriamoci di avere anche un Renderer
            if (materialTarget != null)
            {
                if (objectRenderer == null)
                {
                    objectRenderer = materialTarget.GetComponent<Renderer>();
                    if (objectRenderer == null)
                    {
                        objectRenderer = materialTarget.AddComponent<Renderer>();
                    }
                }

                // Applica il materiale se presente
                if (noteData.noteMaterial != null)
                {
                    objectRenderer.material = noteData.noteMaterial;
                }
            }

            // Applica il Gradient del NoteData al ParticleSystemController (se esiste)
            if (particleSystemController != null)
            {
                particleSystemController.ApplyStartColorGradient(noteData.colorGradient);
            }
            else
            {
                Debug.LogWarning("[Note] particleSystemController non assegnato, impossibile applicare il gradient.");
            }

            // Aggiorna il colore delle luci se presente
            if (pointLights != null && pointLights.Length > 0)
            {
                foreach (Light pl in pointLights)
                {
                    if (pl != null)
                    {
                        pl.color = noteData.lightColor;
                    }
                }
            }
        }
    }
}
