using UnityEngine;
using System.Collections;

public class AlphaController : MonoBehaviour
{
    [Tooltip("Durata massima della transizione in secondi")]
    public float TransitionDuration = 1f;

    [Tooltip("Curva di interpolazione per l'aumento dell'alpha")]
    public AnimationCurve ActivationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("Curva di interpolazione per la diminuzione dell'alpha")]
    public AnimationCurve DeactivationCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [Tooltip("Valore minimo dell'alpha (default: 0)")]
    [Range(0f, 1f)]
    public float MinAlpha = 0f;

    [Tooltip("Valore massimo dell'alpha (default: 1)")]
    [Range(0f, 1f)]
    public float MaxAlpha = 1f;

    private Renderer _renderer;
    private Material _material;

    private float _currentAlpha;
    private float _transitionTime;
    private float _targetAlpha;
    private bool _isIncreasing;

    private const string AlphaProperty = "_Alpha";

    private void Awake()
    {
        // Ottieni il renderer e il materiale
        _renderer = GetComponent<Renderer>();
        if (_renderer != null)
        {
            _material = _renderer.material;
            _currentAlpha = Mathf.Clamp(_material.GetFloat(AlphaProperty), MinAlpha, MaxAlpha);
            _material.SetFloat(AlphaProperty, _currentAlpha);
        }
        _transitionTime = 0f;

        StartCoroutine(Blink());
    }

    private void Update()
    {
        if (_material == null || TransitionDuration <= 0f) return;

        // Controlla se la transizione è in corso
        if (!Mathf.Approximately(_currentAlpha, _targetAlpha))
        {
            // Calcola il progresso della transizione
            _transitionTime += Time.deltaTime / TransitionDuration;
            float progress = Mathf.Clamp01(_transitionTime);

            // Usa la curva appropriata per interpolare
            float curveValue = _isIncreasing
                ? ActivationCurve.Evaluate(progress)
                : DeactivationCurve.Evaluate(progress);

            // Aggiorna l'alpha
            _currentAlpha = Mathf.Lerp(MinAlpha, MaxAlpha, curveValue);
            _material.SetFloat(AlphaProperty, _currentAlpha);

            //Debug.Log($"CurrentAlpha: {_currentAlpha}, TargetAlpha: {_targetAlpha}");

            // Controlla se la transizione è completa
            if (progress >= 1f)
            {
                _currentAlpha = Mathf.Clamp(_targetAlpha, MinAlpha, MaxAlpha); // Forza il valore finale
                _material.SetFloat(AlphaProperty, _currentAlpha);
                _transitionTime = 0f; // Resetta il tempo di transizione
            }
        }
    }

    public IEnumerator Blink()
    {
        // Imposta l'alpha iniziale a 0 manualmente
        _currentAlpha = 0f;
        if (_material != null)
        {
            _material.SetFloat(AlphaProperty, _currentAlpha);
        }

        // Avvia l'incremento
        IncreaseAlpha();
        yield return new WaitForSeconds(1);

        // Avvia il decremento
        DecreaseAlpha();
    }


    public void IncreaseAlpha()
    {
        _isIncreasing = true;
        _targetAlpha = MaxAlpha;
        _transitionTime = 0f; // Resetta il tempo di transizione
    }

    public void DecreaseAlpha()
    {
        _isIncreasing = false;
        _targetAlpha = MinAlpha;
        _transitionTime = 0f; // Resetta il tempo di transizione
    }
}
