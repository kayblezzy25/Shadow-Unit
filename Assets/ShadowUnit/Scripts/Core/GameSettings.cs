using UnityEngine;

namespace ShadowUnit
{
    public enum QualityPreset { Low, Medium, High }
    public enum BotDifficulty { Easy, Normal, Hard }

    /// <summary>Player options stored in PlayerPrefs.</summary>
    public static class GameSettings
    {
        static float F(string k, float d) => PlayerPrefs.GetFloat(k, d);
        static bool B(string k, bool d) => PlayerPrefs.GetInt(k, d ? 1 : 0) == 1;
        static void SetB(string k, bool v) => PlayerPrefs.SetInt(k, v ? 1 : 0);

        public static float LookSensitivity { get => F("su_look", 1f); set => PlayerPrefs.SetFloat("su_look", Mathf.Clamp(value, 0.1f, 3f)); }
        public static float AimSensitivity { get => F("su_aim", 0.7f); set => PlayerPrefs.SetFloat("su_aim", Mathf.Clamp(value, 0.1f, 3f)); }
        public static bool InvertY { get => B("su_invert", false); set => SetB("su_invert", value); }
        public static bool GyroAim { get => B("su_gyro", false); set => SetB("su_gyro", value); }
        public static bool Vibration { get => B("su_vibrate", true); set => SetB("su_vibrate", value); }
        public static bool Fps60 { get => B("su_fps60", false); set => SetB("su_fps60", value); }
        public static float Volume { get => F("su_volume", 0.8f); set => PlayerPrefs.SetFloat("su_volume", Mathf.Clamp01(value)); }
        public static float HudOpacity { get => F("su_hudop", 0.75f); set => PlayerPrefs.SetFloat("su_hudop", Mathf.Clamp(value, 0.2f, 1f)); }
        public static float HudScale { get => F("su_hudscale", 1f); set => PlayerPrefs.SetFloat("su_hudscale", Mathf.Clamp(value, 0.7f, 1.4f)); }

        public static QualityPreset Quality
        {
            get => (QualityPreset)PlayerPrefs.GetInt("su_quality", Application.isMobilePlatform ? 1 : 2);
            set => PlayerPrefs.SetInt("su_quality", (int)value);
        }

        public static BotDifficulty Difficulty
        {
            get => (BotDifficulty)PlayerPrefs.GetInt("su_difficulty", 1);
            set => PlayerPrefs.SetInt("su_difficulty", (int)value);
        }

        public static void Save() => PlayerPrefs.Save();

        /// <summary>Applies frame rate, audio and graphics options.</summary>
        public static void Apply(Light sun, Camera cam)
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = Fps60 ? 60 : 30;
            AudioListener.volume = Volume;

            QualityPreset q = Quality;
            if (sun != null)
                sun.shadows = q == QualityPreset.Low ? LightShadows.None : q == QualityPreset.Medium ? LightShadows.Hard : LightShadows.Soft;
            QualitySettings.shadowDistance = q == QualityPreset.High ? 70f : 40f;
            EffectsManager.FlashLights = q != QualityPreset.Low;
            QualitySettings.antiAliasing = q == QualityPreset.High ? 2 : 0;
            QualitySettings.lodBias = q == QualityPreset.Low ? 0.6f : 1f;
            if (cam != null) cam.farClipPlane = q == QualityPreset.Low ? 110f : q == QualityPreset.Medium ? 170f : 260f;

#if ENABLE_LEGACY_INPUT_MANAGER
            if (SystemInfo.supportsGyroscope) Input.gyro.enabled = GyroAim;
#endif
        }

        public static void Vibrate()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (Vibration) Handheld.Vibrate();
#endif
        }
    }
}
