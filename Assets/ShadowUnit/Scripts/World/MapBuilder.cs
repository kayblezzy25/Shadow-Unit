using System.Collections.Generic;
using UnityEngine;

namespace ShadowUnit
{
    /// <summary>Everything the match needs to know about the built map.</summary>
    public class MapInfo
    {
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
    /// Builds the original "Kessel Crossing" battlefield from modular primitives: roads, enterable buildings,
    /// ruins, cover, team bases and three capture points. Placeholder geometry by design (see README).
    /// </summary>
    public static class MapBuilder
    {
        static Transform root;
        static System.Random rng;

        static Material Ground => MaterialLibrary.Lit("ground", new Color(0.52f, 0.47f, 0.37f), MaterialLibrary.Tex("dirt"), new Vector2(40, 40), 0.05f);
        static Material Asphalt(Vector2 t) => MaterialLibrary.Lit("asphalt", new Color(0.25f, 0.25f, 0.26f), MaterialLibrary.Tex("asphalt"), t, 0.2f);
        static Material Concrete(Vector2 t) => MaterialLibrary.Lit("concrete", new Color(0.66f, 0.64f, 0.6f), MaterialLibrary.Tex("concrete"), t, 0.1f);
        static Material Plaster(Vector2 t) => MaterialLibrary.Lit("plaster", new Color(0.74f, 0.69f, 0.6f), MaterialLibrary.Tex("concrete"), t, 0.08f);
        static Material Sandbag(Vector2 t) => MaterialLibrary.Lit("sandbag", new Color(0.66f, 0.58f, 0.43f), MaterialLibrary.Tex("sandbag"), t, 0.02f);
        static Material Wood(Vector2 t) => MaterialLibrary.Lit("wood", new Color(0.55f, 0.42f, 0.28f), MaterialLibrary.Tex("wood"), t, 0.1f);
        static Material Metal(Color c) => MaterialLibrary.Lit("metal" + ColorUtility.ToHtmlStringRGB(c), c, MaterialLibrary.Tex("metal"), new Vector2(2, 1), 0.35f, 0.4f);
        static Material Rubble => MaterialLibrary.Lit("rubble", new Color(0.55f, 0.53f, 0.5f), MaterialLibrary.Tex("concrete"), Vector2.one, 0.05f);
        static Material Burnt => MaterialLibrary.Lit("burnt", new Color(0.16f, 0.15f, 0.14f), MaterialLibrary.Tex("metal"), Vector2.one, 0.3f, 0.5f);

        static float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        static Vector2 Tile(float w, float h, float unit) => new Vector2(Mathf.Max(0.5f, Mathf.Round(w / unit * 2f) / 2f), Mathf.Max(0.5f, Mathf.Round(h / unit * 2f) / 2f));

        public static MapInfo Build()
        {
            rng = new System.Random(1984);
            var info = new MapInfo();
            root = new GameObject("Map_KesselCrossing").transform;
            info.root = root;

            Lighting(info);

            // ground and roads
            Box(new Vector3(0, -0.5f, 0), new Vector3(300, 1, 300), Ground);
            Box(new Vector3(0, 0.01f, 0), new Vector3(8, 0.02f, 150), Asphalt(new Vector2(2, 38)), 0, false);
            Box(new Vector3(0, 0.012f, -2), new Vector3(150, 0.02f, 7), Asphalt(new Vector2(38, 2)), 0, false);
            for (int i = -14; i <= 14; i++) Box(new Vector3(0, 0.025f, i * 5f), new Vector3(0.18f, 0.01f, 2.2f), MaterialLibrary.Lit("paint", new Color(0.82f, 0.78f, 0.6f)), 0, false);

            Perimeter(info.halfSize);

            // capture points: A west compound, B central plaza, C east warehouse yard
            info.points.Add(Point("A", new Vector3(-38, 0, -4)));
            info.points.Add(Point("B", new Vector3(0, 0, 6)));
            info.points.Add(Point("C", new Vector3(38, 0, -2)));

            // west compound around A
            Building(new Vector3(-46, 0, 9), 12, 9, 3.3f, new[] { 'S', 'E' }, false);
            Building(new Vector3(-28.5f, 0, -19), 8, 8, 3f, new[] { 'N', 'W' }, true);
            WallLine(new Vector3(-54, 0, -14), new Vector3(-54, 0, 2), 2.2f, true);
            Sandbags(new Vector3(-36, 0, 3.5f), 6f, 0f);
            Sandbags(new Vector3(-41, 0, -10.5f), 5f, 0f);
            Container(new Vector3(-27, 0, 4), 90f, new Color(0.45f, 0.2f, 0.14f));
            Crates(new Vector3(-44, 0, -6), 3);

            // central plaza B
            Building(new Vector3(-11, 0, 15), 9, 7, 3.1f, new[] { 'S', 'E', 'N' }, false);
            Building(new Vector3(12, 0, -11f), 10, 8, 3.2f, new[] { 'W', 'N' }, false);
            Ruin(new Vector3(9, 0, 19), 7, 6);
            Barrier(new Vector3(-4.5f, 0, 2), 90f);
            Barrier(new Vector3(4.5f, 0, 9), 90f);
            Barrier(new Vector3(0, 0, 13.5f), 0f);
            Wreck(new Vector3(2, 0, -24), 15f);
            Wreck(new Vector3(-2.5f, 0, 33), -160f);
            Sandbags(new Vector3(-6, 0, 8), 4f, 90f);

            // east warehouse yard around C
            Building(new Vector3(46, 0, 11), 16, 11, 4.6f, new[] { 'S', 'W' }, false, true);
            Container(new Vector3(30, 0, 8), 0f, new Color(0.17f, 0.3f, 0.45f));
            Container(new Vector3(30, 0, 8), 0f, new Color(0.3f, 0.38f, 0.22f), 2.6f);
            Container(new Vector3(44, 0, -14), 90f, new Color(0.55f, 0.42f, 0.16f));
            Container(new Vector3(26, 0, -12), 35f, new Color(0.42f, 0.42f, 0.44f));
            Crates(new Vector3(36, 0, 5), 4);
            Sandbags(new Vector3(39, 0, -8.5f), 5f, 0f);
            Barrels(new Vector3(48, 0, -5), 5);

            // scattered houses and ruins on the flanks
            Building(new Vector3(24, 0, -30), 7, 7, 3f, new[] { 'N', 'W' }, true);
            Building(new Vector3(-21, 0, 28), 7, 8, 3f, new[] { 'S', 'E' }, false);
            Ruin(new Vector3(30, 0, 30), 8, 6);
            Ruin(new Vector3(-34, 0, 34), 6, 7);
            Ruin(new Vector3(-30, 0, -36), 7, 6);
            Wreck(new Vector3(-58, 0, -2), 90f);
            Wreck(new Vector3(58, 0, -1), -80f);
            Physics.SyncTransforms();
            for (int i = 0, placed = 0; i < 80 && placed < 18; i++)
            {
                var p = new Vector3(R(-62, 62), 0, R(-44, 44));
                if (Physics.CheckSphere(p + Vector3.up * 3.8f, 3.5f, ~0, QueryTriggerInteraction.Ignore)) continue; // sphere stays above the ground slab
                Rocks(p);
                placed++;
            }

            // team bases
            Base(info.blueSpawns, -1);
            Base(info.redSpawns, 1);

            Outskirts(info.halfSize);

            info.graph = new WaypointGraph();
            info.graph.Build(new Vector2(-info.halfSize + 1.5f, -info.halfSize + 1.5f), new Vector2(info.halfSize - 1.5f, info.halfSize - 1.5f), 1.5f);
            return info;
        }

        // ---------- lighting ----------

        static void Lighting(MapInfo info)
        {
            var sunGo = new GameObject("Sun");
            sunGo.transform.SetParent(root);
            Light sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.9f, 0.76f);
            sun.intensity = 1.25f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.85f;
            sunGo.transform.rotation = Quaternion.Euler(38f, -35f, 0f);
            info.sun = sun;
            RenderSettings.sun = sun;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.6f, 0.68f);
            RenderSettings.ambientEquatorColor = new Color(0.46f, 0.44f, 0.4f);
            RenderSettings.ambientGroundColor = new Color(0.22f, 0.2f, 0.17f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.0105f;
            RenderSettings.fogColor = new Color(0.66f, 0.66f, 0.62f);
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
            if (door) list.Add(new Opening { start = doorCenter - doorWidth / 2f, end = doorCenter + doorWidth / 2f, bottom = 0f, top = Mathf.Min(2.4f, doorWidth > 3f ? 3.6f : 2.4f) });
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

        /// <summary>Enterable single-storey building. Doors on the listed sides (N/S/E/W).</summary>
        static void Building(Vector3 center, float w, float d, float h, char[] doors, bool ruined, bool warehouse = false)
        {
            Transform t = Group(ruined ? "Ruined building" : warehouse ? "Warehouse" : "Building", center);
            Material wall = warehouse ? Metal(new Color(0.48f, 0.5f, 0.47f)) : Plaster(Tile(w, h, 3f));
            Box(new Vector3(0, 0.05f, 0), new Vector3(w, 0.1f, d), Concrete(Tile(w, d, 4f)), 0, true, t);
            System.Func<char, bool> Has = c => System.Array.IndexOf(doors, c) >= 0;
            float dw = warehouse ? 4f : 2.2f;
            Wall(t, new Vector3(-w / 2, 0, -d / 2), 0, w, h, Openings(w, Has('S'), !warehouse, Has('S') ? dw : 2.2f), wall, 0.3f, ruined);
            Wall(t, new Vector3(w / 2, 0, d / 2), 180, w, h, Openings(w, Has('N'), !warehouse, Has('N') ? dw : 2.2f), wall, 0.3f, ruined);
            Wall(t, new Vector3(-w / 2, 0, d / 2), 90, d, h, Openings(d, Has('W'), true), wall, 0.3f, ruined);
            Wall(t, new Vector3(w / 2, 0, -d / 2), -90, d, h, Openings(d, Has('E'), true), wall, 0.3f, ruined);
            if (!ruined)
            {
                Box(new Vector3(0, h + 0.12f, 0), new Vector3(w + 0.6f, 0.25f, d + 0.6f), Concrete(Tile(w, d, 4f)), 0, true, t);
                Box(new Vector3(0, h + 0.35f, -d / 2 - 0.2f), new Vector3(w + 0.6f, 0.25f, 0.15f), Concrete(Vector2.one), 0, true, t);
            }
            else
            {
                Box(new Vector3(-w * 0.2f, h + 0.1f, d * 0.15f), new Vector3(w * 0.5f, 0.22f, d * 0.6f), Concrete(Vector2.one), 8f, true, t);
                for (int i = 0; i < 4; i++)
                    Box(new Vector3(R(-w / 3, w / 3), 0.25f, R(-d / 3, d / 3)), new Vector3(R(0.4f, 1.2f), R(0.2f, 0.5f), R(0.4f, 1f)), Rubble, R(0, 360), false, t);
            }
            // interior props
            if (warehouse)
            {
                for (int i = 0; i < 3; i++) Box(new Vector3(-w / 2 + 3 + i * 4.5f, 1.1f, d / 2 - 1.6f), new Vector3(3.2f, 2.2f, 1.2f), Wood(new Vector2(2, 1)), 0, true, t);
                Box(new Vector3(2, 0.6f, -1), new Vector3(1.2f, 1.2f, 1.2f), Wood(Vector2.one), 15f, true, t);
            }
            else
            {
                Box(new Vector3(w * 0.22f, 0.4f, d * 0.2f), new Vector3(1.6f, 0.8f, 0.8f), Wood(Vector2.one), 0, true, t);
                Box(new Vector3(-w * 0.28f, 0.5f, -d * 0.15f), new Vector3(1f, 1f, 1f), Wood(Vector2.one), 25f, true, t);
            }
        }

        static void Ruin(Vector3 center, float w, float d)
        {
            Transform t = Group("Ruin", center, R(0, 90));
            Material wall = Plaster(Tile(w, 3f, 3f));
            Wall(t, new Vector3(-w / 2, 0, -d / 2), 0, w, 2.8f, Openings(w, false, true), wall, 0.3f, true);
            Wall(t, new Vector3(-w / 2, 0, d / 2), 90, d * 0.6f, 2.4f, new List<Opening>(), wall, 0.3f, true);
            for (int i = 0; i < 6; i++)
                Box(new Vector3(R(-w / 2, w / 2), 0.2f, R(-d / 2, d / 2)), new Vector3(R(0.5f, 1.4f), R(0.25f, 0.6f), R(0.4f, 1.1f)), Rubble, R(0, 360), true, t);
        }

        static void WallLine(Vector3 a, Vector3 b, float h, bool ruined)
        {
            Vector3 d = b - a;
            float len = d.magnitude;
            float rot = -Mathf.Atan2(d.z, d.x) * Mathf.Rad2Deg;
            Transform t = Group("Wall line", a);
            Wall(t, Vector3.zero, rot, len, h, new List<Opening> { new Opening { start = len * 0.45f, end = len * 0.45f + 2.4f, bottom = 0, top = h } }, Concrete(Tile(len, h, 4f)), 0.4f, ruined);
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

        static void Container(Vector3 pos, float rotY, Color c, float yOffset = 0f)
        {
            Transform t = Group("Container", pos, rotY);
            Box(new Vector3(0, 1.3f + yOffset, 0), new Vector3(6.1f, 2.6f, 2.45f), Metal(c), 0, true, t);
            Box(new Vector3(3.06f, 1.3f + yOffset, 0), new Vector3(0.05f, 2.5f, 2.35f), Metal(c * 0.7f), 0, false, t);
            Box(new Vector3(3.09f, 1.3f + yOffset, 0.4f), new Vector3(0.04f, 2.2f, 0.06f), Metal(new Color(0.2f, 0.2f, 0.2f)), 0, false, t);
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
            Box(new Vector3(-0.2f, 1.9f, 0.1f), new Vector3(1.0f, 0.12f, 0.5f), Rubble, 20f, false, t);
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
                float s = R(0.5f, 1.4f);
                g.transform.localPosition = new Vector3(R(-1, 1), s * 0.25f, R(-1, 1));
                g.transform.localScale = new Vector3(s * R(1f, 1.6f), s * R(0.5f, 0.8f), s * R(0.9f, 1.4f));
                g.transform.localRotation = Quaternion.Euler(R(-10, 10), R(0, 360), R(-10, 10));
                g.GetComponent<Renderer>().sharedMaterial = Rubble;
            }
        }

        static void Perimeter(float half)
        {
            Material m = Concrete(new Vector2(36, 1));
            Box(new Vector3(0, 1.75f, half + 0.5f), new Vector3(half * 2 + 2, 3.5f, 1), m);
            Box(new Vector3(0, 1.75f, -half - 0.5f), new Vector3(half * 2 + 2, 3.5f, 1), m);
            Box(new Vector3(half + 0.5f, 1.75f, 0), new Vector3(1, 3.5f, half * 2), m);
            Box(new Vector3(-half - 0.5f, 1.75f, 0), new Vector3(1, 3.5f, half * 2), m);
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
            // tent
            Transform tent = Group("Tent", new Vector3(10, 0, z + side * 6));
            Box(new Vector3(0, 1.2f, 0), new Vector3(5, 0.1f, 3.4f), MaterialLibrary.Lit("canvas", new Color(0.38f, 0.4f, 0.3f), MaterialLibrary.Tex("camo"), Vector2.one), 0, false, tent).transform.localRotation = Quaternion.Euler(0, 0, 0);
            foreach (var p in new[] { new Vector3(-2.4f, 0, -1.6f), new Vector3(2.4f, 0, -1.6f), new Vector3(-2.4f, 0, 1.6f), new Vector3(2.4f, 0, 1.6f) })
                Cyl(p, 0.05f, 1.2f, MaterialLibrary.Lit("pole", new Color(0.35f, 0.35f, 0.33f), null, null, 0.5f, 0.6f), true, tent);
        }

        static void Outskirts(float half)
        {
            Material trunk = MaterialLibrary.Lit("trunk", new Color(0.3f, 0.23f, 0.17f), MaterialLibrary.Tex("wood"), Vector2.one);
            Material leaves = MaterialLibrary.Lit("leaves", new Color(0.24f, 0.31f, 0.18f), MaterialLibrary.Tex("camo"), new Vector2(2, 2), 0.02f);
            for (int i = 0; i < 70; i++)
            {
                float a = R(0, Mathf.PI * 2), d = R(half + 6, half + 60);
                var p = new Vector3(Mathf.Cos(a) * d, 0, Mathf.Sin(a) * d);
                if (Mathf.Abs(p.x) < half + 4 && Mathf.Abs(p.z) < half + 4) continue;
                float h = R(4, 9);
                Cyl(p, R(0.15f, 0.3f), h, trunk, false);
                GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Object.DestroyImmediate(crown.GetComponent<Collider>());
                crown.transform.SetParent(root, false);
                crown.transform.localPosition = p + Vector3.up * h;
                crown.transform.localScale = new Vector3(R(3, 5.5f), R(3, 5f), R(3, 5.5f));
                crown.GetComponent<Renderer>().sharedMaterial = leaves;
            }
        }

        static CapturePoint Point(string label, Vector3 pos)
        {
            var go = new GameObject("CapturePoint_" + label);
            go.transform.SetParent(root, false);
            go.transform.localPosition = pos;
            CapturePoint cp = go.AddComponent<CapturePoint>();
            cp.Setup(label);
            return cp;
        }
    }
}
