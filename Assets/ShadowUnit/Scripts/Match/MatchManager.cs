using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ShadowUnit
{
    public enum MatchState { Menu, Playing, Paused, Ended }

    /// <summary>
    /// Warfare match rules: two teams, three capture points, score from held points and kills,
    /// timed respawns, time limit and victory/defeat. Runs locally against AI for Phase 1.
    /// </summary>
    public class MatchManager : MonoBehaviour
    {
        public static MatchManager I { get; private set; }

        [Header("Rules")]
        public float timeLimit = 420f;
        public int scoreToWin = 300;
        public float pointsPerHeldPointPerSecond = 1f;
        public int pointsPerKill = 3;
        public float respawnDelay = 5f;
        public int blueBots = 3;
        public int redBots = 4;

        public MatchState State { get; private set; } = MatchState.Menu;
        public float TimeLeft { get; private set; }
        public float[] Score { get; } = new float[2];
        public Team Winner { get; private set; }
        public float PlayerRespawnAt { get; private set; } = -1f;

        public MapInfo Map { get; private set; }
        public PlayerController Player { get; private set; }

        public event Action<Soldier, Soldier, bool> Kill;   // killer (may be null), victim, headshot
        public event Action<string> Announcement;
        public event Action<Team> MatchEnded;

        readonly List<SoldierAI> bots = new List<SoldierAI>();
        SpawnManager spawns;
        Camera cam;
        WeaponData weapon;

        static readonly string[] BlueNames = { "Nightjar", "Anvil", "Ember", "Hollow", "Vega" };
        static readonly string[] RedNames = { "Kestrel", "Brand", "Volk", "Sable", "Dorn", "Ash" };

        public void Init(MapInfo map, Camera camera, WeaponData weaponData)
        {
            I = this;
            Map = map;
            cam = camera;
            weapon = weaponData;
            spawns = new SpawnManager(map);
            CapturePoint.Captured += (p, t) =>
            {
                Announcement?.Invoke($"{t.Label()} captured {p.label}");
                if (AudioManager.I != null) AudioManager.I.Play2D(AudioLibrary.Capture, 0.6f, t == Team.Blue ? 1.15f : 0.85f);
            };
            CapturePoint.Neutralized += p => Announcement?.Invoke($"{p.label} neutralised");
        }

        public void StartMatch()
        {
            StopAllCoroutines();
            foreach (SoldierAI b in bots) if (b != null) Destroy(b.gameObject);
            bots.Clear();
            Score[0] = Score[1] = 0f;
            TimeLeft = timeLimit;
            foreach (CapturePoint p in Map.points) p.ResetPoint();

            if (Player == null)
            {
                Player = PlayerController.Spawn(cam, weapon);
                Player.Soldier.health.Died += info => ReportDeath(Player.Soldier, info);
            }
            Player.gameObject.SetActive(true);
            Player.AttachCamera();
            Player.Soldier.kills = Player.Soldier.deaths = 0;
            for (int i = 0; i < blueBots; i++) bots.Add(SoldierAI.Spawn(Team.Blue, BlueNames[i % BlueNames.Length], Map));
            for (int i = 0; i < redBots; i++) bots.Add(SoldierAI.Spawn(Team.Red, RedNames[i % RedNames.Length], Map));

            State = MatchState.Playing;
            Time.timeScale = 1f;
            float yaw;
            Player.Teleport(spawns.GetSpawn(Team.Blue, out yaw), yaw);
            Player.Soldier.health.ResetHealth(2f);
            Player.Weapon.ResetWeapon();
            Player.Weapon.SetVisible(true);
            PlayerRespawnAt = -1f;
            foreach (SoldierAI b in bots)
            {
                Vector3 p = spawns.GetSpawn(b.Soldier.team, out yaw);
                b.Respawn(p + new Vector3(UnityEngine.Random.Range(-1.5f, 1.5f), 0, UnityEngine.Random.Range(-1.5f, 1.5f)), yaw);
            }
            if (AudioManager.I != null) AudioManager.I.SetAmbience(true);
            Announcement?.Invoke("Capture and hold A, B and C");
        }

        void Update()
        {
            if (State != MatchState.Playing) return;
            TimeLeft -= Time.deltaTime;
            foreach (CapturePoint p in Map.points)
                if (p.owner >= 0) Score[p.owner] += pointsPerHeldPointPerSecond * Time.deltaTime;

            if (Score[0] >= scoreToWin) End(Team.Blue);
            else if (Score[1] >= scoreToWin) End(Team.Red);
            else if (TimeLeft <= 0f) End(Score[0] >= Score[1] ? Team.Blue : Team.Red);
        }

        public void ReportDeath(Soldier victim, DamageInfo info)
        {
            if (State != MatchState.Playing) return;
            victim.deaths++;
            Soldier killer = info.attacker;
            if (killer != null && killer.team != victim.team)
            {
                killer.kills++;
                Score[(int)killer.team] += pointsPerKill;
            }
            Kill?.Invoke(killer, victim, info.headshot);
            StartCoroutine(RespawnAfterDelay(victim));
            if (victim.isPlayer)
            {
                PlayerRespawnAt = Time.time + respawnDelay;
                Player.Weapon.SetVisible(false);
                if (AudioManager.I != null) AudioManager.I.Play2D(AudioLibrary.Hurt, 0.9f, 0.7f);
            }
        }

        IEnumerator RespawnAfterDelay(Soldier s)
        {
            yield return new WaitForSeconds(respawnDelay);
            while (State == MatchState.Paused) yield return null;
            if (State != MatchState.Playing || s == null) yield break;
            float yaw;
            Vector3 pos = spawns.GetSpawn(s.team, out yaw);
            if (s.isPlayer)
            {
                Player.Teleport(pos, yaw);
                s.health.ResetHealth(2.5f);
                Player.Weapon.ResetWeapon();
                Player.Weapon.SetVisible(true);
                PlayerRespawnAt = -1f;
                if (MobileInputController.I != null) MobileInputController.I.ResetState();
            }
            else
            {
                SoldierAI ai = s.GetComponent<SoldierAI>();
                if (ai != null) ai.Respawn(pos, yaw);
            }
        }

        void End(Team winner)
        {
            State = MatchState.Ended;
            Winner = winner;
            if (AudioManager.I != null)
            {
                AudioManager.I.Play2D(winner == Team.Blue ? AudioLibrary.Victory : AudioLibrary.Defeat, 0.8f);
                AudioManager.I.SetAmbience(false);
            }
            MatchEnded?.Invoke(winner);
        }

        public void Pause(bool pause)
        {
            if (pause && State == MatchState.Playing) { State = MatchState.Paused; Time.timeScale = 0f; }
            else if (!pause && State == MatchState.Paused) { State = MatchState.Playing; Time.timeScale = 1f; }
        }

        public void QuitToMenu()
        {
            StopAllCoroutines();
            Time.timeScale = 1f;
            State = MatchState.Menu;
            foreach (SoldierAI b in bots) if (b != null) Destroy(b.gameObject);
            bots.Clear();
            if (Player != null)
            {
                Player.Weapon.SetVisible(false);
                cam.transform.SetParent(null);
                Player.gameObject.SetActive(false);
            }
            if (AudioManager.I != null) AudioManager.I.SetAmbience(false);
        }

        public IEnumerable<Soldier> Roster()
        {
            if (Player != null) yield return Player.Soldier;
            foreach (SoldierAI b in bots) if (b != null) yield return b.Soldier;
        }
    }
}
