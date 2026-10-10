using UnityEngine;

namespace ShadowUnit
{
    /// <summary>
    /// Entry point. Put this on one GameObject in an empty scene (the editor menu does it for you).
    /// Builds the map, managers and UI, then shows the main menu.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [Tooltip("Optional. If empty, the default VK-7 carbine stats are used.")]
        public WeaponData primaryWeapon;

        public MapInfo Map { get; private set; }
        public Camera MainCamera { get; private set; }

        void Awake()
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.orientation = ScreenOrientation.LandscapeLeft;

            if (primaryWeapon == null) primaryWeapon = Resources.Load<WeaponData>("ShadowUnit/Weapons/VK7_Carbine");

            AudioManager.Create();
            Map = MapBuilder.Build();

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            MainCamera = camGo.AddComponent<Camera>();
            MainCamera.fieldOfView = 70f;
            MainCamera.nearClipPlane = 0.03f;
            camGo.AddComponent<AudioListener>();

            EffectsManager.Create();

            var match = new GameObject("MatchManager").AddComponent<MatchManager>();
            match.Init(Map, MainCamera, primaryWeapon);

            GameSettings.Apply(Map.sun, MainCamera);
            UIManager.Create(this);
        }

        float orbit;

        void LateUpdate()
        {
            // slow orbit over the battlefield while in menus
            if (MatchManager.I == null || MatchManager.I.State != MatchState.Menu) return;
            orbit += Time.unscaledDeltaTime * 4f;
            Transform t = MainCamera.transform;
            if (t.parent != null) t.SetParent(null);
            float a = orbit * Mathf.Deg2Rad;
            t.position = new Vector3(Mathf.Sin(a) * 62f, 34f, Mathf.Cos(a) * 62f);
            t.LookAt(new Vector3(0, 2f, 0));
            MainCamera.fieldOfView = 55f;
        }
    }
}
