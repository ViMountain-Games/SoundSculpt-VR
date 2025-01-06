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

    [TooltipBox("Dimensione dell'array che contiene i campioni dello spettro (deve essere una potenza di due tra 64 e 8192)")]
    [Range(64, 8192)]
    public int spectrumSize = 64;

    [TooltipBox("Aggiorna lo spettro audio solo ogni X frame (1 significa ad ogni frame)")]
    [Range(1, 60)]
    public int framesBetweenUpdates = 1;

    [SerializeField, HideField]
    private float[] spectrumData;

    // Cache componenti per evitare chiamate ripetute
    private AudioSource cachedAudioSource;
    private Transform cachedTransform;

    // Variabili per la gestione della scala
    private Vector3 baseScale;
    private float currentScaleMultiplier = 1.0f;
    private float scaleVelocity; // Usata da SmoothDamp per un passaggio di riferimento

    // Per controllare ogni quanti frame leggere lo spettro audio
    private int frameCounter;

    // Flag per stabilire se l'oggetto è visibile in camera (usato per ridurre calcoli)
    private bool isVisible = true;

    private void OnValidate()
    {
        // Forza spectrumSize ad essere fra 64 e 8192
        spectrumSize = Mathf.Clamp(spectrumSize, 64, 8192);
        // Arrotonda alla potenza di due più vicina
        spectrumSize = Mathf.ClosestPowerOfTwo(spectrumSize);
    }

    private void Awake()
    {
        // Cache del Transform
        cachedTransform = transform;
        objectTransform = cachedTransform;

        // Cache dell'AudioSource (se presente)
        if (audioSource != null)
        {
            cachedAudioSource = audioSource;
        }
        else
        {
            Debug.LogWarning("AudioSource non assegnato! Assegna un AudioSource valido.", this);
        }
    }

    private void Start()
    {
        // Memorizziamo la scala di base
        baseScale = cachedTransform.localScale;

        // Inizializziamo l'array in base a spectrumSize
        spectrumData = new float[spectrumSize];

        // Eventuale randomizzazione iniziale
        if (randomizeAtStart)
        {
            pulseStrength        = Random.Range(pulseStrengthMin, pulseStrengthMax);
            damping              = Random.Range(dampingMin, dampingMax);
            sensitivityThreshold = Random.Range(sensitivityThresholdMin, sensitivityThresholdMax);
        }
    }

    private void Update()
    {
        // Se l'oggetto non è visibile o non c'è audio in riproduzione, facciamo meno calcoli o li saltiamo del tutto
        if (!isVisible || cachedAudioSource == null || !cachedAudioSource.isPlaying)
            return;

        // Richiamiamo GetSpectrumData solo ogni 'framesBetweenUpdates' frame
        frameCounter++;
        if (frameCounter < framesBetweenUpdates) return;
        frameCounter = 0;

        // Campioniamo lo spettro audio
        cachedAudioSource.GetSpectrumData(spectrumData, 0, FFTWindow.BlackmanHarris);

        // Calcoliamo l'energia dei campioni
        float bassEnergy = 0f;
        for (int i = 0; i < spectrumData.Length; i++)
        {
            bassEnergy += spectrumData[i];
        }

        // Applichiamo la soglia di sensibilità
        float effectiveEnergy = Mathf.Max(0f, bassEnergy - sensitivityThreshold);

        // Calcoliamo il valore target per la scala
        float targetScale = 1.0f + (effectiveEnergy * pulseStrength);

        // Aggiorniamo la scala in modo fluido con SmoothDamp
        currentScaleMultiplier = Mathf.SmoothDamp(
            currentScaleMultiplier,         // valore attuale
            targetScale,                    // valore target
            ref scaleVelocity,              // velocità (passata come reference)
            1f / damping                    // tempo di "ammortizzazione": più è grande 'damping', più è veloce il passaggio
        );

        cachedTransform.localScale = baseScale * currentScaleMultiplier;
    }

    // Questi metodi vengono chiamati se l'oggetto ha un Renderer:
    private void OnBecameVisible()
    {
        isVisible = true;
    }

    private void OnBecameInvisible()
    {
        isVisible = false;
    }
}
