using UnityEngine;

namespace ShadowUnit
{
    /// <summary>
    /// Builds a placeholder rifle model from primitives (no colliders). Swap for an imported model in the polish phase.
    /// Returns the root; the muzzle transform is named "Muzzle".
    /// </summary>
    public static class WeaponModels
    {
        public static Transform BuildRifle(Transform parent, float scale = 1f, bool woodFurniture = false)
        {
            var root = new GameObject("Rifle").transform;
            root.SetParent(parent, false);
            Material metal = MaterialLibrary.Lit("gunmetal", new Color(0.11f, 0.115f, 0.12f), MaterialLibrary.Tex("metal"), Vector2.one, 0.45f, 0.65f);
            Material polymer = MaterialLibrary.Lit("polymer", new Color(0.16f, 0.16f, 0.15f), null, null, 0.2f, 0f);
            Material furniture = woodFurniture ? MaterialLibrary.Lit("gunwood", new Color(0.4f, 0.24f, 0.13f), MaterialLibrary.Tex("wood"), Vector2.one, 0.3f) : polymer;
            Material glass = MaterialLibrary.Emissive("optic", new Color(0.1f, 0.2f, 0.3f), new Color(0.05f, 0.2f, 0.35f));

            // Local axes: +Z forward, +Y up. Units in metres.
            Part(root, PrimitiveType.Cube, new Vector3(0, 0, 0), new Vector3(0.045f, 0.07f, 0.26f), metal);               // receiver
            Part(root, PrimitiveType.Cube, new Vector3(0, 0.005f, 0.22f), new Vector3(0.05f, 0.055f, 0.2f), furniture);    // handguard
            Part(root, PrimitiveType.Cylinder, new Vector3(0, 0.012f, 0.4f), new Vector3(0.016f, 0.09f, 0.016f), metal, new Vector3(90, 0, 0)); // barrel
            Part(root, PrimitiveType.Cylinder, new Vector3(0, 0.012f, 0.5f), new Vector3(0.026f, 0.025f, 0.026f), metal, new Vector3(90, 0, 0)); // muzzle brake
            Part(root, PrimitiveType.Cube, new Vector3(0, -0.08f, 0.04f), new Vector3(0.03f, 0.11f, 0.055f), metal, new Vector3(-12, 0, 0));    // magazine
            Part(root, PrimitiveType.Cube, new Vector3(0, -0.07f, -0.09f), new Vector3(0.03f, 0.08f, 0.035f), polymer, new Vector3(18, 0, 0)); // grip
            Part(root, PrimitiveType.Cube, new Vector3(0, -0.01f, -0.24f), new Vector3(0.038f, 0.065f, 0.18f), furniture);    // stock
            Part(root, PrimitiveType.Cube, new Vector3(0, 0.055f, 0.02f), new Vector3(0.035f, 0.035f, 0.08f), metal);         // optic body
            Part(root, PrimitiveType.Cube, new Vector3(0, 0.056f, 0.062f), new Vector3(0.028f, 0.026f, 0.004f), glass);       // optic lens
            Part(root, PrimitiveType.Cube, new Vector3(0, 0.04f, 0.02f), new Vector3(0.02f, 0.012f, 0.16f), metal);           // rail

            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(root, false);
            muzzle.localPosition = new Vector3(0, 0.012f, 0.55f);
            root.localScale = Vector3.one * scale;
            return root;
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
