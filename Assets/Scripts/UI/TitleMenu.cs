using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class TitleMenu : MonoBehaviour
{
    [Serializable]
    private struct VolumeControl
    {
        public Slider slider;
        public Text percentage;
    }

    [SerializeField] private string gameplayScene = "SampleScene";
    [SerializeField] private AudioClip menuMusic;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.1f;

    [Header("Panels")]
    [SerializeField] private GameObject home;
    [SerializeField] private GameObject settings;
    [SerializeField] private Button startButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button previewButton;

    [Header("Volume Controls")]
    [SerializeField] private VolumeControl master;
    [SerializeField] private VolumeControl musicControl;
    [SerializeField] private VolumeControl effects;

    private AudioSource music;
    private bool loading;

    private void Start()
    {
        startButton.onClick.AddListener(StartGame);
        settingsButton.onClick.AddListener(OpenSettings);
        backButton.onClick.AddListener(OpenHome);
        previewButton.onClick.AddListener(() => GameplaySfx.Play(GameplaySound.CodePlacement));
        BindVolume(master, GameAudioSettings.Master, GameAudioSettings.SetMaster);
        BindVolume(musicControl, GameAudioSettings.Music, GameAudioSettings.SetMusic);
        BindVolume(effects, GameAudioSettings.Effects, GameAudioSettings.SetEffects);
        OpenHome();

        if (menuMusic != null)
        {
            music = gameObject.AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.loop = true;
            music.clip = menuMusic;
            ApplyMusicVolume();
            music.Play();
        }
    }

    private static void BindVolume(VolumeControl control, float initialValue, Action<float> setVolume)
    {
        Slider slider = control.slider;
        slider.SetValueWithoutNotify(Mathf.Lerp(slider.minValue, slider.maxValue, initialValue));
        control.percentage.text = $"{Mathf.RoundToInt(slider.normalizedValue * 100f)}%";
        slider.onValueChanged.AddListener(_ =>
        {
            setVolume(slider.normalizedValue);
            control.percentage.text = $"{Mathf.RoundToInt(slider.normalizedValue * 100f)}%";
        });
    }

    private void OnEnable() => GameAudioSettings.MusicChanged += ApplyMusicVolume;

    private void OnDisable()
    {
        GameAudioSettings.MusicChanged -= ApplyMusicVolume;
        GameAudioSettings.Save();
    }

    private void Update()
    {
        if (!loading && settings.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            OpenHome();
    }

    private void ApplyMusicVolume()
    {
        if (music != null)
            music.volume = musicVolume * GameAudioSettings.Music;
    }

    private void OpenHome()
    {
        GameAudioSettings.Save();
        settings.SetActive(false);
        home.SetActive(true);
        startButton.Select();
    }

    private void OpenSettings()
    {
        if (loading)
            return;

        home.SetActive(false);
        settings.SetActive(true);
        master.slider.Select();
    }

    private void StartGame()
    {
        if (loading || !Application.CanStreamedLevelBeLoaded(gameplayScene))
            return;

        loading = true;
        startButton.interactable = false;
        settingsButton.interactable = false;
        GameAudioSettings.Save();
        SceneManager.LoadSceneAsync(gameplayScene);
    }
}
