using Autohand;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using GridGen;

[HelpURL("https://app.gitbook.com/s/5zKO0EvOjzUDeT2aiFk3/auto-hand/extras/collision-sounds")]
public class CollisionSound : MonoBehaviour
{
    [Tooltip("The layers that cause the sound to play")]
    public LayerMask collisionTriggers = ~0;
    [Tooltip("Source to play sound from")]
    public AudioSource source;
    [Tooltip("AudioClip da riprodurre. Può essere assegnato manualmente dall'Inspector o derivato da NoteData.")]
    public AudioClip clip;
    [Space]
    [Tooltip("Source to play sound from")]
    public AnimationCurve velocityVolumeCurve = AnimationCurve.Linear(0, 0, 1, 1);

    public float volumeAmp = 0.8f;
    public float velocityAmp = 0.5f;
    public float soundRepeatDelay = 0.2f;

    [Tooltip("Event triggered when the sound is played")]
    public UnityEvent onSoundPlayed; // Aggiunto evento Unity

    [Tooltip("Oggetto Note che contiene il NoteData con l'AudioClip da usare")]
    public Note noteReference;

    Rigidbody body;
    bool canPlaySound = true;
    Coroutine playSoundRoutine;

    private void Start()
    {
        body = GetComponent<Rigidbody>();

        // Recupera l'AudioClip dal NoteData se disponibile
        if (noteReference != null && noteReference.noteData != null && noteReference.noteData.audioClip != null)
        {
            clip = noteReference.noteData.audioClip;
        }

        if (clip == null && source != null && source.clip == null)
        {
            Debug.LogWarning("Nessun AudioClip assegnato. Si prega di assegnarlo manualmente nell'Inspector o tramite NoteData.");
        }

        // So the sound doesn't play when falling in place on start
        StartCoroutine(SoundPlayBuffer(1f));
    }

    private void OnDisable()
    {
        if (playSoundRoutine != null)
            StopCoroutine(playSoundRoutine);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (body == null && !gameObject.CanGetComponent(out body))
            return;

        if (canPlaySound && collisionTriggers == (collisionTriggers | (1 << collision.gameObject.layer)))
        {
            if (source != null && source.enabled)
            {
                if (collision.collider.attachedRigidbody == null || collision.collider.attachedRigidbody.mass > 0.0000001f)
                {
                    if (clip != null || source.clip != null)
                    {
                        source.PlayOneShot(clip == null ? source.clip : clip, velocityVolumeCurve.Evaluate(collision.relativeVelocity.magnitude * velocityAmp) * volumeAmp);

                        // Richiama l'evento Unity quando il suono viene riprodotto
                        onSoundPlayed?.Invoke();

                        if (playSoundRoutine != null)
                            StopCoroutine(playSoundRoutine);
                        playSoundRoutine = StartCoroutine(SoundPlayBuffer());
                    }
                }
            }
        }
    }

    IEnumerator SoundPlayBuffer()
    {
        canPlaySound = false;
        yield return new WaitForSeconds(soundRepeatDelay);
        canPlaySound = true;
        playSoundRoutine = null;
    }

    IEnumerator SoundPlayBuffer(float time)
    {
        canPlaySound = false;
        yield return new WaitForSeconds(time);
        canPlaySound = true;
        playSoundRoutine = null;
    }
}
