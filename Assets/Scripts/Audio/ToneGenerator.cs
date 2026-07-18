using UnityEngine;

// Generates short synthesized placeholder SFX at runtime so gameplay events are
// audible even though the project has no imported SFX clips yet (only BGM tracks
// exist under Assets/ExternalAssets). Swap SFXManager's Inspector clip slots with
// real audio assets later; any slot left empty keeps using its generated tone.
public static class ToneGenerator
{
    public enum Wave { Sine, Square, Triangle, Sawtooth }

    private const int SampleRate = 44100;

    public static AudioClip Create(string name, float frequency, float duration, Wave wave, float amplitude = 0.5f)
    {
        int sampleCount = Mathf.Max(1, Mathf.RoundToInt(SampleRate * duration));
        var samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SampleRate;
            float progress = (float)i / sampleCount;
            samples[i] = Sample(wave, t, frequency) * amplitude * Envelope(progress);
        }

        var clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        clip.hideFlags = HideFlags.DontSave;
        return clip;
    }

    private static float Sample(Wave wave, float t, float freq)
    {
        float phase = t * freq;
        float frac = phase - Mathf.Floor(phase);
        switch (wave)
        {
            case Wave.Square: return frac < 0.5f ? 1f : -1f;
            case Wave.Triangle: return 4f * Mathf.Abs(frac - 0.5f) - 1f;
            case Wave.Sawtooth: return 2f * frac - 1f;
            default: return Mathf.Sin(2f * Mathf.PI * phase);
        }
    }

    // Quick fade-in/out so the tone doesn't click at the start/end.
    private static float Envelope(float progress)
    {
        const float attack = 0.06f;
        const float release = 0.25f;
        if (progress < attack) return progress / attack;
        if (progress > 1f - release) return Mathf.Clamp01((1f - progress) / release);
        return 1f;
    }
}
