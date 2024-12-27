using System.Collections;
using UnityEngine;

public class ParticleSystemController : MonoBehaviour
{
    [Header("Particle System Settings")]
    [Tooltip("Il Particle System da controllare.")]
    public ParticleSystem targetParticleSystem;

    [Tooltip("Velocità di transizione del valore di emissione.")]
    public float transitionSpeed = 1f;

    [Header("Color Settings")]
    [Tooltip("Gradient usato come Start Color del Particle System.")]
    public Gradient startColorGradient;

    private Coroutine emissionChangeCoroutine;

    /// <summary>
    /// Viene chiamato in Editor quando si modificano proprietà via Inspector.
    /// Aggiorna lo Start Color se il target è assegnato.
    /// </summary>
    private void OnValidate()
    {
        if (targetParticleSystem != null && startColorGradient != null)
        {
            var mainModule = targetParticleSystem.main;
            mainModule.startColor = new ParticleSystem.MinMaxGradient(startColorGradient);
        }
    }

    private void Start()
    {
        if (targetParticleSystem != null && startColorGradient != null)
        {
            var mainModule = targetParticleSystem.main;
            mainModule.startColor = new ParticleSystem.MinMaxGradient(startColorGradient);
        }
    }

    /// <summary>
    /// Cambia il valore di emissione del Particle System in modo istantaneo.
    /// </summary>
    /// <param name="targetRate">Il valore target della emission rate over time.</param>
    public void SetEmissionRateInstant(float targetRate)
    {
        if (targetParticleSystem == null)
        {
            Debug.LogWarning("[ParticleSystemController] targetParticleSystem non assegnato!");
            return;
        }

        var emission = targetParticleSystem.emission;
        emission.rateOverTime = targetRate;
    }

    /// <summary>
    /// Cambia il valore di emissione del Particle System in modo fluido.
    /// </summary>
    /// <param name="targetRate">Il valore target della emission rate over time.</param>
    public void SetEmissionRateSmooth(float targetRate)
    {
        if (targetParticleSystem == null)
        {
            Debug.LogWarning("[ParticleSystemController] targetParticleSystem non assegnato!");
            return;
        }

        if (emissionChangeCoroutine != null)
        {
            StopCoroutine(emissionChangeCoroutine);
        }

        emissionChangeCoroutine = StartCoroutine(ChangeEmissionRateRoutine(targetRate));
    }

    private IEnumerator ChangeEmissionRateRoutine(float targetRate)
    {
        var emission = targetParticleSystem.emission;
        float startRate = emission.rateOverTime.constant;
        float elapsedTime = 0f;

        float duration = 1f / transitionSpeed;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);

            float newRate = Mathf.Lerp(startRate, targetRate, t);
            emission.rateOverTime = newRate;

            yield return null;
        }

        emission.rateOverTime = targetRate;
    }

    /// <summary>
    /// Applica dinamicamente il Gradient come Start Color al Particle System e
    /// aggiorna la variabile "startColorGradient" locale.
    /// </summary>
    /// <param name="gradient">Il Gradient da applicare.</param>
    public void ApplyStartColorGradient(Gradient gradient)
    {
        if (gradient == null)
        {
            Debug.LogWarning("[ParticleSystemController] gradient è null, impossibile applicarlo!");
            return;
        }

        // Aggiorna il campo locale in modo che OnValidate/Start future non lo sovrascrivano
        startColorGradient = gradient;

        if (targetParticleSystem == null)
        {
            Debug.LogWarning("[ParticleSystemController] targetParticleSystem non assegnato!");
            return;
        }

        Debug.Log($"[ParticleSystemController] ApplyStartColorGradient({gradient}) eseguito correttamente.");

        var mainModule = targetParticleSystem.main;
        mainModule.startColor = new ParticleSystem.MinMaxGradient(gradient);
    }
}
