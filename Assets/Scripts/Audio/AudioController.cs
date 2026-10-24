using UnityEngine;

namespace CurveShot.Audio
{
    /// <summary>
    /// Manages sound effects using procedurally synthesized audio waveforms.
    /// Operates completely self-contained without requiring external audio asset files.
    /// Provides mute toggle and persistent audio settings.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioController : MonoBehaviour
    {
        [Header("Audio Settings")]
        [SerializeField] private bool isMuted = false;
        [SerializeField] private float masterVolume = 0.75f;

        private AudioSource _audioSource;
        private AudioClip _launchClip;
        private AudioClip _impactClip;
        private AudioClip _successClip;
        private AudioClip _failureClip;

        public bool IsMuted => isMuted;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _audioSource.playOnAwake = false;

            // Load persistent mute state
            isMuted = PlayerPrefs.GetInt("CurveShot_Muted", 0) == 1;

            GenerateProceduralClips();
        }

        private void GenerateProceduralClips()
        {
            _launchClip = GenerateTone(44100, 0.15f, 220f, 660f, WaveformType.Sine, true);
            _impactClip = GenerateTone(44100, 0.08f, 520f, 280f, WaveformType.Triangle, false);
            _successClip = GenerateFanfare(44100, 0.45f);
            _failureClip = GenerateTone(44100, 0.25f, 180f, 60f, WaveformType.Sawtooth, false);
        }

        public void PlayLaunchSound()
        {
            PlayClip(_launchClip, 0.6f);
        }

        public void PlayRampImpactSound()
        {
            PlayClip(_impactClip, 0.7f);
        }

        public void PlaySuccessSound()
        {
            PlayClip(_successClip, 0.85f);
        }

        public void PlayFailureSound()
        {
            PlayClip(_failureClip, 0.75f);
        }

        private void PlayClip(AudioClip clip, float volumeScale)
        {
            if (isMuted || _audioSource == null || clip == null) return;
            _audioSource.PlayOneShot(clip, masterVolume * volumeScale);
        }

        public void ToggleMute()
        {
            isMuted = !isMuted;
            PlayerPrefs.SetInt("CurveShot_Muted", isMuted ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void SetMuted(bool mute)
        {
            isMuted = mute;
            PlayerPrefs.SetInt("CurveShot_Muted", isMuted ? 1 : 0);
            PlayerPrefs.Save();
        }

        private enum WaveformType { Sine, Triangle, Sawtooth }

        private AudioClip GenerateTone(int sampleRate, float duration, float startFreq, float endFreq, WaveformType type, bool isWhoosh)
        {
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float currentFreq = Mathf.Lerp(startFreq, endFreq, t);
                float phase = 2f * Mathf.PI * currentFreq * (i / (float)sampleRate);

                float rawSample = 0f;
                switch (type)
                {
                    case WaveformType.Sine:
                        rawSample = Mathf.Sin(phase);
                        break;
                    case WaveformType.Triangle:
                        rawSample = Mathf.PingPong(phase / Mathf.PI, 1f) * 2f - 1f;
                        break;
                    case WaveformType.Sawtooth:
                        rawSample = (phase / Mathf.PI % 2f) - 1f;
                        break;
                }

                // Smooth attack and decay envelope
                float envelope = Mathf.Sin(t * Mathf.PI);
                samples[i] = rawSample * envelope * 0.5f;
            }

            AudioClip clip = AudioClip.Create("SynthTone", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateFanfare(int sampleRate, float duration)
        {
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            // Harmonic triad (C5, E5, G5)
            float freq1 = 523.25f;
            float freq2 = 659.25f;
            float freq3 = 783.99f;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float timeSec = i / (float)sampleRate;

                float sample = Mathf.Sin(2f * Mathf.PI * freq1 * timeSec) * 0.35f
                             + Mathf.Sin(2f * Mathf.PI * freq2 * timeSec) * 0.25f
                             + Mathf.Sin(2f * Mathf.PI * freq3 * timeSec) * 0.25f;

                float envelope = Mathf.Pow(1f - t, 0.8f);
                samples[i] = sample * envelope * 0.6f;
            }

            AudioClip clip = AudioClip.Create("SynthFanfare", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
