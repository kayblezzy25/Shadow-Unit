using UnityEngine;

namespace ShadowUnit
{
    /// <summary>Raycast helpers that ignore the shooter's own colliders.</summary>
    public static class Ballistics
    {
        static readonly RaycastHit[] Buffer = new RaycastHit[32];

        public static bool Raycast(Vector3 origin, Vector3 dir, float range, Transform ignoreRoot, out RaycastHit hit)
        {
            int n = Physics.RaycastNonAlloc(origin, dir, Buffer, range, ~0, QueryTriggerInteraction.Ignore);
            hit = default(RaycastHit);
            float best = float.MaxValue;
            bool found = false;
            for (int i = 0; i < n; i++)
            {
                RaycastHit h = Buffer[i];
                if (ignoreRoot != null && h.collider.transform.IsChildOf(ignoreRoot)) continue;
                if (h.distance < best) { best = h.distance; hit = h; found = true; }
            }
            return found;
        }

        /// <summary>True when nothing except the two given roots blocks the segment.</summary>
        public static bool LineOfSight(Vector3 from, Vector3 to, Transform ignoreA, Transform ignoreB)
        {
            Vector3 d = to - from;
            float dist = d.magnitude;
            if (dist < 0.01f) return true;
            int n = Physics.RaycastNonAlloc(from, d / dist, Buffer, dist, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Transform t = Buffer[i].collider.transform;
                if (ignoreA != null && t.IsChildOf(ignoreA)) continue;
                if (ignoreB != null && t.IsChildOf(ignoreB)) continue;
                return false;
            }
            return true;
        }

        /// <summary>Applies a hit to whatever was struck and spawns the right impact effect.</summary>
        public static void ApplyHit(RaycastHit hit, Vector3 dir, float damage, float headshotMultiplier, Soldier shooter, out bool hitSoldier, out bool killed, out bool headshot)
        {
            hitSoldier = false; killed = false; headshot = false;
            Soldier victim = hit.collider.GetComponentInParent<Soldier>();
            if (victim != null && victim.Alive)
            {
                hitSoldier = true;
                if (shooter != null && victim.team == shooter.team) return; // no friendly fire
                headshot = hit.point.y >= victim.HeadshotHeight;
                float dmg = damage * (headshot ? headshotMultiplier : 1f);
                victim.health.TakeDamage(new DamageInfo { amount = dmg, point = hit.point, direction = dir, attacker = shooter, headshot = headshot });
                killed = victim.health.IsDead;
                if (EffectsManager.I != null) EffectsManager.I.Impact(hit.point, -dir, true);
            }
            else if (EffectsManager.I != null)
            {
                EffectsManager.I.Impact(hit.point, hit.normal, false);
                if (AudioManager.I != null) AudioManager.I.Play(AudioLibrary.Impact, hit.point, 0.5f, Random.Range(0.85f, 1.2f));
            }
        }
    }
}
