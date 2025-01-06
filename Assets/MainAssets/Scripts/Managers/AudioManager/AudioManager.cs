using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using CustomInspector;

public class VolumeSettings : MonoBehaviour
{
    [HorizontalLine("Audio Mixer")]
    [ForceFill(errorMessage = "Audio Mixer must be assigned!")]
    [SerializeField] private AudioMixer mixer;

    [HorizontalLine("Sliders")]
    [ForceFill]
    [SerializeField] private Slider musicSlider;
    [ForceFill]
    [SerializeField] private Slider sfxSlider;
    [ForceFill]
    [SerializeField] private Slider masterSlider;
    [ForceFill]
    [SerializeField] private Slider uiSlider;

    [HorizontalLine("Slider Limits")]
    [SerializeField] private float musicMinVolume = 0.0001f;
    [SerializeField] private float musicMaxVolume = 1f;
    [SerializeField] private float sfxMinVolume = 0.0001f;
    [SerializeField] private float sfxMaxVolume = 1f;
    [SerializeField] private float masterMinVolume = 0.0001f;
    [SerializeField] private float masterMaxVolume = 1f;
    [SerializeField] private float uiMinVolume = 0.0001f;
    [SerializeField] private float uiMaxVolume = 1f;

    private const string MIXER_MUSIC = "MusicVolume";
    private const string MIXER_SFX = "SfxVolume";
    private const string MIXER_MASTER = "MasterVolume";
    private const string MIXER_UI = "UiVolume";

    private void Awake()
    {
        InitializeSliders();
        InitializeVolumeSettings();
    }

    private void InitializeSliders()
    {
        musicSlider.minValue = musicMinVolume;
        musicSlider.maxValue = musicMaxVolume;
        musicSlider.onValueChanged.AddListener(SetMusicVolume);

        sfxSlider.minValue = sfxMinVolume;
        sfxSlider.maxValue = sfxMaxVolume;
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);

        masterSlider.minValue = masterMinVolume;
        masterSlider.maxValue = masterMaxVolume;
        masterSlider.onValueChanged.AddListener(SetMasterVolume);

        uiSlider.minValue = uiMinVolume;
        uiSlider.maxValue = uiMaxVolume;
        uiSlider.onValueChanged.AddListener(SetUIVolume);
    }

    private void InitializeVolumeSettings()
    {
        SetMusicVolume(Mathf.Clamp(musicSlider.value, musicMinVolume, musicMaxVolume));
        SetSFXVolume(Mathf.Clamp(sfxSlider.value, sfxMinVolume, sfxMaxVolume));
        SetMasterVolume(Mathf.Clamp(masterSlider.value, masterMinVolume, masterMaxVolume));
        SetUIVolume(Mathf.Clamp(uiSlider.value, uiMinVolume, uiMaxVolume));
    }

    private void SetMusicVolume(float value)
    {
        float clampedValue = Mathf.Clamp(value, musicMinVolume, musicMaxVolume);
        musicSlider.value = clampedValue;
        mixer.SetFloat(MIXER_MUSIC, Mathf.Log10(clampedValue) * 20);
    }

    private void SetSFXVolume(float value)
    {
        float clampedValue = Mathf.Clamp(value, sfxMinVolume, sfxMaxVolume);
        sfxSlider.value = clampedValue;
        mixer.SetFloat(MIXER_SFX, Mathf.Log10(clampedValue) * 20);
    }

    private void SetMasterVolume(float value)
    {
        float clampedValue = Mathf.Clamp(value, masterMinVolume, masterMaxVolume);
        masterSlider.value = clampedValue;
        mixer.SetFloat(MIXER_MASTER, Mathf.Log10(clampedValue) * 20);
    }

    private void SetUIVolume(float value)
    {
        float clampedValue = Mathf.Clamp(value, uiMinVolume, uiMaxVolume);
        uiSlider.value = clampedValue;
        mixer.SetFloat(MIXER_UI, Mathf.Log10(clampedValue) * 20);
    }
}
