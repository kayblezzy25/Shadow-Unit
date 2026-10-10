using System.Collections.Generic;
using UnityEngine;

namespace ShadowUnit
{
    /// <summary>
    /// State-machine soldier used for both teams: works objectives, spots enemies by vision cone and line of sight,
    /// reacts to gunfire and damage, fights in bursts, reloads, takes cover, searches, then returns to objectives.
    /// It never sees through walls: every detection needs a clear line of sight.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class SoldierAI : MonoBehaviour
    {
        enum State { Objective, Engage, Cover, Search }

        public static readonly List<SoldierAI> All = new List<SoldierAI>();

        // tuning (set from difficulty)
        float reactionTime = 0.55f, aimError = 1.7f, damage = 12f, visionRange = 55f;
        const float FovHalf = 62f, RunSpeed = 3.9f, CombatSpeed = 1.9f, MagSize = 30, FireInterval = 0.1f;

        Soldier me;
        CharacterController cc;
        SoldierBody body;
        WaypointGraph graph;
        MapInfo map;

        State state = State.Objective;
        Soldier target;
        bool targetVisible;
        float reactionReady, lastSeenTime;
        Vector3 lastKnownPos;

        readonly List<Vector3> path = new List<Vector3>();
        readonly List<WaypointGraph.Node> nearby = new List<WaypointGraph.Node>();
        int pathIndex;
        Vector3 pathGoal = new Vector3(9999, 0, 0);
        float repathAt, perceiveAt, stuckTimer;

        int ammo = 30, burstLeft;
        float nextShot, reloadDone = -1f;
        float coverUntil, searchUntil, strafeChangeAt, strafeDir = 1f;
        CapturePoint objective;
        float objectiveRethinkAt, wanderAt;
        Vector3 wanderTarget;
        float verticalVelocity;
        Vector3 lastPos;
        float desiredYaw;
        bool crouched;

        public Soldier Soldier => me;

        public static SoldierAI Spawn(Team team, string displayName, MapInfo map)
        {
            var go = new GameObject("Bot_" + displayName);
            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0, 0.9f, 0);
            cc.stepOffset = 0.4f;
            go.AddComponent<Health>().max = 100f;
            Soldier s = go.AddComponent<Soldier>();
            s.team = team;
            s.displayName = displayName;
            SoldierBody body = go.AddComponent<SoldierBody>();
            body.Build(team);
            SoldierAI ai = go.AddComponent<SoldierAI>();
            ai.map = map;
            ai.graph = map.graph;
            return ai;
        }

        void Awake()
        {
            me = GetComponent<Soldier>();
            cc = GetComponent<CharacterController>();
            body = GetComponent<SoldierBody>();
            All.Add(this);
        }

        void Start()
        {
            me.health.Damaged += OnDamaged;
            me.health.Died += OnDied;
            ApplyDifficulty();
        }

        void OnDestroy() { All.Remove(this); }

        void ApplyDifficulty()
        {
            switch (GameSettings.Difficulty)
            {
                case BotDifficulty.Easy: reactionTime = 0.95f; aimError = 2.8f; damage = 9f; visionRange = 45f; break;
                case BotDifficulty.Hard: reactionTime = 0.32f; aimError = 1.05f; damage = 15f; visionRange = 70f; break;
                default: reactionTime = 0.55f; aimError = 1.75f; damage = 12f; visionRange = 58f; break;
            }
        }

        public void Respawn(Vector3 position, float yaw)
        {
            cc.enabled = false;
            transform.position = position;
            transform.rotation = Quaternion.Euler(0, yaw, 0);
            cc.enabled = true;
            me.health.ResetHealth(2f);
            body.Revive();
            state = State.Objective;
            target = null;
            ammo = 30;
            reloadDone = -1f;
            path.Clear();
            pathGoal = new Vector3(9999, 0, 0);
            objective = null;
            desiredYaw = yaw;
            lastPos = position;
            ApplyDifficulty();
        }

        // ---------- events ----------

        /// <summary>Gunshots alert enemies within hearing range (they search the sound, they do not see through walls).</summary>
        public static void ReportGunfire(Soldier shooter, Vector3 position)
        {
            foreach (SoldierAI ai in All)
            {
                if (ai.me == null || !ai.me.Alive || ai.me.team == shooter.team) continue;
                if (ai.state == State.Engage || ai.state == State.Cover) continue;
                if ((ai.transform.position - position).sqrMagnitude > 45f * 45f) continue;
                ai.lastKnownPos = position;
                ai.state = State.Search;
                ai.searchUntil = Time.time + 7f;
                ai.FaceTowards(position);
            }
        }

        void OnDamaged(DamageInfo info)
        {
            if (info.attacker == null) return;
            lastKnownPos = info.attacker.transform.position;
            FaceTowards(lastKnownPos);
            if (state != State.Engage) { state = State.Search; searchUntil = Time.time + 6f; }
            if (me.health.Fraction < 0.5f && Random.value < 0.6f) EnterCover();
        }

        void OnDied(DamageInfo info)
        {
            body.Die(info.direction);
            cc.enabled = false;
            target = null;
            if (MatchManager.I != null) MatchManager.I.ReportDeath(me, info);
        }

        // ---------- main loop ----------

        void Update()
        {
            float dt = Time.deltaTime;
            if (me == null) return;
            if (!me.Alive || MatchManager.I == null || MatchManager.I.State != MatchState.Playing)
            {
                body.Animate(0f, false, dt);
                return;
            }

            if (Time.time >= perceiveAt) { perceiveAt = Time.time + 0.15f; Perceive(); }
            if (reloadDone > 0f && Time.time >= reloadDone) { ammo = (int)MagSize; reloadDone = -1f; }

            Vector3 move = Vector3.zero;
            crouched = false;
            switch (state)
            {
                case State.Objective: move = DoObjective(); break;
                case State.Engage: move = DoEngage(); break;
                case State.Cover: move = DoCover(); break;
                case State.Search: move = DoSearch(); break;
            }

            move += Separation();
            if (!cc.enabled) return;
            if (cc.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            verticalVelocity += -19f * dt;
            cc.Move((move + Vector3.up * verticalVelocity) * dt);

            Vector3 flat = transform.position - lastPos;
            flat.y = 0f;
            float speed = flat.magnitude / Mathf.Max(dt, 0.0001f);
            if (move.sqrMagnitude > 0.5f && speed < move.magnitude * 0.25f) stuckTimer += dt; else stuckTimer = 0f;
            if (stuckTimer > 1.2f) { stuckTimer = 0f; repathAt = 0f; strafeDir = -strafeDir; }
            lastPos = transform.position;

            float yaw = Mathf.MoveTowardsAngle(transform.eulerAngles.y, desiredYaw, 300f * dt);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            body.Animate(speed, crouched, dt);
        }

        void Perceive()
        {
            Soldier best = null;
            float bestScore = float.MaxValue;
            Vector3 eye = me.Eye;
            foreach (Soldier s in Soldier.All)
            {
                if (s == null || s.team == me.team || !s.Alive) continue;
                Vector3 to = s.AimPoint - eye;
                float d = to.magnitude;
                float range = visionRange;
                PlayerController pc = s.isPlayer ? s.GetComponent<PlayerController>() : null;
                if (pc != null && pc.IsCrouched) range *= 0.7f; // crouching is harder to spot at distance
                if (d > range) continue;
                bool remembered = s == target && Time.time - lastSeenTime < 1.5f;
                if (!remembered && d > 5f && Vector3.Angle(transform.forward, to) > FovHalf) continue;
                if (!Ballistics.LineOfSight(eye, s.AimPoint, transform, s.transform)) continue;
                float score = d - (s == target ? 10f : 0f);
                if (score < bestScore) { bestScore = score; best = s; }
            }

            targetVisible = best != null;
            if (best != null)
            {
                if (best != target)
                {
                    target = best;
                    float dist = Vector3.Distance(best.transform.position, transform.position);
                    reactionReady = Time.time + reactionTime * (dist > 30f ? 1.35f : 1f) * Random.Range(0.8f, 1.2f);
                }
                lastSeenTime = Time.time;
                lastKnownPos = best.transform.position;
                if (me.team == Team.Blue) best.spottedUntil = Time.time + 2f;
                if (state != State.Cover) state = State.Engage;
            }
            else if (target != null && Time.time - lastSeenTime > 0.8f && state == State.Engage)
            {
                state = State.Search;
                searchUntil = Time.time + 6f;
            }
        }

        // ---------- states ----------

        Vector3 DoObjective()
        {
            if (objective == null || Time.time > objectiveRethinkAt || objective.IsOwnedBy(me.team) && !objective.Contested && Random.value < 0.002f)
                ChooseObjective();
            if (objective == null) return Vector3.zero;

            Vector3 flatTo = objective.transform.position - transform.position;
            flatTo.y = 0f;
            if (flatTo.magnitude < objective.radius * 0.8f)
            {
                if (Time.time > wanderAt)
                {
                    wanderAt = Time.time + Random.Range(3f, 6f);
                    graph.NodesNear(objective.transform.position, objective.radius * 0.8f, nearby);
                    wanderTarget = nearby.Count > 0 ? nearby[Random.Range(0, nearby.Count)].pos : objective.transform.position;
                }
                return FollowTo(wanderTarget, CombatSpeed, true);
            }
            return FollowTo(objective.transform.position, RunSpeed, true);
        }

        void ChooseObjective()
        {
            objectiveRethinkAt = Time.time + Random.Range(8f, 14f);
            CapturePoint best = null;
            float bestScore = float.MaxValue;
            foreach (CapturePoint p in map.points)
            {
                float d = Vector3.Distance(p.transform.position, transform.position);
                float score = d;
                if (p.IsOwnedBy(me.team)) score += p.Contested ? -15f : 60f;
                score += Random.Range(0f, 25f); // spread bots across points
                if (score < bestScore) { bestScore = score; best = p; }
            }
            objective = best;
        }

        Vector3 DoEngage()
        {
            if (target == null || !target.Alive) { target = null; state = State.Objective; return Vector3.zero; }
            Vector3 toT = target.transform.position - transform.position;
            float dist = toT.magnitude;
            FaceTowards(target.transform.position);

            if (ammo <= 0 && reloadDone < 0f)
            {
                reloadDone = Time.time + 2.3f;
                if (Random.value < 0.6f) { EnterCover(); return Vector3.zero; }
            }

            if (targetVisible && reloadDone < 0f && Time.time >= reactionReady) TryShoot(dist);

            if (me.health.Fraction < 0.4f && Time.time > coverUntil + 4f) { EnterCover(); return Vector3.zero; }

            crouched = dist > 25f && targetVisible && Random.value < 0.002f || crouched;
            if (!targetVisible) return FollowTo(lastKnownPos, RunSpeed, false);
            if (dist > 32f) return FollowTo(target.transform.position, CombatSpeed * 1.4f, false);

            if (Time.time > strafeChangeAt) { strafeChangeAt = Time.time + Random.Range(0.8f, 2f); strafeDir = Random.value < 0.5f ? -1f : 1f; }
            Vector3 side = Vector3.Cross(Vector3.up, toT.normalized) * strafeDir;
            Vector3 approach = dist < 10f ? -toT.normalized * 0.5f : Vector3.zero;
            return (side + approach).normalized * CombatSpeed;
        }

        void EnterCover()
        {
            Vector3 threat = target != null ? target.AimPoint : lastKnownPos + Vector3.up * 1.2f;
            graph.NodesNear(transform.position, 13f, nearby);
            WaypointGraph.Node best = null;
            float bestD = float.MaxValue;
            int checks = 0;
            foreach (WaypointGraph.Node n in nearby)
            {
                if (!n.cover) continue;
                float d = (n.pos - transform.position).sqrMagnitude;
                if (d >= bestD) continue;
                if (++checks > 30) break;
                if (Ballistics.LineOfSight(n.pos + Vector3.up * 1.1f, threat, transform, target != null ? target.transform : null)) continue;
                best = n;
                bestD = d;
            }
            if (best == null) return;
            state = State.Cover;
            wanderTarget = best.pos;
            coverUntil = Time.time + Random.Range(2.5f, 4.5f);
            path.Clear();
            pathGoal = new Vector3(9999, 0, 0);
        }

        Vector3 DoCover()
        {
            if (target != null) FaceTowards(target.transform.position);
            if (ammo <= 0 && reloadDone < 0f) reloadDone = Time.time + 2.3f;
            float d = Vector3.Distance(transform.position, wanderTarget);
            if (d < 0.8f)
            {
                crouched = true;
                if (Time.time > coverUntil && reloadDone < 0f)
                    state = target != null && target.Alive && targetVisible ? State.Engage : State.Search;
                return Vector3.zero;
            }
            if (Time.time > coverUntil + 4f) state = State.Search;
            return FollowTo(wanderTarget, RunSpeed, true);
        }

        Vector3 DoSearch()
        {
            if (Time.time > searchUntil) { state = State.Objective; target = null; return Vector3.zero; }
            if (Vector3.Distance(transform.position, lastKnownPos) < 2f)
            {
                desiredYaw += 90f * Time.deltaTime;
                return Vector3.zero;
            }
            return FollowTo(lastKnownPos, RunSpeed * 0.8f, true);
        }

        // ---------- combat ----------

        void TryShoot(float dist)
        {
            if (Time.time < nextShot || ammo <= 0) return;
            if (Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.y, desiredYaw)) > 12f) return;
            if (burstLeft <= 0)
            {
                burstLeft = Random.Range(3, 7);
                nextShot = Time.time + Random.Range(0.35f, 0.8f);
                return;
            }
            burstLeft--;
            ammo--;
            nextShot = Time.time + FireInterval;

            Vector3 origin = body.Muzzle != null ? body.Muzzle.position : me.Eye;
            Vector3 aimAt = target.AimPoint;
            float targetSpeed = 0f;
            CharacterController tcc = target.body;
            if (tcc != null) { Vector3 v = tcc.velocity; v.y = 0f; targetSpeed = v.magnitude; }
            float errDeg = aimError * (1f + targetSpeed * 0.25f) * (dist > 25f ? 1.25f : 1f);
            Vector3 dir = WeaponController.SpreadDirection((aimAt - origin).normalized, errDeg);
            RaycastHit hit;
            Vector3 end = origin + dir * 200f;
            if (Ballistics.Raycast(origin, dir, 200f, transform, out hit))
            {
                end = hit.point;
                bool hs, killed, head;
                Ballistics.ApplyHit(hit, dir, damage, 1.8f, me, out hs, out killed, out head);
            }
            if (EffectsManager.I != null)
            {
                EffectsManager.I.MuzzleFlash(origin, 0.8f);
                EffectsManager.I.Tracer(origin, end);
            }
            Camera listener = Camera.main;
            float listenerDist = listener != null ? Vector3.Distance(listener.transform.position, origin) : 0f;
            AudioManager.I.Play(listenerDist > 35f ? AudioLibrary.DistantShot : AudioLibrary.RifleShot, origin, listenerDist > 35f ? 0.9f : 0.55f, Random.Range(0.92f, 1.06f));
            ReportGunfire(me, origin);
        }

        // ---------- movement ----------

        Vector3 FollowTo(Vector3 goal, float speed, bool faceMovement)
        {
            if ((goal - pathGoal).sqrMagnitude > 4f || Time.time > repathAt)
            {
                repathAt = Time.time + Random.Range(2f, 3f);
                pathGoal = goal;
                if (!graph.FindPath(transform.position, goal, path)) { path.Clear(); path.Add(goal); }
                pathIndex = 0;
                // skip the first node if we are already past it
                if (path.Count > 1 && Vector3.Distance(transform.position, path[1]) < Vector3.Distance(path[0], path[1])) pathIndex = 1;
            }
            if (pathIndex >= path.Count) return Vector3.zero;
            Vector3 next = path[pathIndex];
            Vector3 d = next - transform.position;
            d.y = 0f;
            if (d.magnitude < 0.7f)
            {
                pathIndex++;
                if (pathIndex >= path.Count) return Vector3.zero;
                d = path[pathIndex] - transform.position;
                d.y = 0f;
            }
            Vector3 v = d.normalized * speed;
            if (faceMovement && v.sqrMagnitude > 0.01f) desiredYaw = Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg;
            return v;
        }

        Vector3 Separation()
        {
            Vector3 push = Vector3.zero;
            foreach (SoldierAI o in All)
            {
                if (o == this || o.me == null || !o.me.Alive) continue;
                Vector3 d = transform.position - o.transform.position;
                d.y = 0f;
                float m = d.magnitude;
                if (m > 0.01f && m < 1.3f) push += d / m * (1.3f - m) * 2.5f;
            }
            return push;
        }

        void FaceTowards(Vector3 p)
        {
            Vector3 d = p - transform.position;
            if (d.sqrMagnitude > 0.01f) desiredYaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
        }
    }
}
