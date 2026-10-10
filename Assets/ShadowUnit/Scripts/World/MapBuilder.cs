using System.Collections.Generic;
using UnityEngine;

namespace ShadowUnit
{
    /// <summary>Everything the match needs to know about the built map.</summary>
    public class MapInfo
    {
        public string name = "The Wasteland Ward";
        public Transform root;
        public Light sun;
        public readonly List<CapturePoint> points = new List<CapturePoint>();
        public readonly List<Vector3> blueSpawns = new List<Vector3>();
        public readonly List<Vector3> redSpawns = new List<Vector3>();
        public WaypointGraph graph;
        public float halfSize = 70f;
        public Vector3 menuCameraPivot = new Vector3(0, 0, 0);
    }

    /// <summary>
    /// Builds the grey-box Battle Mode map "The Wasteland Ward" (docs/WORLD.md) from primitives:
    /// a canyon floor walled by cliffs, the Market District (A) in the west, the crashed freighter of the Hub (B)
    /// in the middle, and the Watchtower (C) across the river in the east, joined by the Outer Road.
    /// Blue spawns in the south, Red in the north. The floor is flat because bot navigation walks ground level only;
    /// height comes from stacked buildings, the freighter and the cliffs. Placeholder geometry by design.
    /// </summary>
    public static class MapBuilder
    {
        static Transform root;
        static System.Random rng;

        // canyon palette: dusty red-orange rock and sand, rusted metal, cold concrete underground
        static Material Ground => MaterialLibrary.Lit("sand", new Color(0.66f, 0.5f, 0.37f), MaterialLibrary.Tex("dirt"), new Vector2(40, 40), 0.05f);
        static Material RoadDirt(Vector2 t) => MaterialLibrary.Lit("roaddirt", new Color(0.5f, 0.4f, 0.31f), MaterialLibrary.Tex("dirt"), t, 0.05f);
        static Material Rock(Vector2 t) => MaterialLibrary.Lit("rock", new Color(0.62f, 0.38f, 0.27f), MaterialLibrary.Tex("concrete"), t, 0.05f);
        static Material Concrete(Vector2 t) => MaterialLibrary.Lit("concrete", new Color(0.62f, 0.6f, 0.56f), MaterialLibrary.Tex("concrete"), t, 0.1f);
        static Material Plaster(Vector2 t) => MaterialLibrary.Lit("plaster", new Color(0.74f, 0.66f, 0.55f), MaterialLibrary.Tex("concrete"), t, 0.08f);
        static Material Sandbag(Vector2 t) => MaterialLibrary.Lit("sandbag", new Color(0.66f, 0.58f, 0.43f), MaterialLibrary.Tex("sandbag"), t, 0.02f);
        static Material Wood(Vector2 t) => MaterialLibrary.Lit("wood", new Color(0.55f, 0.42f, 0.28f), MaterialLibrary.Tex("wood"), t, 0.1f);
        static Material Metal(Color c) => MaterialLibrary.Lit("metal" + ColorUtility.ToHtmlStringRGB(c), c, MaterialLibrary.Tex("metal"), new Vector2(2, 1), 0.35f, 0.4f);
        static Material Rust => Metal(new Color(0.45f, 0.27f, 0.18f));
        static Material Hull => MaterialLibrary.Lit("hull", new Color(0.56f, 0.57f, 0.56f), MaterialLibrary.Tex("metal"), new Vector2(4, 2), 0.3f, 0.5f);
        static Material Rubble => MaterialLibrary.Lit("rubble", new Color(0.58f, 0.45f, 0.36f), MaterialLibrary.Tex("concrete"), Vector2.one, 0.05f);
        static Material Burnt => MaterialLibrary.Lit("burnt", new Color(0.16f, 0.15f, 0.14f), MaterialLibrary.Tex("metal"), Vector2.one, 0.3f, 0.5f);
        static Material Water => MaterialLibrary.Lit("water", new Color(0.16f, 0.36f, 0.4f), null, null, 0.9f, 0.1f);
        static Material Lantern => MaterialLibrary.Emissive("lantern", new Color(0.9f, 0.2f, 0.12f), new Color(1.6f, 0.35f, 0.15f));
        static Material TunnelLight => MaterialLibrary.Emissive("tubelight", new Color(0.8f, 1f, 0.9f), new Color(1.2f, 1.6f, 1.4f));

        // layout anchors (metres; x east, z north)
        // embankments and railings are taller than a jump (1.05 m), so nobody can end up stuck in the channel
        const float RiverX = 47f, RiverHalfWidth = 2.5f, EmbankmentHeight = 1.5f;
        static readonly float[] BridgeZ = { -12f, 22f };
        const float BridgeHalfWidth = 3f;

        static float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        static Vector2 Tile(float w, float h, float unit) => new Vector2(Mathf.Max(0.5f, Mathf.Round(w / unit * 2f) / 2f), Mathf.Max(0.5f, Mathf.Round(h / unit * 2f) / 2f));

        public static MapInfo Build()
        {
            rng = new System.Random(2068);
            var info = new MapInfo();
            root = new GameObject("Map_WastelandWard").transform;
            info.root = root;

            Lighting(info);
            Box(new Vector3(0, -0.5f, 0), new Vector3(300, 1, 300), Ground);
            Cliffs(info.halfSize);

            // capture points: A Market District (west), B the Hub (centre), C the Watchtower (east, across the river).
            // All three sit on the centre line so both bases are the same distance from each (C: the bridges differ, the routes don't).
            info.points.Add(Point("A", "Market District", new Vector3(-43, 0, 1)));
            info.points.Add(Point("B", "The Hub", new Vector3(2, 0, 0)));
            info.points.Add(Point("C", "Watchtower", new Vector3(59, 0, 0)));

            OuterRoad();
            MarketDistrict();
            UndercityTunnel();
            TheHub();
            River();
            Watchtower();
            RoadsideCover();

            Physics.SyncTransforms();
            for (int i = 0, placed = 0; i < 90 && placed < 16; i++)
            {
                var p = new Vector3(R(-62, 62), 0, R(-48, 48));
                if (Mathf.Abs(p.x - RiverX) < 7f) continue; // keep the river and its banks clear
                if (Physics.CheckSphere(p + Vector3.up * 3.8f, 3.5f, ~0, QueryTriggerInteraction.Ignore)) continue;
                Rocks(p);
                placed++;
            }

            // team bases: Blue south, Red north
            Base(info.blueSpawns, -1);
            Base(info.redSpawns, 1);

            Outskirts(info.halfSize);

            info.graph = new WaypointGraph();
            info.graph.Build(new Vector2(-info.halfSize + 1.5f, -info.halfSize + 1.5f), new Vector2(info.halfSize - 1.5f, info.halfSize - 1.5f), 1.5f);

            // the map never moves after this point: batch it to cut draw calls (capture point flags move, so they stay out)
            var statics = new List<GameObject>();
            foreach (MeshRenderer r in root.GetComponentsInChildren<MeshRenderer>())
                if (r.GetComponentInParent<CapturePoint>() == null) statics.Add(r.gameObject);
            StaticBatchingUtility.Combine(statics.ToArray(), root.gameObject);
            return info;
        }

        // ---------- lighting ----------

        static void Lighting(MapInfo info)
        {
            var sunGo = new GameObject("Sun");
            sunGo.transform.SetParent(root);
            Light sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.84f, 0.64f);
            sun.intensity = 1.3f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.85f;
            sunGo.transform.rotation = Quaternion.Euler(30f, -55f, 0f); // low afternoon sun for long canyon shadows
            info.sun = sun;
            RenderSettings.sun = sun;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.6f, 0.6f);
            RenderSettings.ambientEquatorColor = new Color(0.56f, 0.45f, 0.36f);
            RenderSettings.ambientGroundColor = new Color(0.3f, 0.22f, 0.16f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.0075f;
            RenderSettings.fogColor = new Color(0.78f, 0.65f, 0.52f); // dusty haze
            Material sky = Resources.Load<Material>("ShadowUnit/SU_Sky");
            if (sky != null) RenderSettings.skybox = sky;
        }

        // ---------- primitives ----------

        static GameObject Box(Vector3 pos, Vector3 size, Material m, float rotY = 0f, bool collider = true, Transform parent = null)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            if (!collider) Object.DestroyImmediate(g.GetComponent<Collider>());
            g.transform.SetParent(parent != null ? parent : root, false);
            g.transform.localPosition = pos;
            g.transform.localRotation = Quaternion.Euler(0, rotY, 0);
            g.transform.localScale = size;
            g.GetComponent<Renderer>().sharedMaterial = m;
            return g;
        }

        /// <summary>A box with no collider and no shadow, for distant scenery.</summary>
        static GameObject Scenery(Vector3 pos, Vector3 size, Material m, float rotY = 0f)
        {
            GameObject g = Box(pos, size, m, rotY, false);
            g.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return g;
        }

        static GameObject Cyl(Vector3 pos, float radius, float height, Material m, bool collider = true, Transform parent = null)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            if (!collider) Object.DestroyImmediate(g.GetComponent<Collider>());
            g.transform.SetParent(parent != null ? parent : root, false);
            g.transform.localPosition = pos + Vector3.up * height * 0.5f;
            g.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
            g.GetComponent<Renderer>().sharedMaterial = m;
            return g;
        }

        static Transform Group(string name, Vector3 pos, float rotY = 0f)
        {
            var t = new GameObject(name).transform;
            t.SetParent(root, false);
            t.localPosition = pos;
            t.localRotation = Quaternion.Euler(0, rotY, 0);
            return t;
        }

        /// <summary>A flat strip between two points (roads), with no collider.</summary>
        static void Strip(Vector3 a, Vector3 b, float width, Material m, float y)
        {
            Vector3 d = b - a;
            float len = d.magnitude;
            float rot = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            Box((a + b) * 0.5f + Vector3.up * y, new Vector3(width, 0.02f, len + width * 0.5f), m, rot, false);
        }

        // ---------- modules ----------

        struct Opening { public float start, end, bottom, top; }

        /// <summary>Wall along local X from x=0..length with gaps for doors and windows.</summary>
        static void Wall(Transform parent, Vector3 start, float rotY, float length, float height, List<Opening> openings, Material m, float thickness = 0.3f, bool ruined = false)
        {
            var t = new GameObject("Wall").transform;
            t.SetParent(parent, false);
            t.localPosition = start;
            t.localRotation = Quaternion.Euler(0, rotY, 0);
            openings.Sort((a, b) => a.start.CompareTo(b.start));
            float cursor = 0f;
            System.Action<float, float, float, float> seg = (x0, x1, y0, y1) =>
            {
                if (x1 - x0 < 0.05f || y1 - y0 < 0.05f) return;
                float top = y1;
                if (ruined && y1 >= height - 0.01f) top = y0 + (y1 - y0) * R(0.35f, 1f);
                Box(new Vector3((x0 + x1) * 0.5f, (y0 + top) * 0.5f, 0), new Vector3(x1 - x0, top - y0, thickness), m, 0, true, t);
            };
            foreach (Opening o in openings)
            {
                if (ruined)
                {
                    float steps = Mathf.Max(1, Mathf.Floor((o.start - cursor) / 1.2f));
                    float w = (o.start - cursor) / steps;
                    for (int i = 0; i < steps; i++) seg(cursor + i * w, cursor + (i + 1) * w, 0, height);
                }
                else seg(cursor, o.start, 0, height);
                seg(o.start, o.end, 0, o.bottom);
                seg(o.start, o.end, o.top, height);
                cursor = o.end;
            }
            if (ruined)
            {
                float steps = Mathf.Max(1, Mathf.Floor((length - cursor) / 1.2f));
                float w = (length - cursor) / steps;
                for (int i = 0; i < steps; i++) seg(cursor + i * w, cursor + (i + 1) * w, 0, height);
            }
            else seg(cursor, length, 0, height);
        }

        static List<Opening> Openings(float length, bool door, bool windows, float doorWidth = 2.2f)
        {
            var list = new List<Opening>();
            float doorCenter = length * 0.5f;
            if (door) list.Add(new Opening { start = doorCenter - doorWidth / 2f, end = doorCenter + doorWidth / 2f, bottom = 0f, top = doorWidth > 3f ? 3.6f : 2.4f });
            if (windows)
            {
                for (float x = 1.4f; x + 1.2f < length - 0.8f; x += 3.2f)
                {
                    if (door && x + 1.2f > doorCenter - doorWidth / 2f - 0.4f && x < doorCenter + doorWidth / 2f + 0.4f) continue;
                    list.Add(new Opening { start = x, end = x + 1.2f, bottom = 1.05f, top = 1.95f });
                }
            }
            return list;
        }

        /// <summary>Enterable single-storey building. Doors on the listed sides (N/S/E/W). Returns its group.</summary>
        static Transform Building(Vector3 center, float w, float d, float h, char[] doors, bool ruined, Material wall = null)
        {
            Transform t = Group(ruined ? "Ruined building" : "Building", center);
            if (wall == null) wall = Plaster(Tile(w, h, 3f));
            Box(new Vector3(0, 0.05f, 0), new Vector3(w, 0.1f, d), Concrete(Tile(w, d, 4f)), 0, true, t);
            System.Func<char, bool> Has = c => System.Array.IndexOf(doors, c) >= 0;
            Wall(t, new Vector3(-w / 2, 0, -d / 2), 0, w, h, Openings(w, Has('S'), true), wall, 0.3f, ruined);
            Wall(t, new Vector3(w / 2, 0, d / 2), 180, w, h, Openings(w, Has('N'), true), wall, 0.3f, ruined);
            Wall(t, new Vector3(-w / 2, 0, d / 2), 90, d, h, Openings(d, Has('W'), true), wall, 0.3f, ruined);
            Wall(t, new Vector3(w / 2, 0, -d / 2), -90, d, h, Openings(d, Has('E'), true), wall, 0.3f, ruined);
            if (!ruined) Box(new Vector3(0, h + 0.12f, 0), new Vector3(w + 0.6f, 0.25f, d + 0.6f), Concrete(Tile(w, d, 4f)), 0, true, t);
            Box(new Vector3(w * 0.22f, 0.4f, d * 0.2f), new Vector3(1.6f, 0.8f, 0.8f), Wood(Vector2.one), 0, true, t);
            Box(new Vector3(-w * 0.28f, 0.5f, -d * 0.15f), new Vector3(1f, 1f, 1f), Wood(Vector2.one), 25f, true, t);
            return t;
        }

        /// <summary>Extra storeys piled on a roof, slightly offset, like the stacked market towers in the concept art.</summary>
        static void StackedFloors(Transform building, float w, float d, float h, int floors)
        {
            Color[] tints = { new Color(0.74f, 0.66f, 0.55f), new Color(0.6f, 0.52f, 0.44f), new Color(0.45f, 0.27f, 0.18f), new Color(0.5f, 0.55f, 0.5f) };
            float y = h + 0.25f;
            for (int i = 0; i < floors; i++)
            {
                float fw = w * R(0.7f, 1.05f), fd = d * R(0.7f, 1.05f), fh = R(2.4f, 3.2f);
                Material m = i % 2 == 0 ? Plaster(Tile(fw, fh, 3f)) : Metal(tints[rng.Next(tints.Length)]);
                Box(new Vector3(R(-0.8f, 0.8f), y + fh / 2f, R(-0.8f, 0.8f)), new Vector3(fw, fh, fd), m, R(-4f, 4f), true, building);
                // a few windows lit from inside
                if (rng.NextDouble() < 0.7)
                    Box(new Vector3(R(-fw / 3f, fw / 3f), y + fh * 0.55f, -fd / 2f - 0.02f), new Vector3(0.9f, 0.7f, 0.04f), Lantern, 0, false, building);
                y += fh;
            }
        }

        static void Sandbags(Vector3 pos, float length, float rotY)
        {
            Transform t = Group("Sandbags", pos, rotY);
            Box(new Vector3(0, 0.4f, 0), new Vector3(length, 0.8f, 0.9f), Sandbag(new Vector2(length / 1.2f, 1.2f)), 0, true, t);
            Box(new Vector3(0, 1.0f, 0), new Vector3(length - 0.5f, 0.4f, 0.75f), Sandbag(new Vector2((length - 0.5f) / 1.2f, 0.6f)), 0, true, t);
        }

        static void Barrier(Vector3 pos, float rotY)
        {
            Transform t = Group("Barrier", pos, rotY);
            for (int i = -1; i <= 1; i++)
            {
                Box(new Vector3(i * 2.05f, 0.25f, 0), new Vector3(2f, 0.5f, 0.75f), Concrete(Vector2.one), 0, true, t);
                Box(new Vector3(i * 2.05f, 0.75f, 0), new Vector3(2f, 0.55f, 0.4f), Concrete(Vector2.one), 0, true, t);
            }
        }

        static void Container(Vector3 pos, float rotY, Color c, float yOffset = 0f, Transform parent = null)
        {
            Transform t = parent != null ? parent : Group("Container", pos, rotY);
            Vector3 o = parent != null ? pos : Vector3.zero;
            float r = parent != null ? rotY : 0f;
            Box(o + new Vector3(0, 1.3f + yOffset, 0), new Vector3(6.1f, 2.6f, 2.45f), Metal(c), r, true, t);
        }

        static void Crates(Vector3 pos, int count)
        {
            Transform t = Group("Crates", pos, R(0, 90));
            for (int i = 0; i < count; i++)
            {
                float s = R(0.9f, 1.25f);
                Vector3 p = new Vector3((i % 2) * 1.35f, s / 2f, (i / 2) * 1.35f);
                Box(p, Vector3.one * s, Wood(Vector2.one), R(-8, 8), true, t);
                if (rng.NextDouble() < 0.35) Box(p + Vector3.up * (s / 2f + 0.4f), Vector3.one * 0.8f, Wood(Vector2.one), R(0, 40), true, t);
            }
        }

        static void Barrels(Vector3 pos, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Color c = i % 3 == 0 ? new Color(0.3f, 0.36f, 0.22f) : i % 3 == 1 ? new Color(0.2f, 0.28f, 0.4f) : new Color(0.45f, 0.22f, 0.14f);
                Cyl(pos + new Vector3(R(-1.5f, 1.5f), 0, R(-1.5f, 1.5f)), 0.3f, 0.9f, Metal(c));
            }
        }

        static void Wreck(Vector3 pos, float rotY)
        {
            Transform t = Group("Burnt vehicle", pos, rotY);
            Box(new Vector3(0, 0.75f, 0), new Vector3(4.4f, 0.9f, 1.9f), Burnt, 0, true, t);
            Box(new Vector3(-0.4f, 1.45f, 0), new Vector3(2.2f, 0.6f, 1.7f), Burnt, 0, true, t);
            foreach (var w in new[] { new Vector3(1.4f, 0.4f, 0.95f), new Vector3(1.4f, 0.4f, -0.95f), new Vector3(-1.4f, 0.4f, 0.95f), new Vector3(-1.4f, 0.4f, -0.95f) })
            {
                GameObject wheel = Cyl(Vector3.zero, 0.4f, 0.3f, MaterialLibrary.Lit("tyre", new Color(0.08f, 0.08f, 0.08f)), false, t);
                wheel.transform.localPosition = w;
                wheel.transform.localRotation = Quaternion.Euler(90, 0, 0);
            }
        }

        static void Rocks(Vector3 pos)
        {
            Transform t = Group("Rocks", pos, R(0, 360));
            int n = 1 + rng.Next(3);
            for (int i = 0; i < n; i++)
            {
                GameObject g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                g.transform.SetParent(t, false);
                float s = R(0.6f, 1.6f);
                g.transform.localPosition = new Vector3(R(-1, 1), s * 0.25f, R(-1, 1));
                g.transform.localScale = new Vector3(s * R(1f, 1.6f), s * R(0.5f, 0.8f), s * R(0.9f, 1.4f));
                g.transform.localRotation = Quaternion.Euler(R(-10, 10), R(0, 360), R(-10, 10));
                g.GetComponent<Renderer>().sharedMaterial = Rock(Vector2.one);
            }
        }

        /// <summary>Canyon walls: a solid boundary on every side, roughened with uneven rock blocks above and outside it.</summary>
        static void Cliffs(float half)
        {
            Material rock = Rock(new Vector2(12, 3));
            const float H = 11f, T = 4f;
            Box(new Vector3(0, H / 2f, half + T / 2f), new Vector3(half * 2 + T * 2, H, T), rock);
            Box(new Vector3(0, H / 2f, -half - T / 2f), new Vector3(half * 2 + T * 2, H, T), rock);
            Box(new Vector3(half + T / 2f, H / 2f, 0), new Vector3(T, H, half * 2), rock);
            Box(new Vector3(-half - T / 2f, H / 2f, 0), new Vector3(T, H, half * 2), rock);

            Material rough = Rock(new Vector2(3, 3));
            for (int side = 0; side < 4; side++)
            {
                for (float s = -half; s < half; s += R(9f, 14f))
                {
                    float height = R(10f, 22f), depth = R(8f, 14f), len = R(10f, 18f);
                    float offset = half + T + depth * 0.5f - R(1f, 3f);
                    Vector3 p = side == 0 ? new Vector3(s, height / 2f, offset) : side == 1 ? new Vector3(s, height / 2f, -offset)
                              : side == 2 ? new Vector3(offset, height / 2f, s) : new Vector3(-offset, height / 2f, s);
                    Vector3 size = side < 2 ? new Vector3(len, height, depth) : new Vector3(depth, height, len);
                    Scenery(p, size, rough, R(-8f, 8f));
                }
            }
            // a few boulders fallen from the cliffs, as cover along the edges
            for (int i = 0; i < 10; i++)
            {
                float along = R(-half + 8f, half - 8f);
                bool northSouth = i % 2 == 0;
                float edge = (i % 4 < 2 ? 1 : -1) * (half - R(1.5f, 3f));
                Vector3 p = northSouth ? new Vector3(along, 0, edge) : new Vector3(edge, 0, along);
                if (Mathf.Abs(p.x) < 24f && Mathf.Abs(p.z) > 50f) continue; // keep the bases clear
                if (Mathf.Abs(p.x - RiverX) < 6f) continue;
                Box(p + Vector3.up * 1.1f, new Vector3(R(2f, 4f), R(1.6f, 2.6f), R(2f, 4f)), rough, R(0, 90));
            }
        }

        /// <summary>The dirt road that winds from the Blue base past the market and the Hub to the Red base, with branches to the bridges.</summary>
        static void OuterRoad()
        {
            Material dirt = RoadDirt(new Vector2(2, 10));
            Vector3[] main = { new Vector3(0, 0, -64), new Vector3(-6, 0, -42), new Vector3(-16, 0, -22), new Vector3(-21, 0, 2), new Vector3(-15, 0, 26), new Vector3(-5, 0, 44), new Vector3(0, 0, 64) };
            for (int i = 0; i < main.Length - 1; i++) Strip(main[i], main[i + 1], 6f, dirt, 0.012f);
            // east branches to the two bridges
            Strip(new Vector3(-6, 0, -42), new Vector3(22, 0, -22), 5f, dirt, 0.014f);
            Strip(new Vector3(22, 0, -22), new Vector3(43, 0, BridgeZ[0]), 5f, dirt, 0.014f);
            Strip(new Vector3(-15, 0, 26), new Vector3(20, 0, 30), 5f, dirt, 0.014f);
            Strip(new Vector3(20, 0, 30), new Vector3(43, 0, BridgeZ[1]), 5f, dirt, 0.014f);
            Strip(new Vector3(51, 0, BridgeZ[0]), new Vector3(58, 0, -5), 4.5f, dirt, 0.014f);
            Strip(new Vector3(51, 0, BridgeZ[1]), new Vector3(58, 0, 5), 4.5f, dirt, 0.014f);
        }

        /// <summary>A narrow lantern-lit street of stacked stalls and shops under the Syndicate's shark-head sign.</summary>
        static void MarketDistrict()
        {
            const float streetX = -43f;
            // west row (doors east, onto the street) and east row (doors west); the gap at z 0..9 is the plaza around A
            float[] westZ = { -22f, -11f, 14f, 25f };
            float[] eastZ = { -22f, -11f, 14f };
            foreach (float z in westZ)
            {
                Transform b = Building(new Vector3(streetX - 8.5f, 0, z), 10f, 9f, 3.3f, new[] { 'E', 'W' }, false);
                StackedFloors(b, 10f, 9f, 3.3f, 1 + rng.Next(3));
            }
            foreach (float z in eastZ)
            {
                Transform b = Building(new Vector3(streetX + 8.5f, 0, z), 10f, 9f, 3.3f, new[] { 'W', 'E' }, false);
                StackedFloors(b, 10f, 9f, 3.3f, 1 + rng.Next(2));
            }
            // a collapsed shop on the plaza's east side gives cover without closing the plaza
            Building(new Vector3(streetX + 8.5f, 0, 3f), 8f, 7f, 2.8f, new[] { 'W', 'N', 'S' }, true);

            // stalls with awnings (awnings have no collider so they never block walking or navigation)
            Color[] awning = { new Color(0.65f, 0.16f, 0.12f), new Color(0.2f, 0.42f, 0.3f), new Color(0.22f, 0.32f, 0.5f), new Color(0.7f, 0.5f, 0.18f) };
            for (int i = 0; i < 9; i++)
            {
                float z = -26f + i * 6.5f;
                if (z > -1f && z < 8f) continue; // keep the plaza open
                float side = i % 2 == 0 ? -1f : 1f;
                Vector3 p = new Vector3(streetX + side * 2.1f, 0, z + R(-1f, 1f));
                Box(p + Vector3.up * 0.45f, new Vector3(1.0f, 0.9f, 2.2f), Wood(Vector2.one), 0, true);
                Box(p + new Vector3(side * 0.3f, 2.5f, 0), new Vector3(2.0f, 0.06f, 2.8f), Metal(awning[i % awning.Length]), 0, false);
            }
            // plaza: a few crates and barrels to fight around
            Crates(new Vector3(streetX - 2f, 0, 1f), 3);
            Barrels(new Vector3(streetX + 2.5f, 0, 6.5f), 3);
            Sandbags(new Vector3(streetX, 0, -4.5f), 3.5f, 0f);

            // red lanterns strung over the street
            for (float z = -26f; z <= 30f; z += 4f)
                Box(new Vector3(streetX + R(-1.5f, 1.5f), 3.1f, z), new Vector3(0.35f, 0.45f, 0.35f), Lantern, 0, false);

            // the Syndicate's shark-head sign, hung high on the west row facing the street
            Transform shark = Group("Shark sign", new Vector3(streetX - 3.2f, 7.5f, 14f), 90f);
            Material skin = Metal(new Color(0.55f, 0.58f, 0.6f));
            Material mouth = MaterialLibrary.Lit("sharkmouth", new Color(0.45f, 0.08f, 0.08f));
            Material teeth = MaterialLibrary.Lit("sharkteeth", new Color(0.92f, 0.9f, 0.84f));
            Box(new Vector3(0, 0.6f, 0.4f), new Vector3(3.6f, 1.3f, 3.2f), skin, 0, false, shark);       // upper head
            Box(new Vector3(0, -0.9f, 0.2f), new Vector3(3.2f, 0.9f, 2.8f), skin, 0, false, shark);      // lower jaw
            Box(new Vector3(0, -0.15f, 1.0f), new Vector3(3.0f, 0.7f, 1.4f), mouth, 0, false, shark);    // open mouth
            for (int i = -3; i <= 3; i++)
            {
                Box(new Vector3(i * 0.42f, 0.1f, 1.75f), new Vector3(0.18f, 0.32f, 0.08f), teeth, 0, false, shark);
                Box(new Vector3(i * 0.42f, -0.42f, 1.55f), new Vector3(0.18f, 0.28f, 0.08f), teeth, 0, false, shark);
            }
            Box(new Vector3(1.2f, 0.9f, 1.75f), new Vector3(0.25f, 0.25f, 0.05f), mouth, 0, false, shark); // eyes
            Box(new Vector3(-1.2f, 0.9f, 1.75f), new Vector3(0.25f, 0.25f, 0.05f), mouth, 0, false, shark);
            foreach (Renderer r in shark.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        /// <summary>Concrete tunnel through a rock spur south of the market: the Undercity entrance and a flank route from the Blue side.</summary>
        static void UndercityTunnel()
        {
            Material rock = Rock(new Vector2(4, 2));
            Material conc = Concrete(new Vector2(4, 2));
            const float x = -43f, z0 = -46f, z1 = -31f, halfW = 2.2f, h = 3.6f;
            float len = z1 - z0, cz = (z0 + z1) * 0.5f;
            // rock spur either side of the tunnel, closing the market's south end except through the tunnel
            Box(new Vector3(-58f, 4f, cz), new Vector3(24f, 8f, len), rock);
            Box(new Vector3(-34.5f, 4f, cz), new Vector3(13f, 8f, len), rock);
            // tunnel walls and roof
            Box(new Vector3(x - halfW - 0.2f, h / 2f, cz), new Vector3(0.4f, h, len), conc);
            Box(new Vector3(x + halfW + 0.2f, h / 2f, cz), new Vector3(0.4f, h, len), conc);
            Box(new Vector3(x, h + 2.2f, cz), new Vector3(halfW * 2f + 0.8f, 4.4f, len), rock);
            // portal frames
            foreach (float pz in new[] { z0, z1 })
                Box(new Vector3(x, h + 0.25f, pz), new Vector3(halfW * 2f + 1.2f, 0.5f, 0.6f), conc);
            // cold tube lights and pipes along the walls
            for (float z = z0 + 2f; z < z1 - 1f; z += 3.5f)
                Box(new Vector3(x - halfW + 0.05f, 2.6f, z), new Vector3(0.06f, 0.08f, 1.2f), TunnelLight, 0, false);
            Cyl(Vector3.zero, 0.12f, len, Rust, false).transform.SetPositionAndRotation(new Vector3(x + halfW - 0.2f, 2.9f, cz), Quaternion.Euler(90, 0, 0));
            Cyl(Vector3.zero, 0.08f, len, Metal(new Color(0.4f, 0.45f, 0.42f)), false).transform.SetPositionAndRotation(new Vector3(x + halfW - 0.2f, 2.5f, cz), Quaternion.Euler(90, 0, 0));
            Crates(new Vector3(x + 0.6f, 0, -40f), 2);
        }

        /// <summary>The Hub: a settlement built in and around a crashed sky-freighter. Point B is inside the open stern half.</summary>
        static void TheHub()
        {
            const float L = 34f, W = 11f, H = 5.5f;
            Transform hull = Group("Crashed freighter", new Vector3(2, 0, 0), 25f);
            Material m = Hull;
            Box(new Vector3(0, 0.05f, 0), new Vector3(L, 0.1f, W), Metal(new Color(0.35f, 0.36f, 0.36f)), 0, true, hull); // deck

            var south = new List<Opening>
            {
                new Opening { start = 6f, end = 9.5f, bottom = 0f, top = 3.4f },
                new Opening { start = 21f, end = 24.5f, bottom = 0f, top = 3.4f },
                new Opening { start = 13f, end = 15f, bottom = 1.3f, top = 2.4f },
            };
            var north = new List<Opening>
            {
                new Opening { start = 10f, end = 13.5f, bottom = 0f, top = 3.4f },
                new Opening { start = 24f, end = 26f, bottom = 1.3f, top = 2.4f },
            };
            Wall(hull, new Vector3(-L / 2f, 0, -W / 2f), 0, L, H, south, m, 0.45f);
            Wall(hull, new Vector3(L / 2f, 0, W / 2f), 180, L, H, north, m, 0.45f);
            // bow closed, stern torn open (the tail broke away in the crash)
            Wall(hull, new Vector3(L / 2f, 0, -W / 2f), -90, W, H, new List<Opening>(), m, 0.45f);
            Wall(hull, new Vector3(-L / 2f, 0, W / 2f), 90, W, H, new List<Opening> { new Opening { start = 2f, end = 9f, bottom = 0f, top = H } }, m, 0.45f, true);

            // roof plating over the bow half; bare ribs over the stern half, open to the sky so point B's flag shows
            Box(new Vector3(10f, H + 0.15f, 0), new Vector3(14f, 0.3f, W + 0.4f), m, 0, true, hull);
            for (float x = -15f; x < 2f; x += 3.2f)
                Box(new Vector3(x, H + 0.15f, 0), new Vector3(0.35f, 0.3f, W + 0.4f), Rust, 0, true, hull);
            // bridge tower on the bow, a nose cone and a raised fin
            Box(new Vector3(12f, H + 2.3f, 0), new Vector3(4f, 4f, 5f), m, 0, true, hull);
            Box(new Vector3(12f, H + 3f, -2.55f), new Vector3(3.2f, 0.8f, 0.05f), Lantern, 0, false, hull);
            Box(new Vector3(L / 2f + 2f, 2.6f, 0), new Vector3(4.2f, 5.2f, 8.5f), m, 0, true, hull);
            Box(new Vector3(-6f, H + 2.5f, 0), new Vector3(9f, 5f, 0.5f), m, 0, false, hull);
            Cyl(new Vector3(12f, H + 4.3f, 0), 0.08f, 6f, Metal(new Color(0.3f, 0.3f, 0.3f)), false, hull);
            Box(new Vector3(12.7f, H + 9.2f, 0), new Vector3(1.4f, 0.9f, 0.04f), MaterialLibrary.Lit("syndicateflag", new Color(0.62f, 0.12f, 0.1f)), 0, false, hull);

            // cover inside the hold
            Container(new Vector3(6f, 0, 2.6f), 0f, new Color(0.42f, 0.3f, 0.2f), 0f, hull);
            Container(new Vector3(-10f, 0, -2.8f), 0f, new Color(0.22f, 0.32f, 0.38f), 0f, hull);
            Box(new Vector3(-3f, 0.6f, 2.5f), new Vector3(2.4f, 1.2f, 1.2f), Wood(Vector2.one), 10f, true, hull);
            Box(new Vector3(1f, 0.5f, -3f), new Vector3(1.4f, 1f, 1.4f), Wood(Vector2.one), -20f, true, hull);

            // shacks and scaffolding around the wreck
            Transform s1 = Building(new Vector3(-8f, 0, 19f), 6f, 5f, 3f, new[] { 'S', 'E' }, false, Rust);
            StackedFloors(s1, 6f, 5f, 3f, 1);
            Building(new Vector3(28f, 0, -6f), 6f, 6f, 3f, new[] { 'W', 'N' }, false, Rust);
            Building(new Vector3(-10f, 0, -16f), 5f, 5f, 2.8f, new[] { 'E', 'N' }, true);
            Transform scaffold = Group("Scaffold", new Vector3(16f, 0, 12f));
            Material pipe = Rust;
            for (int i = 0; i < 4; i++)
                Cyl(new Vector3(i % 2 == 0 ? -1.5f : 1.5f, 0, i < 2 ? -1.5f : 1.5f), 0.1f, 9f, pipe, true, scaffold);
            Box(new Vector3(0, 4.5f, 0), new Vector3(3.6f, 0.15f, 3.6f), Wood(Vector2.one), 0, true, scaffold);
            Box(new Vector3(0, 9f, 0), new Vector3(3.6f, 0.15f, 3.6f), Wood(Vector2.one), 0, true, scaffold);
            Sandbags(new Vector3(-6f, 0, -14f), 4f, 25f);
            Sandbags(new Vector3(12f, 0, 14f), 4f, 25f);
            Barrels(new Vector3(-4f, 0, 16f), 4);
        }

        /// <summary>The river channel between chest-high embankments, crossed by two bridges. It splits the Watchtower from the rest of the canyon.</summary>
        static void River()
        {
            float half = 70f;
            float inner = RiverHalfWidth, wallT = 0.4f;
            Material conc = Concrete(new Vector2(20, 1));
            // water surface (decorative; the embankments keep everyone out of the channel)
            Box(new Vector3(RiverX, 0.04f, 0), new Vector3(inner * 2f, 0.02f, half * 2f), Water, 0, false);

            // embankments, broken where the bridges cross
            var cuts = new List<float> { -half };
            foreach (float bz in BridgeZ) { cuts.Add(bz - BridgeHalfWidth); cuts.Add(bz + BridgeHalfWidth); }
            cuts.Add(half);
            for (int i = 0; i + 1 < cuts.Count; i += 2)
            {
                float z0 = cuts[i], z1 = cuts[i + 1], cz = (z0 + z1) * 0.5f, len = z1 - z0;
                foreach (float side in new[] { -1f, 1f })
                    Box(new Vector3(RiverX + side * (inner + wallT / 2f), EmbankmentHeight / 2f, cz), new Vector3(wallT, EmbankmentHeight, len), conc);
            }

            // bridges: a deck over the channel with railings on both edges so nobody can step down into the water
            foreach (float bz in BridgeZ)
            {
                Transform b = Group("Bridge", new Vector3(RiverX, 0, bz));
                float span = inner * 2f + wallT * 2f + 3f;
                Box(new Vector3(0, 0.1f, 0), new Vector3(span, 0.2f, BridgeHalfWidth * 2f), Concrete(new Vector2(3, 2)), 0, true, b);
                foreach (float side in new[] { -1f, 1f })
                    Box(new Vector3(0, 0.2f + 0.75f, side * (BridgeHalfWidth - 0.05f)), new Vector3(span, 1.5f, 0.1f), Rust, 0, true, b);
                Box(new Vector3(0, -0.5f, 0), new Vector3(1.2f, 1f, BridgeHalfWidth * 2f - 0.4f), Concrete(Vector2.one), 0, false, b); // pier
            }
            // sandbag nests guarding each bridge from the east bank
            Sandbags(new Vector3(RiverX + 7f, 0, BridgeZ[0] + 4.5f), 3.5f, 90f);
            Sandbags(new Vector3(RiverX + 7f, 0, BridgeZ[1] - 4.5f), 3.5f, 90f);
        }

        /// <summary>The sandbagged Watchtower on the east bank (point C): a tower on four legs, a radio mast and a bunker.</summary>
        static void Watchtower()
        {
            Transform t = Group("Watchtower", new Vector3(62f, 0, 4f)); // beside point C, clear of its flagpole
            Material legs = Rust;
            const float deck = 7f;
            foreach (var p in new[] { new Vector3(-1.8f, 0, -1.8f), new Vector3(1.8f, 0, -1.8f), new Vector3(-1.8f, 0, 1.8f), new Vector3(1.8f, 0, 1.8f) })
                Cyl(p, 0.2f, deck, legs, true, t);
            // cross bracing (no collider: thin enough to shoot through)
            Box(new Vector3(0, 3f, -1.8f), new Vector3(3.8f, 0.12f, 0.12f), legs, 0, false, t).transform.localRotation = Quaternion.Euler(0, 0, 40f);
            Box(new Vector3(0, 3f, 1.8f), new Vector3(3.8f, 0.12f, 0.12f), legs, 0, false, t).transform.localRotation = Quaternion.Euler(0, 0, -40f);
            Box(new Vector3(0, deck, 0), new Vector3(5f, 0.3f, 5f), Wood(new Vector2(2, 2)), 0, true, t);
            // cabin with a window band, roof and the radio mast
            foreach (float s in new[] { -1f, 1f })
            {
                Box(new Vector3(s * 2.3f, deck + 0.6f, 0), new Vector3(0.2f, 1.0f, 4.8f), Sandbag(new Vector2(4, 1)), 0, true, t);
                Box(new Vector3(0, deck + 0.6f, s * 2.3f), new Vector3(4.8f, 1.0f, 0.2f), Sandbag(new Vector2(4, 1)), 0, true, t);
                Cyl(new Vector3(s * 2.2f, deck, s * 2.2f), 0.08f, 2.6f, legs, false, t);
                Cyl(new Vector3(s * 2.2f, deck, -s * 2.2f), 0.08f, 2.6f, legs, false, t);
            }
            Box(new Vector3(0, deck + 2.7f, 0), new Vector3(5.4f, 0.2f, 5.4f), Rust, 0, true, t);
            Cyl(new Vector3(1.6f, deck + 2.8f, 1.6f), 0.07f, 6f, Metal(new Color(0.3f, 0.3f, 0.3f)), false, t);
            Box(new Vector3(2.3f, deck + 8f, 1.6f), new Vector3(1.3f, 0.8f, 0.04f), MaterialLibrary.Lit("syndicateflag", new Color(0.62f, 0.12f, 0.1f)), 0, false, t);

            // ground defences around the tower
            Sandbags(new Vector3(56f, 0, 8f), 4f, 0f);
            Sandbags(new Vector3(56f, 0, -8f), 4f, 0f);
            Sandbags(new Vector3(66f, 0, -2f), 4f, 90f);
            Building(new Vector3(63f, 0, -12f), 7f, 5f, 2.8f, new[] { 'N', 'W' }, false, Concrete(new Vector2(2, 1)));
            Container(new Vector3(64f, 0, 20f), 90f, new Color(0.5f, 0.2f, 0.15f));
            Crates(new Vector3(55f, 0, 22f), 3);
        }

        /// <summary>Wrecks, barriers and ruins along the Outer Road between the landmarks.</summary>
        static void RoadsideCover()
        {
            Wreck(new Vector3(-9f, 0, -36f), 70f);
            Wreck(new Vector3(-23f, 0, 10f), -10f);
            Wreck(new Vector3(-10f, 0, 36f), 40f);
            Wreck(new Vector3(30f, 0, -18f), 15f);
            Barrier(new Vector3(8f, 0, -30f), 20f);
            Barrier(new Vector3(28f, 0, 30f), 0f);
            Barrier(new Vector3(-26f, 0, -14f), 90f);
            Container(new Vector3(26f, 0, 4f), 80f, new Color(0.45f, 0.2f, 0.14f));
            Container(new Vector3(28f, 0, 44f), 10f, new Color(0.3f, 0.38f, 0.22f));
            Container(new Vector3(-28f, 0, 42f), 100f, new Color(0.22f, 0.32f, 0.45f));
            Building(new Vector3(30f, 0, -36f), 7f, 6f, 3f, new[] { 'N', 'W' }, true);
            Building(new Vector3(-30f, 0, 46f), 6f, 6f, 3f, new[] { 'S', 'E' }, true);
            Crates(new Vector3(14f, 0, 38f), 3);
            Barrels(new Vector3(-4f, 0, -20f), 4);
        }

        static void Base(List<Vector3> spawns, int side)
        {
            float z = side * 60f;
            Group(side < 0 ? "Blue base" : "Red base", new Vector3(0, 0, z));
            Sandbags(new Vector3(-12, 0, z - side * 7), 6, 0);
            Sandbags(new Vector3(12, 0, z - side * 7), 6, 0);
            Barrier(new Vector3(-6.5f, 0, z - side * 10), 0);
            Barrier(new Vector3(6.5f, 0, z - side * 10), 0);
            Container(new Vector3(-20, 0, z), 90f, side < 0 ? new Color(0.22f, 0.32f, 0.45f) : new Color(0.5f, 0.2f, 0.15f));
            Container(new Vector3(20, 0, z), 90f, side < 0 ? new Color(0.22f, 0.32f, 0.45f) : new Color(0.5f, 0.2f, 0.15f));
            for (int i = 0; i < 8; i++) spawns.Add(new Vector3(-10.5f + i * 3f, 0.1f, z + side * 3f));
            Transform tent = Group("Tent", new Vector3(10, 0, z + side * 6));
            Box(new Vector3(0, 1.2f, 0), new Vector3(5, 0.1f, 3.4f), MaterialLibrary.Lit("canvas", new Color(0.5f, 0.42f, 0.3f), MaterialLibrary.Tex("camo"), Vector2.one), 0, false, tent);
            foreach (var p in new[] { new Vector3(-2.4f, 0, -1.6f), new Vector3(2.4f, 0, -1.6f), new Vector3(-2.4f, 0, 1.6f), new Vector3(2.4f, 0, 1.6f) })
                Cyl(p, 0.05f, 1.2f, MaterialLibrary.Lit("pole", new Color(0.35f, 0.35f, 0.33f), null, null, 0.5f, 0.6f), true, tent);
        }

        /// <summary>Mesas beyond the canyon walls and the sealed towers of the Orbital City on the north-east horizon.</summary>
        static void Outskirts(float half)
        {
            Material mesa = Rock(new Vector2(6, 4));
            for (int i = 0; i < 26; i++)
            {
                float a = R(0, Mathf.PI * 2), d = R(half + 35, half + 110);
                var p = new Vector3(Mathf.Cos(a) * d, 0, Mathf.Sin(a) * d);
                if (p.x > 60f && p.z > 60f) continue; // leave the city view open
                float h = R(12, 40);
                Scenery(p + Vector3.up * h / 2f, new Vector3(R(14, 40), h, R(14, 40)), mesa, R(0, 90));
            }

            Material tower = MaterialLibrary.Emissive("orbitaltower", new Color(0.6f, 0.66f, 0.74f), new Color(0.05f, 0.08f, 0.12f));
            Material glow = MaterialLibrary.Emissive("orbitalglow", new Color(0.8f, 0.9f, 1f), new Color(0.6f, 0.75f, 1f));
            var city = new Vector3(118f, 0, 130f);
            for (int i = 0; i < 24; i++)
            {
                Vector3 p = city + new Vector3(R(-45f, 45f), 0, R(-30f, 30f));
                float h = R(35f, 120f), w = R(6f, 14f);
                Scenery(p + Vector3.up * h / 2f, new Vector3(w, h, w), tower, R(0, 90));
                if (rng.NextDouble() < 0.5) Scenery(p + Vector3.up * (h + 1f), new Vector3(w * 0.4f, 2f, w * 0.4f), glow);
            }
            Scenery(city + new Vector3(0, 60f, 0), new Vector3(100f, 3f, 6f), tower, 30f); // sky bridge
        }

        static CapturePoint Point(string label, string placeName, Vector3 pos)
        {
            var go = new GameObject("CapturePoint_" + label);
            go.transform.SetParent(root, false);
            go.transform.localPosition = pos;
            CapturePoint cp = go.AddComponent<CapturePoint>();
            cp.Setup(label, placeName);
            return cp;
        }
    }
}
