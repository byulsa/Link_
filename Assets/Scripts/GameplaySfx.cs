using System;
using System.Collections.Generic;
using UnityEngine;

public enum GameplaySound
{
    WaveStart = 0,
    MeleeAttack = 1,
    ProjectileAttack = 2,
    LaserAttack = 3,
    EnemyDeath = 4,
    SelfDestructWarning = 5,
    Explosion = 6,
    PlayerDeath = 7,
    CodePlacement = 8,
    EnemyHit = 9,
    BossLaser = 10,
    BossSlash = 11,
    BossPhaseBurst = 12,
    BossPhaseWarning = 13
}

public sealed class GameplaySfx : ScriptableObject
{
    [Serializable]
    private struct Cue
    {
        public GameplaySound sound;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume;
    }

    [SerializeField] private Cue[] cues;

    private static readonly Dictionary<AudioClip, AudioSource> sources = new();
    private static GameObject audioRoot;
    private static AudioSource deathSource;
    private static PlayerDeathAudioGlitch deathGlitch;
    private static GameplaySfx settings;

    public static void Play(GameplaySound sound)
    {
        if (settings == null)
            settings = Resources.Load<GameplaySfx>(nameof(GameplaySfx));

        if (settings == null || settings.cues == null)
            return;

        foreach (Cue cue in settings.cues)
        {
            if (cue.sound != sound || cue.clip == null)
                continue;

            if (audioRoot == null)
            {
                audioRoot = new GameObject("Gameplay SFX");
                sources.Clear();
            }

            if (sound == GameplaySound.PlayerDeath)
            {
                PlayDeathVoice(cue);
                return;
            }

            if (!sources.TryGetValue(cue.clip, out AudioSource source) || source == null)
            {
                source = audioRoot.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.clip = cue.clip;
                sources[cue.clip] = source;
            }

            source.Stop();
            source.volume = cue.volume * GameAudioSettings.Effects;
            source.Play();
            return;
        }
    }

    private static void PlayDeathVoice(Cue cue)
    {
        if (deathSource == null)
        {
            var voice = new GameObject("Death Voice");
            voice.transform.SetParent(audioRoot.transform, false);
            deathSource = voice.AddComponent<AudioSource>();
            deathSource.playOnAwake = false;
            deathSource.spatialBlend = 0f;
            deathGlitch = voice.AddComponent<PlayerDeathAudioGlitch>();
        }

        deathSource.Stop();
        deathSource.clip = cue.clip;
        deathSource.volume = cue.volume * GameAudioSettings.Effects;
        deathGlitch.Restart();
        deathSource.Play();
    }
}
