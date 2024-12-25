using UnityEngine;

public class LevelManagerActions : MonoBehaviour
{
    /// <summary>
    /// Carica il prossimo livello usando il LevelManager.
    /// </summary>
    public void LoadNextLevel()
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.LoadNextLevel();
        }
        else
        {
            Debug.LogWarning("LevelManager non trovato! Assicurati che sia attivo nella scena iniziale.");
        }
    }

    /// <summary>
    /// Carica un livello specifico dato il suo indice nella lista sceneNames.
    /// </summary>
    /// <param name="index">Indice del livello da caricare.</param>
    public void LoadSpecificLevel(int index)
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.LoadSpecificLevel(index);
        }
        else
        {
            Debug.LogWarning("LevelManager non trovato! Assicurati che sia attivo nella scena iniziale.");
        }
    }

    /// <summary>
    /// Legge la progressione di caricamento dal LevelManager.
    /// </summary>
    /// <returns>La percentuale di caricamento (0..1).</returns>
    public float GetLoadingProgress()
    {
        if (LevelManager.Instance != null)
        {
            return LevelManager.Instance.LoadingProgress;
        }
        else
        {
            Debug.LogWarning("LevelManager non trovato! Assicurati che sia attivo nella scena iniziale.");
            return 0f;
        }
    }

    /// <summary>
    /// Resetta il progresso del gioco e cancella il file di salvataggio.
    /// </summary>
    public void ResetProgress()
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.ResetProgress();
        }
        else
        {
            Debug.LogWarning("LevelManager non trovato! Assicurati che sia attivo nella scena iniziale.");
        }
    }

    /// <summary>
    /// Salva manualmente il progresso attuale.
    /// </summary>
    public void SaveProgress()
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.SaveProgress();
        }
        else
        {
            Debug.LogWarning("LevelManager non trovato! Assicurati che sia attivo nella scena iniziale.");
        }
    }

    /// <summary>
    /// Ricarica manualmente i dati salvati da file.
    /// </summary>
    public void LoadProgress()
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.LoadProgress();
        }
        else
        {
            Debug.LogWarning("LevelManager non trovato! Assicurati che sia attivo nella scena iniziale.");
        }
    }
}
