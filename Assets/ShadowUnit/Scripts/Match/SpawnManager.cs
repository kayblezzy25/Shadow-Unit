using System.Collections.Generic;
using UnityEngine;

namespace ShadowUnit
{
    /// <summary>Chooses safe spawn positions: base spawns, or a held capture point with no enemies nearby.</summary>
    public class SpawnManager
    {
        readonly MapInfo map;
        readonly List<WaypointGraph.Node> nodes = new List<WaypointGraph.Node>();

        public SpawnManager(MapInfo mapInfo) { map = mapInfo; }

        public Vector3 GetSpawn(Team team, out float yaw)
        {
            yaw = team == Team.Blue ? 0f : 180f;

            // forward spawn at a held, quiet capture point half of the time
            if (Random.value < 0.5f)
            {
                foreach (CapturePoint p in map.points)
                {
                    if (!p.IsOwnedBy(team) || p.Contested || EnemyNear(team, p.transform.position, 28f)) continue;
                    map.graph.NodesNear(p.transform.position + (team == Team.Blue ? Vector3.back : Vector3.forward) * 8f, 5f, nodes);
                    if (nodes.Count > 0) return nodes[Random.Range(0, nodes.Count)].pos + Vector3.up * 0.1f;
                }
            }

            List<Vector3> list = team == Team.Blue ? map.blueSpawns : map.redSpawns;
            Vector3 best = list[0];
            float bestScore = float.MinValue;
            foreach (Vector3 s in list)
            {
                float score = NearestEnemyDistance(team, s) - (FriendlyBlocking(s) ? 50f : 0f) + Random.Range(0f, 3f);
                if (score > bestScore) { bestScore = score; best = s; }
            }
            return best;
        }

        static bool EnemyNear(Team team, Vector3 p, float r)
        {
            foreach (Soldier s in Soldier.All)
                if (s.Alive && s.team != team && (s.transform.position - p).sqrMagnitude < r * r) return true;
            return false;
        }

        static float NearestEnemyDistance(Team team, Vector3 p)
        {
            float best = 999f;
            foreach (Soldier s in Soldier.All)
                if (s.Alive && s.team != team) best = Mathf.Min(best, Vector3.Distance(s.transform.position, p));
            return best;
        }

        static bool FriendlyBlocking(Vector3 p)
        {
            foreach (Soldier s in Soldier.All)
                if (s.Alive && (s.transform.position - p).sqrMagnitude < 1.5f) return true;
            return false;
        }
    }
}
