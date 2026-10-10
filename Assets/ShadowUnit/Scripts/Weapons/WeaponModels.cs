using UnityEngine;

namespace ShadowUnit
{
    /// <summary>
    /// Builds placeholder guns from primitives (no colliders). Swap for imported models in the art phase.
    /// Returns the root; the muzzle transform is named "Muzzle".
    /// Local axes: +Z forward, +Y up, metres. The sight line runs along y = SightHeight, which is where
    /// WeaponController puts the eye when aiming down sights, so every sight must be see-through there.
    /// </summary>
    public static class WeaponModels
    {
        public const float SightHeight = 0.056f;

        struct Mats { public Material metal, polymer, furniture, accent, reticle; }

        public static Transform Build(WeaponModelType type, Transform parent, float scale = 1f, bool woodFurniture = false)
        {
            switch (type)
            {
                case WeaponModelType.SMG: return BuildSmg(parent, scale);
                case WeaponModelType.DMR: return BuildDmr(parent, scale);
                default: return BuildRifle(parent, scale, woodFurniture);
            }
        }

        static Mats Materials(bool woodFurniture, Color? accentColor = null)
        {
            var m = new Mats();
            m.metal = MaterialLibrary.Lit("gunmetal", new Color(0.11f, 0.115f, 0.12f), MaterialLibrary.Tex("metal"), Vector2.one, 0.45f, 0.65f);
            m.polymer = MaterialLibrary.Lit("polymer", new Color(0.16f, 0.16f, 0.15f), null, null, 0.2f, 0f);
            m.furniture = woodFurniture ? MaterialLibrary.Lit("gunwood", new Color(0.4f, 0.24f, 0.13f), MaterialLibrary.Tex("wood"), Vector2.one, 0.3f) : m.polymer;
            m.accent = accentColor.HasValue ? MaterialLibrary.Lit("gunaccent" + ColorUtility.ToHtmlStringRGB(accentColor.Value), accentColor.Value, MaterialLibrary.Tex("metal"), Vector2.one, 0.35f, 0.3f) : m.polymer;
            m.reticle = MaterialLibrary.Emissive("reticle", new Color(1f, 0.15f, 0.1f), new Color(2.2f, 0.25f, 0.15f));
            return m;
        }

        /// <summary>VK-7 Carbine: olive furniture, angular stock, mid-length handguard.</summary>
        public static Transform BuildRifle(Transform parent, float scale = 1f, bool woodFurniture = false)
        {
            Transform root = Root("Rifle", parent);
            Mats m = Materials(woodFurniture, woodFurniture ? (Color?)null : new Color(0.27f, 0.3f, 0.2f));
            Material body = woodFurniture ? m.furniture : m.accent;

            Part(root, PrimitiveType.Cube, new Vector3(0, 0, 0), new Vector3(0.045f, 0.07f, 0.26f), m.metal);               // receiver
            Part(root, PrimitiveType.Cube, new Vector3(0, 0.005f, 0.22f), new Vector3(0.05f, 0.055f, 0.2f), m.metal);       // handguard
            Part(root, PrimitiveType.Cylinder, new Vector3(0, 0.012f, 0.4f), new Vector3(0.016f, 0.09f, 0.016f), m.metal, new Vector3(90, 0, 0)); // barrel
            Part(root, PrimitiveType.Cylinder, new Vector3(0, 0.012f, 0.5f), new Vector3(0.026f, 0.025f, 0.026f), m.metal, new Vector3(90, 0, 0)); // muzzle brake
            Part(root, PrimitiveType.Cube, new Vector3(0, -0.08f, 0.04f), new Vector3(0.03f, 0.11f, 0.055f), body, new Vector3(-12, 0, 0)); // magazine
            Part(root, PrimitiveType.Cube, new Vector3(0, -0.07f, -0.09f), new Vector3(0.03f, 0.08f, 0.035f), m.polymer, new Vector3(18, 0, 0)); // grip
            Part(root, PrimitiveType.Cube, new Vector3(0, -0.005f, -0.24f), new Vector3(0.04f, 0.075f, 0.18f), body);       // stock
            Part(root, PrimitiveType.Cube, new Vector3(0, -0.055f, -0.27f), new Vector3(0.035f, 0.05f, 0.03f), body);       // stock heel
            Part(root, PrimitiveType.Cube, new Vector3(0, 0.04f, 0.02f), new Vector3(0.02f, 0.012f, 0.16f), m.metal);       // top rail
            RedDot(root, 0.03f, m);
            Muzzle(root, 0.53f);
            root.localScale = Vector3.one * scale;
            return root;
        }

        /// <summary>Hornet-9 SMG: short and compact, long stick magazine, skeleton stock.</summary>
        static Transform BuildSmg(Transform parent, float scale)
        {
            Transform root = Root("SMG", parent);
            Mats m = Materials(false);

            Part(root, PrimitiveType.Cube, new Vector3(0, 0, 0), new Vector3(0.045f, 0.07f, 0.2f), m.metal);                // receiver
            Part(root, PrimitiveType.Cube, new Vector3(0, 0.005f, 0.17f), new Vector3(0.05f, 0.055f, 0.14f), m.metal);      // handguard
            Part(root, PrimitiveType.Cylinder, new Vector3(0, 0.012f, 0.27f), new Vector3(0.024f, 0.025f, 0.024f), m.metal, new Vector3(90, 0, 0)); // flash hider
            Part(root, PrimitiveType.Cube, new Vector3(0, -0.13f, 0.03f), new Vector3(0.025f, 0.2f, 0.04f), m.polymer, new Vector3(-4, 0, 0)); // long stick magazine
            Part(root, PrimitiveType.Cube, new Vector3(0, -0.07f, -0.07f), new Vector3(0.03f, 0.08f, 0.035f), m.polymer, new Vector3(18, 0, 0)); // grip
            Part(root, PrimitiveType.Cube, new Vector3(0, 0.01f, -0.19f), new Vector3(0.012f, 0.012f, 0.18f), m.metal);     // stock rod
            Part(root, PrimitiveType.Cube, new Vector3(0, -0.015f, -0.28f), new Vector3(0.035f, 0.075f, 0.02f), m.polymer); // butt plate
            Part(root, PrimitiveType.Cube, new Vector3(0, 0.04f, 0.04f), new Vector3(0.02f, 0.012f, 0.24f), m.metal);       // top rail
            RedDot(root, 0.02f, m);
            Muzzle(root, 0.3f);
            root.localScale = Vector3.one * scale;
            return root;
        }

        /// <summary>Longreach DMR: long handguard, suppressor, 20-round magazine, scope housing.</summary>
        static Transform BuildDmr(Transform parent, float scale)
        {
            Transform root = Root("DMR", parent);
            Mats m = Materials(false);

            Part(root, PrimitiveType.Cube, new Vector3(0, 0, 0), new Vector3(0.045f, 0.07f, 0.3f), m.metal);                // receiver
            Part(root, PrimitiveType.Cube, new Vector3(0, 0.005f, 0.3f), new Vector3(0.05f, 0.055f, 0.3f), m.metal);        // long handguard
            Part(root, PrimitiveType.Cylinder, new Vector3(0, 0.012f, 0.5f), new Vector3(0.016f, 0.06f, 0.016f), m.metal, new Vector3(90, 0, 0)); // barrel
            Part(root, PrimitiveType.Cylinder, new Vector3(0, 0.012f, 0.64f), new Vector3(0.034f, 0.09f, 0.034f), m.polymer, new Vector3(90, 0, 0)); // suppressor
            Part(root, PrimitiveType.Cube, new Vector3(0, -0.08f, 0.06f), new Vector3(0.035f, 0.12f, 0.06f), m.polymer, new Vector3(-8, 0, 0)); // magazine
            Part(root, PrimitiveType.Cube, new Vector3(0, -0.07f, -0.1f), new Vector3(0.03f, 0.08f, 0.035f), m.polymer, new Vector3(18, 0, 0)); // grip
            Part(root, PrimitiveType.Cube, new Vector3(0, -0.005f, -0.3f), new Vector3(0.04f, 0.07f, 0.22f), m.polymer);    // stock
            Part(root, PrimitiveType.Cube, new Vector3(0, 0.04f, 0.1f), new Vector3(0.02f, 0.012f, 0.4f), m.metal);         // top rail
            // scope: open sight window on the eye line, with the tube housing above it so nothing blocks the view
            RedDot(root, 0.06f, m, 0.034f, 0.045f);
            Part(root, PrimitiveType.Cylinder, new Vector3(0, SightHeight + 0.03f, 0.04f), new Vector3(0.022f, 0.08f, 0.022f), m.metal, new Vector3(90, 0, 0));
            Muzzle(root, 0.74f);
            root.localScale = Vector3.one * scale;
            return root;
        }

        /// <summary>
        /// An open sight frame around the eye line with a small emissive dot in the middle. The crosshair hides while
        /// aiming, so this dot is the aim point; the frame is hollow so the target stays visible through it.
        /// </summary>
        static void RedDot(Transform root, float z, Mats m, float width = 0.032f, float length = 0.03f)
        {
            float bottom = 0.046f, top = SightHeight + 0.016f, wall = 0.004f;
            float h = top - bottom;
            Part(root, PrimitiveType.Cube, new Vector3(-width / 2f, bottom + h / 2f, z), new Vector3(wall, h, length), m.metal); // left wall
            Part(root, PrimitiveType.Cube, new Vector3(width / 2f, bottom + h / 2f, z), new Vector3(wall, h, length), m.metal);  // right wall
            Part(root, PrimitiveType.Cube, new Vector3(0, top, z), new Vector3(width + wall, wall, length), m.metal);              // top
            Part(root, PrimitiveType.Cube, new Vector3(0, bottom - 0.004f, z), new Vector3(width, 0.008f, length + 0.01f), m.metal); // base
            Part(root, PrimitiveType.Cube, new Vector3(0, SightHeight, z + length / 2f), new Vector3(0.0022f, 0.0022f, 0.001f), m.reticle); // dot
        }

        static Transform Root(string name, Transform parent)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            return root;
        }

        static void Muzzle(Transform root, float z)
        {
            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(root, false);
            muzzle.localPosition = new Vector3(0, 0.012f, z);
        }

        static void Part(Transform parent, PrimitiveType type, Vector3 pos, Vector3 scale, Material m, Vector3? euler = null)
        {
            GameObject g = GameObject.CreatePrimitive(type);
            Object.DestroyImmediate(g.GetComponent<Collider>());
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localScale = scale;
            if (euler.HasValue) g.transform.localRotation = Quaternion.Euler(euler.Value);
            Renderer r = g.GetComponent<Renderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
