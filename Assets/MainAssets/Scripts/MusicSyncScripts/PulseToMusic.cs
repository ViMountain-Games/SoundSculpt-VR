using UnityEngine;
using CustomInspector;

[RequireComponent(typeof(Transform))]
public class PulseToMusic : MonoBehaviour
{
    [Title("Riferimenti Audio")]
    [MessageBox("Trascina un componente AudioSource. Verr\u00e0 campionato lo spettro audio per pulsare l'oggetto.", MessageBoxType.Info)]
    [ForceFill]
    public AudioSource audioSource;

    [HorizontalLine("Parametri di Pulsazione", 2)]
    [TooltipBox("Intensit\u00e0 della pulsazione")]
    [Range(0.1f, 5f)]
    public float pulseStrength = 1.0f;

    [TooltipBox("Velocit\u00e0 di ritorno alla scala originale")]
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
    private float[] spectrumData = new float[16]; // Ridotto a 16 campioni direttamente

    private Vector3 baseScale;
    private float currentScaleMultiplier = 1.0f;

    private void Start()
    {
        baseScale = transform.localScale;

        if (!audioSource)
        {
            Debug.LogWarning("AudioSource non assegnato! Assegna un AudioSource valido.", this);
        }

        if (randomizeAtStart)
        {
            pulseStrength = Random.Range(pulseStrengthMin, pulseStrengthMax);
            damping = Random.Range(dampingMin, dampingMax);
            sensitivityThreshold = Random.Range(sensitivityThresholdMin, sensitivityThresholdMax);
        }

        objectTransform = transform;
    }

    private void Update()
    {
        if (audioSource == null || !audioSource.isPlaying)
            return;

        audioSource.GetSpectrumData(spectrumData, 0, FFTWindow.BlackmanHarris);

        float bassEnergy = 0f;
        for (int i = 0; i < spectrumData.Length; i++)
        {
            bassEnergy += spectrumData[i];
        }

        float effectiveEnergy = Mathf.Max(0f, bassEnergy - sensitivityThreshold);

        float targetScale = 1.0f + effectiveEnergy * pulseStrength;

        if (!Mathf.Approximately(currentScaleMultiplier, targetScale))
        {
            currentScaleMultiplier = Mathf.Lerp(currentScaleMultiplier, targetScale, Time.deltaTime * damping);
            objectTransform.localScale = baseScale * currentScaleMultiplier;
        }
    }
}
