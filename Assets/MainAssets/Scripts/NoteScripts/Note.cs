using UnityEngine;

public class Note : MonoBehaviour
{
    public NoteData noteData;
    public int gridX, gridY, gridZ;

    private MusicScaleGenerator musicScaleGenerator;

    private void Awake()
    {
        musicScaleGenerator = GetComponent<MusicScaleGenerator>();
        if (musicScaleGenerator == null)
        {
            // Se MusicScaleGenerator non è presente, lo aggiungiamo
            musicScaleGenerator = gameObject.AddComponent<MusicScaleGenerator>();
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
        if (musicScaleGenerator != null)
        {
            musicScaleGenerator.PlayNoteByPosition(gridX, gridY, gridZ);
        }
    }
}
