using UnityEngine;
using CustomInspector;

[RequireComponent(typeof(Transform))]
public class PulseToMusic : MonoBehaviour
{
    [Title("Riferimenti Audio")]
    [MessageBox("Trascina un componente AudioSource. Verrà campionato lo spettro audio per pulsare l'oggetto.", MessageBoxType.Info)]
    [ForceFill]
    public AudioSource audioSource;

    [HorizontalLine("Parametri di Pulsazione", 2)]
    [TooltipBox("Intensità della pulsazione")]
    [Range(0.1f, 5f)]
    public float pulseStrength = 1.0f;

    [TooltipBox("Velocità di ritorno alla scala originale")]
    [Range(0.1f, 10f)]
    public float damping = 2.0f;

    [TooltipBox("Soglia minima di attivazione della pulsazione (filtra i valori bassi)")]
    [Range(0f, 0.1f)]
    public float sensitivityThreshold = 0.01f;

    [Title("Configurazione Random", fontSize = 12)]
    [HorizontalLine(1, FixedColor.Gray)]
    [TooltipBox("Abilita randomizzazione dei parametri all'inizio")]
    public bool randomizeAtStart = false;

    [ShowIf(nameof(randomizeAtStart))]
    [TooltipBox("Min e Max per Pulse Strength Random")]
    public float pulseStrengthMin = 0.5f;
    [ShowIf(nameof(randomizeAtStart))]
    public float pulseStrengthMax = 3.0f;

    [ShowIf(nameof(randomizeAtStart))]
    [TooltipBox("Min e Max per Damping Random")]
    public float dampingMin = 1.0f;
    [ShowIf(nameof(randomizeAtStart))]
    public float dampingMax = 5.0f;

    [ShowIf(nameof(randomizeAtStart))]
    [TooltipBox("Min e Max per Sensitivity Threshold Random")]
    public float sensitivityThresholdMin = 0.005f;
    [ShowIf(nameof(randomizeAtStart))]
    public float sensitivityThresholdMax = 0.05f;

    [Title("Configurazione Interna", fontSize = 12)]
    [HorizontalLine(1, FixedColor.Gray)]
    [ReadOnly, SelfFill]
    public Transform objectTransform;

    [SerializeField, HideField]
    private float[] spectrumData = new float[1024];

    private Vector3 baseScale;
    private float currentScaleMultiplier = 1.0f;

    private void Start()
    {
        // Ottiene automaticamente la scala corrente come base.
        baseScale = transform.localScale;
        if (!audioSource)
            Debug.LogWarning("AudioSource non assegnato! Assegna un AudioSource valido.", this);

        // Randomizza i valori solo se il bool è attivato
        if (randomizeAtStart)
        {
            pulseStrength = Random.Range(pulseStrengthMin, pulseStrengthMax);
            damping = Random.Range(dampingMin, dampingMax);
            sensitivityThreshold = Random.Range(sensitivityThresholdMin, sensitivityThresholdMax);

            Debug.Log($"Parametri Randomizzati: pulseStrength={pulseStrength}, damping={damping}, sensitivityThreshold={sensitivityThreshold}", this);
        }
    }

    private void Update()
    {
        if (!audioSource || !audioSource.isPlaying)
            return;

        // Campiona lo spettro audio
        audioSource.GetSpectrumData(spectrumData, 0, FFTWindow.BlackmanHarris);

        // Calcola energia nelle basse frequenze
        float bassEnergy = 0f;
        for (int i = 0; i < 16; i++) // Solo le prime 16 frequenze (basse frequenze)
        {
            bassEnergy += spectrumData[i];
        }

        // Filtra i valori bassi in base alla soglia
        bassEnergy = Mathf.Max(0, bassEnergy - sensitivityThreshold);

        // Mappa l'energia ai valori di scala
        float targetScale = 1.0f + bassEnergy * pulseStrength;

        // Applica smorzamento
        currentScaleMultiplier = Mathf.Lerp(currentScaleMultiplier, targetScale, Time.deltaTime * damping);

        // Aggiorna la scala dell'oggetto
        objectTransform.localScale = baseScale * currentScaleMultiplier;
    }
}
