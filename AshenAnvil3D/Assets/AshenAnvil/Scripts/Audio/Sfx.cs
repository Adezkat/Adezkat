using System.Collections.Generic;
using UnityEngine;

namespace AshenAnvil
{
    public enum SfxId { Swing, Hit, PlayerHurt, EnemyDeath, Explosion, Coin, Loot, LevelUp, Skill, Heal, Dodge, Break, Click, Victory, Defeat }

    /// <summary>
    /// Procedurally synthesised sound effects (no audio files needed), played from a small
    /// pool of AudioSources with per-sound rate limiting for mobile.
    /// </summary>
    public class Sfx : MonoBehaviour
    {
        static Sfx instance;
        readonly Dictionary<SfxId, AudioClip> clips = new Dictionary<SfxId, AudioClip>();
        readonly Dictionary<SfxId, float> lastPlayed = new Dictionary<SfxId, float>();
        AudioSource[] sources;
        int next;
        const int SampleRate = 22050;

        static Sfx Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("[Sfx]");
                    DontDestroyOnLoad(go);
                    instance = go.AddComponent<Sfx>();
                    instance.Init();
                }
                return instance;
            }
        }

        void Init()
        {
            sources = new AudioSource[10];
            for (int i = 0; i < sources.Length; i++)
            {
                sources[i] = gameObject.AddComponent<AudioSource>();
                sources[i].playOnAwake = false;
                sources[i].spatialBlend = 0f;
            }
        }

        public static void Play(SfxId id, Vector3 position = default, float volume = 1f)
        {
            var s = Instance;
            float vol = PlayerProfile.Instance.Data.settings.sfxVolume * volume;
            if (vol <= 0.001f) return;
            float now = Time.unscaledTime;
            if (s.lastPlayed.TryGetValue(id, out var last) && now - last < 0.05f) return;
            s.lastPlayed[id] = now;

            if (!s.clips.TryGetValue(id, out var clip))
            {
                clip = Synthesize(id);
                s.clips[id] = clip;
            }
            var src = s.sources[s.next];
            s.next = (s.next + 1) % s.sources.Length;
            src.pitch = Random.Range(0.94f, 1.06f);
            src.PlayOneShot(clip, vol);
        }

        static AudioClip Synthesize(SfxId id)
        {
            switch (id)
            {
                case SfxId.Swing: return Make(id, 0.16f, (t, d) => Noise() * Env(t, d, 0.02f) * 0.35f * (1f - t / d));
                case SfxId.Hit: return Make(id, 0.14f, (t, d) => (Noise() * 0.6f + Sin(110f - 300f * t, t)) * Env(t, d, 0.005f) * 0.6f);
                case SfxId.PlayerHurt: return Make(id, 0.22f, (t, d) => Square(180f - 200f * t, t) * Env(t, d, 0.01f) * 0.35f);
                case SfxId.EnemyDeath: return Make(id, 0.35f, (t, d) => (Sin(220f - 400f * t, t) * 0.5f + Noise() * 0.3f) * Env(t, d, 0.01f) * 0.5f);
                case SfxId.Explosion: return Make(id, 0.6f, (t, d) => (Noise() * 0.8f + Sin(60f, t) * 0.5f) * Mathf.Exp(-t * 6f) * 0.7f);
                case SfxId.Coin: return Make(id, 0.18f, (t, d) => Sin(t < 0.06f ? 1320f : 1760f, t) * Env(t, d, 0.003f) * 0.35f);
                case SfxId.Loot: return Make(id, 0.4f, (t, d) => Sin(660f + 880f * t, t) * Env(t, d, 0.01f) * 0.35f);
                case SfxId.LevelUp: return Make(id, 0.7f, (t, d) => Sin(t < 0.2f ? 523f : t < 0.4f ? 659f : 784f, t) * Env(t, d, 0.01f) * 0.4f);
                case SfxId.Skill: return Make(id, 0.4f, (t, d) => (Sin(300f + 600f * t, t) * 0.5f + Noise() * 0.25f) * Env(t, d, 0.02f) * 0.5f);
                case SfxId.Heal: return Make(id, 0.5f, (t, d) => (Sin(523f, t) + Sin(784f, t)) * 0.25f * Env(t, d, 0.05f));
                case SfxId.Dodge: return Make(id, 0.15f, (t, d) => Noise() * Env(t, d, 0.03f) * 0.25f);
                case SfxId.Break: return Make(id, 0.3f, (t, d) => (Noise() * 0.7f + Square(90f, t) * 0.2f) * Mathf.Exp(-t * 12f) * 0.6f);
                case SfxId.Click: return Make(id, 0.05f, (t, d) => Sin(1000f, t) * Env(t, d, 0.002f) * 0.3f);
                case SfxId.Victory: return Make(id, 1.0f, (t, d) => Sin(t < 0.25f ? 523f : t < 0.5f ? 659f : t < 0.75f ? 784f : 1046f, t) * Env(t, d, 0.01f) * 0.4f);
                case SfxId.Defeat: return Make(id, 1.0f, (t, d) => Sin(t < 0.33f ? 392f : t < 0.66f ? 330f : 262f, t) * Env(t, d, 0.01f) * 0.4f);
            }
            return Make(id, 0.1f, (t, d) => 0f);
        }

        static AudioClip Make(SfxId id, float duration, System.Func<float, float, float> fn)
        {
            int samples = Mathf.CeilToInt(duration * SampleRate);
            var data = new float[samples];
            for (int i = 0; i < samples; i++) data[i] = Mathf.Clamp(fn(i / (float)SampleRate, duration), -1f, 1f);
            var clip = AudioClip.Create("sfx_" + id, samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Sin(float freq, float t) => Mathf.Sin(2f * Mathf.PI * freq * t);
        static float Square(float freq, float t) => Mathf.Sign(Mathf.Sin(2f * Mathf.PI * freq * t));
        static float Noise() => Random.value * 2f - 1f;
        static float Env(float t, float d, float attack) => Mathf.Min(1f, t / attack) * Mathf.Clamp01(1f - t / d);
    }
}
