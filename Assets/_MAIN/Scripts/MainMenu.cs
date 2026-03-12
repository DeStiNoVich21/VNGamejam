using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private Slider volumeSlider, sfxSlider;
    [SerializeField] private AudioMixer audioMixer;

    private const string VolumeKey = "Music";
    private const string SFXKey = "SFX";

    private void Awake()
    {
        float volume = PlayerPrefs.GetFloat(VolumeKey, 0.5f);
        float sfxvol = PlayerPrefs.GetFloat(SFXKey, 0.5f);

        SetMixer(VolumeKey, volume);
        SetMixer(SFXKey, sfxvol);

        volumeSlider.value = volume;
        sfxSlider.value = sfxvol;

        volumeSlider.onValueChanged.AddListener(v =>
        {
            PlayerPrefs.SetFloat(VolumeKey, v);
            SetMixer(VolumeKey, v);
        });

        sfxSlider.onValueChanged.AddListener(v =>
        {
            PlayerPrefs.SetFloat(SFXKey, v);
            SetMixer(SFXKey, v);
        });
    }

    void SetMixer(string key, float value)
    {
        float db = Mathf.Lerp(-80f, 0f, value);
        audioMixer.SetFloat(key, db);
    }

    public void ChangeScene(int index)
    {
        SceneManager.LoadScene(index);
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
