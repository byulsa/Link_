using System;
using UnityEngine;

public static class GameAudioSettings
{
    private const string MasterKey = "LINK.Audio.Master";
    private const string MusicKey = "LINK.Audio.Music";
    private const string EffectsKey = "LINK.Audio.Effects";

    public static float Master => Read(MasterKey);
    public static float Music => Read(MusicKey);
    public static float Effects => Read(EffectsKey);
    public static event Action MusicChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplyMaster() => AudioListener.volume = Master;

    public static void SetMaster(float value)
    {
        PlayerPrefs.SetFloat(MasterKey, Mathf.Clamp01(value));
        ApplyMaster();
    }

    public static void SetMusic(float value)
    {
        PlayerPrefs.SetFloat(MusicKey, Mathf.Clamp01(value));
        MusicChanged?.Invoke();
    }

    public static void SetEffects(float value) => PlayerPrefs.SetFloat(EffectsKey, Mathf.Clamp01(value));

    public static void Save() => PlayerPrefs.Save();

    private static float Read(string key) => Mathf.Clamp01(PlayerPrefs.GetFloat(key, 1f));
}
