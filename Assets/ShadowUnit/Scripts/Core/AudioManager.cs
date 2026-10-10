using UnityEngine;

namespace ShadowUnit
{
    /// <summary>Pooled one-shot audio with 3D positioning, plus a looping ambience track.</summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager I { get; private set; }

        AudioSource[] pool;
        int next;
        AudioSource ambience;

        public static AudioManager Create()
        {
            var go = new GameObject("AudioManager");
            I = go.AddComponent<AudioManager>();
            I.Init();
            return I;
        }

        void Init()
        {
            AudioLibrary.Build();
            pool = new AudioSource[28];
            for (int i = 0; i < pool.Length; i++)
            {
                var src = new GameObject("Voice" + i).AddComponent<AudioSource>();
                src.transform.SetParent(transform);
                src.playOnAwake = false;
                src.rolloffMode = AudioRolloffMode.Linear;
                src.minDistance = 4f;
                src.maxDistance = 120f;
                src.dopplerLevel = 0f;
                pool[i] = src;
            }
            ambience = gameObject.AddComponent<AudioSource>();
            ambience.clip = AudioLibrary.Wind;
            ambience.loop = true;
            ambience.volume = 0.12f;
            ambience.spatialBlend = 0f;
        }

        public void Play(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f, float spatial = 1f)
        {
            if (clip == null) return;
            AudioSource s = pool[next];
            next = (next + 1) % pool.Length;
            s.transform.position = position;
            s.spatialBlend = spatial;
            s.pitch = pitch;
            s.volume = volume;
            s.clip = clip;
            s.Play();
        }

        public void Play2D(AudioClip clip, float volume = 1f, float pitch = 1f) => Play(clip, Vector3.zero, volume, pitch, 0f);

        public void SetAmbience(bool on)
        {
            if (on && !ambience.isPlaying) ambience.Play();
            if (!on && ambience.isPlaying) ambience.Stop();
        }
    }
}
