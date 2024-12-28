using System.Collections;
using UnityEngine;

public class AudioVolumeController : MonoBehaviour
{
    public AudioSource audioSource; // Riferimento all'AudioSource
    public float volumeChangeDuration = 1f; // Durata del cambiamento del volume
    public float minVolume = 0f; // Valore minimo del volume
    public float maxVolume = 1f; // Valore massimo del volume

    private Coroutine volumeCoroutine;

    void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            Debug.LogError("AudioSource non trovato. Aggiungi un AudioSource al GameObject o assegnalo manualmente.");
        }
    }

    // Funzione per aumentare il volume fino al valore massimo, con un delay opzionale
    public void IncreaseVolume(float delay = 0f)
    {
        if (volumeCoroutine != null)
        {
            StopCoroutine(volumeCoroutine);
        }
        volumeCoroutine = StartCoroutine(ChangeVolume(maxVolume, delay));
    }

    // Funzione per diminuire il volume fino al valore minimo, con un delay opzionale
    public void DecreaseVolume(float delay = 0f)
    {
        if (volumeCoroutine != null)
        {
            StopCoroutine(volumeCoroutine);
        }
        volumeCoroutine = StartCoroutine(ChangeVolume(minVolume, delay));
    }

    // Coroutine per cambiare gradualmente il volume con un delay opzionale
    private IEnumerator ChangeVolume(float targetVolume, float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        float startVolume = audioSource.volume;
        float elapsedTime = 0f;

        while (elapsedTime < volumeChangeDuration)
        {
            elapsedTime += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsedTime / volumeChangeDuration);
            yield return null;
        }

        audioSource.volume = targetVolume;
    }
}
