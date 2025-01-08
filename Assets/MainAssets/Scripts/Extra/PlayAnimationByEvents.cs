using UnityEngine;

public class PlayAnimationByEvents : MonoBehaviour
{
    [SerializeField]
    private Animator animator; // Riferimento all'Animator da assegnare nell'Inspector
    [SerializeField]
    private string triggerName; // Nome del trigger da attivare, configurabile nell'Inspector

    /// <summary>
    /// Attiva il trigger specificato per avviare un'animazione.
    /// </summary>
    public void TriggerAnimation()
    {
        if (animator == null)
        {
            Debug.LogError("Errore: L'Animator non è assegnato.");
            return;
        }

        if (string.IsNullOrEmpty(triggerName))
        {
            Debug.LogError("Errore: Il nome del trigger non è stato specificato.");
            return;
        }

        try
        {
            // Imposta il trigger sull'Animator
            animator.SetTrigger(triggerName);
            Debug.Log($"Trigger '{triggerName}' attivato con successo.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Errore durante l'attivazione del trigger '{triggerName}': {ex.Message}");
        }
    }
}
