using System.Collections.Generic;
using UnityEngine;

namespace ShadowUnit
{
    /// <summary>Identity shared by the player and every bot: team, name, stats and aim points.</summary>
    [RequireComponent(typeof(Health))]
    public class Soldier : MonoBehaviour
    {
        public static readonly List<Soldier> All = new List<Soldier>();

        public Team team;
        public string displayName = "Soldier";
        public bool isPlayer;
        public int kills;
        public int deaths;

        [HideInInspector] public Health health;
        [HideInInspector] public CharacterController body;
        /// <summary>Time until which this soldier is shown to the opposing team's minimap.</summary>
        [HideInInspector] public float spottedUntil;

        void Awake()
        {
            health = GetComponent<Health>();
            body = GetComponent<CharacterController>();
            if (!All.Contains(this)) All.Add(this);
        }

        void OnDestroy() { All.Remove(this); }

        public bool Alive => health != null && !health.IsDead && gameObject.activeInHierarchy;

        public float Height => body != null ? body.height : 1.8f;

        /// <summary>Eye position used for line-of-sight checks.</summary>
        public Vector3 Eye => transform.position + Vector3.up * (Height - 0.15f);

        /// <summary>Centre of mass, where bots aim by default.</summary>
        public Vector3 AimPoint => transform.position + Vector3.up * (Height * 0.62f);

        /// <summary>Hits above this height count as headshots.</summary>
        public float HeadshotHeight => transform.position.y + Height - 0.32f;
    }
}
