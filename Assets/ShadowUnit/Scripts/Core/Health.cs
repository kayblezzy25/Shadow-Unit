using System;
using UnityEngine;

namespace ShadowUnit
{
    /// <summary>Damage event passed to anything that can be hurt.</summary>
    public struct DamageInfo
    {
        public float amount;
        public Vector3 point;
        public Vector3 direction;
        public Soldier attacker;
        public bool headshot;
    }

    /// <summary>Shared health component for the player and AI soldiers.</summary>
    public class Health : MonoBehaviour
    {
        public float max = 100f;
        [Tooltip("Seconds without damage before regeneration starts. 0 regen rate disables it.")]
        public float regenDelay = 5f;
        public float regenRate = 0f;

        public float Current { get; private set; }
        public bool IsDead => Current <= 0f;
        public float Fraction => max > 0f ? Current / max : 0f;

        public event Action<DamageInfo> Damaged;
        public event Action<DamageInfo> Died;

        float lastDamageTime = -99f;
        float invulnerableUntil;

        void Awake() { Current = max; }

        public void ResetHealth(float spawnProtection = 0f)
        {
            Current = max;
            lastDamageTime = -99f;
            invulnerableUntil = Time.time + spawnProtection;
        }

        public void TakeDamage(DamageInfo info)
        {
            if (IsDead || Time.time < invulnerableUntil) return;
            Current = Mathf.Max(0f, Current - info.amount);
            lastDamageTime = Time.time;
            Damaged?.Invoke(info);
            if (Current <= 0f) Died?.Invoke(info);
        }

        void Update()
        {
            if (IsDead || regenRate <= 0f || Current >= max) return;
            if (Time.time - lastDamageTime > regenDelay)
                Current = Mathf.Min(max, Current + regenRate * Time.deltaTime);
        }
    }
}
