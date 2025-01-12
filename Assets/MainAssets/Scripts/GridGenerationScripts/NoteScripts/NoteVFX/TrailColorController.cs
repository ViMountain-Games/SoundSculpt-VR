using UnityEngine;

public class TrailColorController : MonoBehaviour
{
    [Header("Trail Renderer Settings")]
    [Tooltip("Il TrailRenderer da controllare.")]
    public TrailRenderer targetTrail;

    /// <summary>
    /// Applica dinamicamente il Gradient come colore della Trail e
    /// aggiorna la colorGradient del TrailRenderer.
    /// </summary>
    /// <param name="gradient">Il Gradient da applicare.</param>
    public void ApplyTrailColorGradient(Gradient gradient)
    {
        if (targetTrail == null)
        {
            Debug.LogWarning("[TrailColorController] targetTrail non assegnato!");
            return;
        }
        
        if (gradient == null)
        {
            Debug.LogWarning("[TrailColorController] gradient è null, impossibile applicarlo!");
            return;
        }

        if (targetTrail == null)
        {
            Debug.LogWarning("[TrailColorController] targetTrail non assegnato!");
            return;
        }

        targetTrail.colorGradient = gradient;
    }
}
