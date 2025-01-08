using UnityEngine;
using GridGen;

public class NoteAnimatorController : MonoBehaviour
{
    [Header("Animator assegnato dall'Inspector")]
    public Animator animator;

    [Header("Note assegnata dall'Inspector")]
    public Note note;

    [Tooltip("Nome del parametro bool per il Floating nell'Animator")]
    public string floatingBoolName = "IsFloating";

    private void Start()
    {
        // Verifica che l'Animator e il Note siano assegnati
        if (animator == null)
        {
            Debug.LogError("[NoteAnimatorController] Animator non assegnato.");
        }

        if (note == null)
        {
            Debug.LogError("[NoteAnimatorController] Note non assegnato.");
        }
    }

    private void Update()
    {
        // Aggiorna il bool nell'Animator in ogni frame
        if (animator != null && note != null)
        {
            animator.SetBool(floatingBoolName, note.isPlaced);
        }
    }
}
