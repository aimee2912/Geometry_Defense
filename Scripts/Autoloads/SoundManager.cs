using System;
using Godot;

namespace VectorBastion;

/// <summary>
/// Procedural minimalist synth audio manager using in-memory AudioStreamWav buffers.
/// No external audio assets required.
/// </summary>
public partial class SoundManager : Node
{
    public static SoundManager Instance { get; private set; }

    private const int SampleRate = 22050;
    private const int PoolSize = 12;

    private AudioStreamPlayer[] _pool;
    private int _poolIndex;

    private AudioStreamWav _sfxShootBolt;
    private AudioStreamWav _sfxShootCannon;
    private AudioStreamWav _sfxExplosion;
    private AudioStreamWav _sfxPulse;
    private AudioStreamWav _sfxEnemyPop;
    private AudioStreamWav _sfxPlace;
    private AudioStreamWav _sfxUpgrade;
    private AudioStreamWav _sfxSell;
    private AudioStreamWav _sfxCoreHit;
    private AudioStreamWav _sfxWaveClear;
    private AudioStreamWav _sfxVictory;
    private AudioStreamWav _sfxDefeat;

    public override void _EnterTree()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always;
    }

    public override void _Ready()
    {
        _pool = new AudioStreamPlayer[PoolSize];
        for (int i = 0; i < PoolSize; i++)
        {
            var player = new AudioStreamPlayer { VolumeDb = -10.0f };
            AddChild(player);
            _pool[i] = player;
        }

        _sfxShootBolt = CreateSweep(920f, 380f, 0.07f, 0.30f, WaveType.Triangle);
        _sfxShootCannon = CreateSweep(240f, 80f, 0.14f, 0.42f, WaveType.Square);
        _sfxExplosion = CreateNoiseBurst(0.18f, 0.35f, 110f);
        _sfxPulse = CreateSweep(520f, 190f, 0.20f, 0.32f, WaveType.Sine);
        _sfxEnemyPop = CreateSweep(300f, 120f, 0.09f, 0.28f, WaveType.Sine);
        _sfxPlace = CreateChord(new[] { 440f, 660f }, 0.12f, 0.30f);
        _sfxUpgrade = CreateArpeggio(new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.06f, 0.30f);
        _sfxSell = CreateArpeggio(new[] { 659.25f, 440f }, 0.07f, 0.28f);
        _sfxCoreHit = CreateSweep(160f, 55f, 0.28f, 0.50f, WaveType.Sawtooth);
        _sfxWaveClear = CreateArpeggio(new[] { 440f, 554.37f, 659.25f }, 0.09f, 0.30f);
        _sfxVictory = CreateArpeggio(new[] { 523.25f, 659.25f, 783.99f, 1046.5f, 1318.5f }, 0.10f, 0.32f);
        _sfxDefeat = CreateArpeggio(new[] { 349.23f, 311.13f, 277.18f, 220f }, 0.14f, 0.35f);
    }

    public void PlayShootBolt() => Play(_sfxShootBolt, -14f, 0.94f, 1.08f);
    public void PlayShootCannon() => Play(_sfxShootCannon, -10f, 0.92f, 1.05f);
    public void PlayExplosion() => Play(_sfxExplosion, -9f, 0.90f, 1.10f);
    public void PlayPulse() => Play(_sfxPulse, -12f, 0.95f, 1.05f);
    public void PlayEnemyPop() => Play(_sfxEnemyPop, -15f, 0.85f, 1.25f);
    public void PlayPlace() => Play(_sfxPlace, -9f, 0.98f, 1.02f);
    public void PlayUpgrade() => Play(_sfxUpgrade, -8f, 1.0f, 1.0f);
    public void PlaySell() => Play(_sfxSell, -10f, 1.0f, 1.0f);
    public void PlayCoreHit() => Play(_sfxCoreHit, -5f, 0.95f, 1.05f);
    public void PlayWaveClear() => Play(_sfxWaveClear, -8f, 1.0f, 1.0f);
    public void PlayVictory() => Play(_sfxVictory, -6f, 1.0f, 1.0f);
    public void PlayDefeat() => Play(_sfxDefeat, -6f, 1.0f, 1.0f);

    private void Play(AudioStreamWav stream, float volumeDb, float minPitch, float maxPitch)
    {
        if (stream == null || _pool == null)
            return;

        var player = _pool[_poolIndex];
        _poolIndex = (_poolIndex + 1) % PoolSize;

        player.Stream = stream;
        player.VolumeDb = volumeDb;
        player.PitchScale = (float)GD.RandRange(minPitch, maxPitch);
        player.Play();
    }

    private enum WaveType { Sine, Square, Triangle, Sawtooth }

    private static float SampleWave(WaveType type, double phase)
    {
        double p = phase - Math.Floor(phase);
        return type switch
        {
            WaveType.Sine => (float)Math.Sin(p * Math.PI * 2.0),
            WaveType.Square => p < 0.5 ? 1f : -1f,
            WaveType.Triangle => (float)(4.0 * Math.Abs(p - 0.5) - 1.0),
            WaveType.Sawtooth => (float)(2.0 * p - 1.0),
            _ => 0f
        };
    }

    private static AudioStreamWav CreateSweep(float startFreq, float endFreq, float duration, float amplitude, WaveType type)
    {
        int count = (int)(SampleRate * duration);
        byte[] data = new byte[count * 2];
        double phase = 0.0;

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / count;
            float freq = Mathf.Lerp(startFreq, endFreq, t);
            float env = (1f - t) * (1f - t);
            phase += freq / SampleRate;
            float s = SampleWave(type, phase) * amplitude * env;
            WriteSample(data, i, s);
        }

        return BuildWav(data);
    }

    private static AudioStreamWav CreateNoiseBurst(float duration, float amplitude, float bassFreq)
    {
        int count = (int)(SampleRate * duration);
        byte[] data = new byte[count * 2];
        var rng = new Random(42);
        double phase = 0.0;

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / count;
            float env = (1f - t) * (1f - t);
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
            phase += Mathf.Lerp(bassFreq, 35f, t) / SampleRate;
            float bass = (float)Math.Sin(phase * Math.PI * 2.0);
            float s = (noise * 0.45f + bass * 0.55f) * amplitude * env;
            WriteSample(data, i, s);
        }

        return BuildWav(data);
    }

    private static AudioStreamWav CreateChord(float[] freqs, float duration, float amplitude)
    {
        int count = (int)(SampleRate * duration);
        byte[] data = new byte[count * 2];

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / count;
            float env = Mathf.Sin(Mathf.Min(t * 15f, 1f) * Mathf.Pi * 0.5f) * (1f - t);
            float sum = 0f;
            foreach (float f in freqs)
            {
                sum += (float)Math.Sin(2.0 * Math.PI * f * i / SampleRate);
            }
            WriteSample(data, i, (sum / freqs.Length) * amplitude * env);
        }

        return BuildWav(data);
    }

    private static AudioStreamWav CreateArpeggio(float[] notes, float noteDuration, float amplitude)
    {
        int noteSamples = (int)(SampleRate * noteDuration);
        int total = noteSamples * notes.Length;
        byte[] data = new byte[total * 2];

        for (int n = 0; n < notes.Length; n++)
        {
            float freq = notes[n];
            for (int i = 0; i < noteSamples; i++)
            {
                float t = (float)i / noteSamples;
                float env = (1f - t * 0.7f);
                float s = (float)Math.Sin(2.0 * Math.PI * freq * i / SampleRate) * amplitude * env;
                WriteSample(data, n * noteSamples + i, s);
            }
        }

        return BuildWav(data);
    }

    private static void WriteSample(byte[] buffer, int index, float value)
    {
        short pcm = (short)(Mathf.Clamp(value, -1f, 1f) * short.MaxValue);
        buffer[index * 2] = (byte)(pcm & 0xFF);
        buffer[index * 2 + 1] = (byte)((pcm >> 8) & 0xFF);
    }

    private static AudioStreamWav BuildWav(byte[] data)
    {
        return new AudioStreamWav
        {
            Data = data,
            Format = AudioStreamWav.FormatEnum.Format16Bits,
            MixRate = SampleRate,
            Stereo = false
        };
    }
}
