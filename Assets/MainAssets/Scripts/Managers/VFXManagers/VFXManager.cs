using UnityEngine;
using CustomInspector;

public class VFXManager : MonoBehaviour
{
    [Header("VFX Settings")]
    public GameObject vfxPrefab; // Il prefab del VFX da istanziare

    [Header("Spawn Settings")]
    public Transform spawnPoint; // Il Transform da cui prendere posizione e scala

    [HorizontalLine("Instantiate VFX")]

    [MessageBox("Press the button below to instantiate and destroy a VFX.", MessageBoxType.Info)]
    [Button(nameof(InstantiateAndDestroyVFX), tooltip = "Instantiates the VFX and destroys it after a specified duration.")]
    public float lifetime = 5f; // Durata predefinita per distruggere il VFX

    /// <summary>
    /// Istanzia un effetto visivo (VFX) nella posizione e scala definite, e lo distrugge dopo un tempo specificato.
    /// </summary>
    public void InstantiateAndDestroyVFX()
    {
        if (vfxPrefab == null || spawnPoint == null)
        {
            Debug.LogError("VFX Prefab o Spawn Point non assegnato.");
            return;
        }

        // Istanzia il VFX
        GameObject instance = Instantiate(vfxPrefab, spawnPoint.position, spawnPoint.rotation);

        // Applica la scala del spawnPoint al VFX istanziato
        instance.transform.localScale = spawnPoint.lossyScale;

        // Distruggi il VFX dopo il tempo specificato
        Destroy(instance, lifetime);
    }
}
