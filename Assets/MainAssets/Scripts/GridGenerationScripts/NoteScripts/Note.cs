using UnityEngine;
using NaughtyAttributes;

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

                // Applica il NoteData iniziale (se impostato) e il materiale associato.
                ApplyNoteDataAndMaterial(noteData);
            }
            else
            {
                Debug.LogWarning("MaterialTarget non è impostato. Il materiale non sarà assegnato.");
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
            }
        }

        /// <summary>
        /// Aggiorna il NoteData dell'oggetto e ri-applica il materiale associato.
        /// </summary>
        public void ApplyNoteDataAndMaterial(NoteData newNoteData)
        {
            // Aggiorna il riferimento al NoteData
            noteData = newNoteData;

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
                if (noteData != null && noteData.noteMaterial != null)
                {
                    objectRenderer.material = noteData.noteMaterial;
                }
            }
        }
    }
}
