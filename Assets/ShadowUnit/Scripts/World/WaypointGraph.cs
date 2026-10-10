using System.Collections.Generic;
using UnityEngine;

namespace ShadowUnit
{
    /// <summary>
    /// Grid navigation graph sampled from the physics scene after the map is built.
    /// Needs no NavMesh package, so it works the same in every Unity version.
    /// </summary>
    public class WaypointGraph
    {
        public class Node
        {
            public int id;
            public Vector3 pos;
            public readonly List<int> links = new List<int>(8);
            /// <summary>Next to an obstacle: a candidate cover position.</summary>
            public bool cover;
        }

        public readonly List<Node> nodes = new List<Node>();
        float spacing;
        Vector2 min;
        int nx, nz;
        int[] grid;

        const float Radius = 0.38f;
        const float Height = 1.8f;

        public void Build(Vector2 minXZ, Vector2 maxXZ, float cellSize)
        {
            Physics.SyncTransforms();
            spacing = cellSize;
            min = minXZ;
            nx = Mathf.FloorToInt((maxXZ.x - minXZ.x) / cellSize) + 1;
            nz = Mathf.FloorToInt((maxXZ.y - minXZ.y) / cellSize) + 1;
            grid = new int[nx * nz];
            nodes.Clear();

            for (int iz = 0; iz < nz; iz++)
            for (int ix = 0; ix < nx; ix++)
            {
                grid[iz * nx + ix] = -1;
                var top = new Vector3(min.x + ix * cellSize, 2.6f, min.y + iz * cellSize);
                RaycastHit hit;
                if (!Physics.Raycast(top, Vector3.down, out hit, 4f, ~0, QueryTriggerInteraction.Ignore)) continue;
                if (hit.point.y > 0.25f) continue; // on top of cover, not walkable ground
                Vector3 p = hit.point;
                if (Physics.CheckCapsule(p + Vector3.up * (Radius + 0.06f), p + Vector3.up * (Height - Radius), Radius, ~0, QueryTriggerInteraction.Ignore)) continue;
                var n = new Node { id = nodes.Count, pos = p };
                grid[iz * nx + ix] = n.id;
                nodes.Add(n);
            }

            int[,] dirs = { { 1, 0 }, { 0, 1 }, { 1, 1 }, { 1, -1 } };
            var blocked = new int[nodes.Count];
            for (int iz = 0; iz < nz; iz++)
            for (int ix = 0; ix < nx; ix++)
            {
                int a = grid[iz * nx + ix];
                if (a < 0) continue;
                for (int d = 0; d < 4; d++)
                {
                    int jx = ix + dirs[d, 0], jz = iz + dirs[d, 1];
                    if (jx < 0 || jz < 0 || jx >= nx || jz >= nz) continue;
                    int b = grid[jz * nx + jx];
                    if (b < 0) { blocked[a]++; continue; }
                    if (Clear(nodes[a].pos, nodes[b].pos))
                    {
                        nodes[a].links.Add(b);
                        nodes[b].links.Add(a);
                    }
                    else { blocked[a]++; blocked[b]++; }
                }
            }
            for (int i = 0; i < nodes.Count; i++)
                nodes[i].cover = blocked[i] > 0 || nodes[i].links.Count < 6;
        }

        static bool Clear(Vector3 a, Vector3 b)
        {
            Vector3 d = b - a;
            float dist = d.magnitude;
            Vector3 dir = d / dist;
            foreach (float h in new[] { 0.6f, 1.5f })
                if (Physics.SphereCast(a + Vector3.up * h, Radius * 0.85f, dir, out _, dist, ~0, QueryTriggerInteraction.Ignore))
                    return false;
            return true;
        }

        public Node Nearest(Vector3 p)
        {
            if (nodes.Count == 0) return null;
            int cx = Mathf.RoundToInt((p.x - min.x) / spacing), cz = Mathf.RoundToInt((p.z - min.y) / spacing);
            Node best = null;
            float bestD = float.MaxValue;
            for (int r = 0; r <= 6 && best == null; r++)
            {
                for (int iz = cz - r; iz <= cz + r; iz++)
                for (int ix = cx - r; ix <= cx + r; ix++)
                {
                    if (ix < 0 || iz < 0 || ix >= nx || iz >= nz) continue;
                    int id = grid[iz * nx + ix];
                    if (id < 0) continue;
                    float d = (nodes[id].pos - p).sqrMagnitude;
                    if (d < bestD) { bestD = d; best = nodes[id]; }
                }
            }
            return best;
        }

        /// <summary>Nodes within a radius, used for cover search and wandering.</summary>
        public void NodesNear(Vector3 p, float radius, List<Node> result)
        {
            result.Clear();
            int r = Mathf.CeilToInt(radius / spacing);
            int cx = Mathf.RoundToInt((p.x - min.x) / spacing), cz = Mathf.RoundToInt((p.z - min.y) / spacing);
            float r2 = radius * radius;
            for (int iz = cz - r; iz <= cz + r; iz++)
            for (int ix = cx - r; ix <= cx + r; ix++)
            {
                if (ix < 0 || iz < 0 || ix >= nx || iz >= nz) continue;
                int id = grid[iz * nx + ix];
                if (id >= 0 && (nodes[id].pos - p).sqrMagnitude <= r2) result.Add(nodes[id]);
            }
        }

        // ---------- A* ----------
        float[] gScore;
        int[] cameFrom;
        int[] closedStamp;
        int stamp;
        readonly List<int> heap = new List<int>();
        float[] fScore;

        public bool FindPath(Vector3 from, Vector3 to, List<Vector3> path)
        {
            path.Clear();
            Node s = Nearest(from), g = Nearest(to);
            if (s == null || g == null) return false;
            int n = nodes.Count;
            if (gScore == null || gScore.Length != n)
            {
                gScore = new float[n]; fScore = new float[n]; cameFrom = new int[n]; closedStamp = new int[n];
            }
            stamp++;
            if (stamp == int.MaxValue) { stamp = 1; System.Array.Clear(closedStamp, 0, n); }
            // stamp*2 = open, stamp*2+1 = closed, tracked via closedStamp values
            int openMark = stamp * 2, closedMark = stamp * 2 + 1;
            heap.Clear();
            gScore[s.id] = 0f;
            fScore[s.id] = Vector3.Distance(s.pos, g.pos);
            cameFrom[s.id] = -1;
            closedStamp[s.id] = openMark;
            Push(s.id);
            int expanded = 0;
            while (heap.Count > 0)
            {
                int cur = Pop();
                if (closedStamp[cur] == closedMark) continue;
                closedStamp[cur] = closedMark;
                if (cur == g.id)
                {
                    for (int c = cur; c != -1; c = cameFrom[c]) path.Add(nodes[c].pos);
                    path.Reverse();
                    path.Add(to);
                    return true;
                }
                if (++expanded > 6000) break;
                foreach (int nb in nodes[cur].links)
                {
                    if (closedStamp[nb] == closedMark) continue;
                    float tentative = gScore[cur] + Vector3.Distance(nodes[cur].pos, nodes[nb].pos);
                    if (closedStamp[nb] != openMark || tentative < gScore[nb])
                    {
                        gScore[nb] = tentative;
                        fScore[nb] = tentative + Vector3.Distance(nodes[nb].pos, g.pos);
                        cameFrom[nb] = cur;
                        closedStamp[nb] = openMark;
                        Push(nb);
                    }
                }
            }
            return false;
        }

        void Push(int id)
        {
            heap.Add(id);
            int i = heap.Count - 1;
            while (i > 0)
            {
                int p = (i - 1) / 2;
                if (fScore[heap[p]] <= fScore[heap[i]]) break;
                int t = heap[p]; heap[p] = heap[i]; heap[i] = t;
                i = p;
            }
        }

        int Pop()
        {
            int top = heap[0];
            int last = heap.Count - 1;
            heap[0] = heap[last];
            heap.RemoveAt(last);
            int i = 0;
            while (true)
            {
                int l = i * 2 + 1, r = l + 1, m = i;
                if (l < heap.Count && fScore[heap[l]] < fScore[heap[m]]) m = l;
                if (r < heap.Count && fScore[heap[r]] < fScore[heap[m]]) m = r;
                if (m == i) break;
                int t = heap[m]; heap[m] = heap[i]; heap[i] = t;
                i = m;
            }
            return top;
        }
    }
}
