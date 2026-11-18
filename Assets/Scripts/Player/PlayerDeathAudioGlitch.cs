using System;
using System.Threading;
using UnityEngine;

public sealed class PlayerDeathAudioGlitch : MonoBehaviour
{
    private readonly float[] heldSamples = new float[8];
    private int sampleRate;
    private int restart;
    private int frame;

    private void Awake()
    {
        sampleRate = AudioSettings.outputSampleRate;
    }

    public void Restart() => Interlocked.Exchange(ref restart, 1);

    private void OnAudioFilterRead(float[] data, int channels)
    {
        if (sampleRate <= 0 || channels <= 0 || channels > heldSamples.Length)
            return;

        if (Interlocked.Exchange(ref restart, 0) != 0)
            frame = 0;

        int hold = Math.Max(1, sampleRate / 5600);
        int stutterFrames = Math.Max(1, sampleRate / 13);
        for (int i = 0; i < data.Length; i += channels, frame++)
        {
            float seconds = (float)frame / sampleRate;
            bool stutter = (seconds >= 0.08f && seconds < 0.28f)
                || (seconds >= 0.46f && seconds < 0.71f);
            int position = frame % stutterFrames;
            float gate = stutter && position > stutterFrames * 0.67f ? 0.12f : 1f;
            float wet = stutter ? 0.8f : 0.55f;
            float tail = Math.Clamp((0.88f - seconds) / 0.09f, 0f, 1f);
            for (int channel = 0; channel < channels; channel++)
            {
                float dry = data[i + channel];
                if (frame % hold == 0)
                    heldSamples[channel] = (float)Math.Round(dry * 28f) / 28f;
                data[i + channel] = (dry * (1f - wet) + heldSamples[channel] * wet) * gate * tail;
            }
        }
    }
}
