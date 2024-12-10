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

        private MusicScaleGenerator musicScaleGenerator;

        private void Awake()
        {
            // Il MusicScaleGenerator è già presente sul GameObject, niente AddComponent.
            // Si limita a recuperare il riferimento, senza operazioni costose.
            musicScaleGenerator = GetComponent<MusicScaleGenerator>();
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
                // Nessuna operazione costosa, la nota è già in cache.
                musicScaleGenerator.PlayNoteByPosition(gridX, gridY, gridZ, noteData.duration, noteData.fadeOutTime);
            }
        }
    }
}
