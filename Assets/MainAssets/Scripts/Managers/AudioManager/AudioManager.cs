using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class VolumeSettings : MonoBehaviour
{
    [SerializeField] AudioMixer mixer;
    [SerializeField] Slider musicSlider;
    [SerializeField] Slider sfxSlider;
    [SerializeField] Slider masterSlider;
    [SerializeField] Slider uiSlider;

    const string MIXER_MUSIC = "MusicVolume";
    const string MIXER_SFX = "SfxVolume";
    const string MIXER_MASTER = "MasterVolume";
    const string MIXER_UI = "UIVolume";

    void Awake()
    {
        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        masterSlider.onValueChanged.AddListener(SetMasterVolume);
        uiSlider.onValueChanged.AddListener(SetUIVolume);
    }

    void SetMusicVolume(float value)
    {
        mixer.SetFloat(MIXER_MUSIC, Mathf.Log10(value) * 20);
    }

    void SetSFXVolume(float value)
    {
        mixer.SetFloat(MIXER_SFX, Mathf.Log10(value) * 20);
    }

    void SetMasterVolume(float value)
    {
        mixer.SetFloat(MIXER_MASTER, Mathf.Log10(value) * 20);
    }

    void SetUIVolume(float value)
    {
        mixer.SetFloat(MIXER_UI, Mathf.Log10(value) * 20);
    }
}
