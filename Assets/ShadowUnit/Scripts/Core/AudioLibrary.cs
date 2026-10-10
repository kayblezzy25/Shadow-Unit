using System;
using UnityEngine;

namespace ShadowUnit
{
    /// <summary>
    /// Original placeholder sound effects synthesised at startup, so the prototype needs no audio assets.
    /// Replace with recorded or licensed sounds in the production-polish phase.
    /// </summary>
    public static class AudioLibrary
    {
        const int Rate = 22050;
        static readonly System.Random Rng = new System.Random(7);
        static float N() => (float)Rng.NextDouble() * 2f - 1f;

        public static AudioClip RifleShot, DistantShot, Reload, DryFire, Impact, Footstep, HitMarker, KillMarker, Hurt, Capture, Victory, Defeat, Wind, UiClick;

        static bool built;

        public static void Build()
        {
            if (built) return;
            built = true;
            float lp = 0f, lp2 = 0f;

            RifleShot = Make("rifle_shot", 0.5f, t =>
            {
                lp += (N() - lp) * 0.45f;
                lp2 += (N() - lp2) * 0.06f;
                return lp * Mathf.Exp(-t * 26f) * 0.9f + Mathf.Sin(2f * Mathf.PI * 85f * t) * Mathf.Exp(-t * 16f) * 0.7f + lp2 * Mathf.Exp(-t * 5f) * 0.5f;
            });
            DistantShot = Make("distant_shot", 0.6f, t =>
            {
                lp += (N() - lp) * 0.09f;
                return lp * Mathf.Exp(-t * 9f) * 1.6f + Mathf.Sin(2f * Mathf.PI * 60f * t) * Mathf.Exp(-t * 10f) * 0.4f;
            });
            Reload = Make("reload", 1.1f, t =>
            {
                float s = 0f;
                foreach (float c in new[] { 0.08f, 0.55f, 0.9f })
                    if (t >= c) s += N() * Mathf.Exp(-(t - c) * 110f) * 0.8f + Mathf.Sin(2f * Mathf.PI * 1400f * (t - c)) * Mathf.Exp(-(t - c) * 70f) * 0.3f;
                return s;
            });
            DryFire = Make("dry", 0.08f, t => N() * Mathf.Exp(-t * 140f) * 0.6f);
            Impact = Make("impact", 0.14f, t => { lp += (N() - lp) * 0.6f; return lp * Mathf.Exp(-t * 55f) * 0.8f; });
            Footstep = Make("step", 0.16f, t => { lp += (N() - lp) * 0.12f; return lp * Mathf.Exp(-t * 30f) * 1.4f; });
            HitMarker = Make("hit", 0.07f, t => Mathf.Sin(2f * Mathf.PI * 1900f * t) * Mathf.Exp(-t * 45f) * 0.5f);
            KillMarker = Make("kill", 0.22f, t => (Mathf.Sin(2f * Mathf.PI * 1500f * t) + Mathf.Sin(2f * Mathf.PI * 2250f * t)) * Mathf.Exp(-t * 14f) * 0.3f);
            Hurt = Make("hurt", 0.3f, t => { lp += (N() - lp) * 0.08f; return (Mathf.Sin(2f * Mathf.PI * 70f * t) * 0.8f + lp) * Mathf.Exp(-t * 11f); });
            Capture = Make("capture", 0.45f, t => Mathf.Sin(2f * Mathf.PI * (t < 0.2f ? 660f : 990f) * t) * Mathf.Exp(-(t % 0.2f) * 12f) * 0.35f);
            Victory = Make("victory", 1.4f, t => (Mathf.Sin(2f * Mathf.PI * 392f * t) + Mathf.Sin(2f * Mathf.PI * 494f * t) + Mathf.Sin(2f * Mathf.PI * 587f * t)) * Mathf.Exp(-t * 2.2f) * 0.18f);
            Defeat = Make("defeat", 1.4f, t => (Mathf.Sin(2f * Mathf.PI * 196f * t) + Mathf.Sin(2f * Mathf.PI * 233f * t)) * Mathf.Exp(-t * 2f) * 0.22f);
            UiClick = Make("click", 0.05f, t => Mathf.Sin(2f * Mathf.PI * 1200f * t) * Mathf.Exp(-t * 90f) * 0.3f);
            Wind = Make("wind", 4f, t =>
            {
                lp += (N() - lp) * 0.02f;
                float env = 0.6f + 0.4f * Mathf.Sin(t * Mathf.PI * 0.5f);
                return lp * 3f * env;
            });
        }

        static AudioClip Make(string name, float seconds, Func<float, float> fn)
        {
            int n = Mathf.CeilToInt(Rate * seconds);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(fn(i / (float)Rate), -1f, 1f);
            AudioClip clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
