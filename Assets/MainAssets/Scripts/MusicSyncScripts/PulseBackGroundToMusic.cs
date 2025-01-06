using UnityEngine;
using CustomInspector;

[RequireComponent(typeof(Transform))]
public class PulseBackGroundToMusic : MonoBehaviour
{
    [Title("Riferimenti Audio")]
    [MessageBox("Trascina un componente AudioSource. Verrà campionato lo spettro audio per regolare l'intensità del materiale.", MessageBoxType.Info)]
    [ForceFill]
    public AudioSource audioSource;

    [TooltipBox("Materiale sul quale verrà modificato il parametro '_Intensity'")]
    public Material targetMaterial;

    [TooltipBox("Nome del parametro di intensità nel materiale (default: _Intensity)")]
    public string intensityParameter = "_Intensity";

    [HorizontalLine("Parametri di Intensità", 2)]
    [TooltipBox("Intensità della pulsazione")]
    [Range(0.1f, 5f)]
    public float pulseStrength = 1.0f;

    [TooltipBox("Velocità di ritorno all'intensità originale")]
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
    [TooltipBox("Dimensione dell'array che contiene i campioni dello spettro (deve essere una potenza di due tra 64 e 8192)")]
    [Range(64, 8192)]
    public int spectrumSize = 64;

    [SerializeField, HideField]
    private float[] spectrumData;

    private float currentIntensity = 1.0f;

    // Questo metodo viene richiamato ogni volta che modifichi qualcosa in Inspector.
    // Qui forziamo spectrumSize a rientrare nei limiti validi di GetSpectrumData (64-8192) 
    // e lo convertiamo alla potenza di due più vicina.
    private void OnValidate()
    {
        // Forza spectrumSize ad essere fra 64 e 8192
        spectrumSize = Mathf.Clamp(spectrumSize, 64, 8192);
        // Arrotonda alla potenza di due più vicina
        spectrumSize = Mathf.ClosestPowerOfTwo(spectrumSize);
    }

    private void Start()
    {
        if (!audioSource)
        {
            Debug.LogWarning("AudioSource non assegnato! Assegna un AudioSource valido.", this);
        }

        // All'avvio, inizializziamo l'array in base al valore (già validato) di spectrumSize
        spectrumData = new float[spectrumSize];

        // Eventuale randomizzazione iniziale
        if (randomizeAtStart)
        {
            pulseStrength = Random.Range(pulseStrengthMin, pulseStrengthMax);
            damping = Random.Range(dampingMin, dampingMax);
            sensitivityThreshold = Random.Range(sensitivityThresholdMin, sensitivityThresholdMax);
        }
    }

    private void Update()
    {
        // Se non c'è audio in riproduzione o non abbiamo un materiale, fermiamo qui.
        if (audioSource == null || !audioSource.isPlaying || targetMaterial == null)
            return;

        // Campioniamo lo spettro audio
        audioSource.GetSpectrumData(spectrumData, 0, FFTWindow.BlackmanHarris);

        // Calcoliamo la somma dei campioni (bassEnergy)
        float bassEnergy = 0f;
        for (int i = 0; i < spectrumData.Length; i++)
        {
            bassEnergy += spectrumData[i];
        }

        // Applichiamo la soglia di sensibilità
        float effectiveEnergy = Mathf.Max(0f, bassEnergy - sensitivityThreshold);

        // Calcoliamo il valore target per l'intensità
        float targetIntensity = 1.0f + (effectiveEnergy * pulseStrength);

        // Interpoliamo verso il target in modo graduale
        if (!Mathf.Approximately(currentIntensity, targetIntensity))
        {
            currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.deltaTime * damping);
            // Impostiamo il parametro sul materiale
            targetMaterial.SetFloat(intensityParameter, currentIntensity);
        }
    }
}
