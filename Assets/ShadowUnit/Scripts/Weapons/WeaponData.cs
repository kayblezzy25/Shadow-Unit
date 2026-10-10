using UnityEngine;

namespace ShadowUnit
{
    public enum WeaponCategory { AssaultRifle, SMG, SniperRifle, Shotgun, LMG, Pistol }
    public enum FireMode { Automatic, SemiAutomatic }

    /// <summary>All tunable weapon stats. Create new weapons via Assets > Create > Shadow Unit > Weapon Data.</summary>
    [CreateAssetMenu(fileName = "NewWeapon", menuName = "Shadow Unit/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        [Header("Identity")]
        public string weaponName = "VK-7 Carbine";
        public WeaponCategory category = WeaponCategory.AssaultRifle;
        public FireMode fireMode = FireMode.Automatic;

        [Header("Damage")]
        public float damage = 24f;
        public float headshotMultiplier = 2f;
        [Tooltip("Full damage up to this distance (metres).")]
        public float effectiveRange = 45f;
        [Tooltip("Damage multiplier at max range.")]
        [Range(0.1f, 1f)] public float minDamageFalloff = 0.55f;
        public float maxRange = 220f;
        [Tooltip("Projectiles per shot (shotguns use more than 1).")]
        public int pellets = 1;

        [Header("Handling")]
        [Tooltip("Rounds per minute.")]
        public float fireRate = 700f;
        public int magazineSize = 30;
        public int reserveAmmo = 150;
        public float reloadTime = 2.1f;
        [Tooltip("Visual only: tracer speed, metres per second.")]
        public float bulletVelocity = 880f;

        [Header("Accuracy (degrees)")]
        public float hipSpread = 2.4f;
        public float adsSpread = 0.35f;
        public float spreadPerShot = 0.35f;
        public float maxExtraSpread = 3.5f;
        public float spreadRecovery = 6f;
        public float moveSpreadMultiplier = 1.6f;

        [Header("Recoil (degrees)")]
        public float recoilVertical = 0.85f;
        public float recoilHorizontal = 0.35f;
        public float recoilRecovery = 7f;
        [Range(0f, 1f)] public float adsRecoilMultiplier = 0.65f;

        [Header("Aim down sights")]
        public float hipFov = 70f;
        public float adsFov = 50f;
        public float adsTime = 0.18f;

        [Header("Audio")]
        public AudioClip fireSound;
        public AudioClip reloadSound;

        public float SecondsPerShot => 60f / Mathf.Max(1f, fireRate);

        /// <summary>Default carbine used if no asset is assigned.</summary>
        public static WeaponData CreateDefaultRifle()
        {
            var d = CreateInstance<WeaponData>();
            d.name = "VK-7 Carbine (default)";
            return d;
        }
    }
}
