using UnityEngine;

namespace ShadowUnit
{
    /// <summary>
    /// Placeholder soldier model built from primitives, with procedural walk and death animation.
    /// Visual only: hits are registered on the CharacterController collider. Replace with a rigged model later.
    /// </summary>
    public class SoldierBody : MonoBehaviour
    {
        Transform visual, hips, legL, legR, kneeL, kneeR, torso;
        public Transform Muzzle { get; private set; }
        float phase;
        float deathT = -1f;
        float deathSide = 1f;

        public void Build(Team team)
        {
            visual = new GameObject("Visual").transform;
            visual.SetParent(transform, false);

            bool blue = team == Team.Blue;
            Material uniform = MaterialLibrary.Lit(blue ? "uniformBlue" : "uniformRed", blue ? new Color(0.27f, 0.29f, 0.27f) : new Color(0.62f, 0.54f, 0.4f), MaterialLibrary.Tex("camo"), Vector2.one, 0.05f);
            Material plate = MaterialLibrary.Lit(blue ? "plateBlue" : "plateRed", blue ? new Color(0.17f, 0.19f, 0.17f) : new Color(0.42f, 0.36f, 0.26f), null, null, 0.1f);
            Material helmet = MaterialLibrary.Lit(blue ? "helmetBlue" : "helmetRed", blue ? new Color(0.2f, 0.21f, 0.2f) : new Color(0.5f, 0.44f, 0.32f), null, null, 0.25f);
            Material skin = MaterialLibrary.Lit("skin", new Color(0.72f, 0.55f, 0.43f), null, null, 0.2f);
            Material boot = MaterialLibrary.Lit("boot", new Color(0.12f, 0.1f, 0.08f), null, null, 0.2f);
            Material glove = MaterialLibrary.Lit("glove", new Color(0.1f, 0.1f, 0.1f), null, null, 0.1f);
            Color tc = team.UiColor();
            Material band = MaterialLibrary.Emissive(blue ? "bandBlue" : "bandRed", tc, tc * 0.35f);

            hips = Pivot(visual, new Vector3(0, 0.95f, 0));
            Part(hips, new Vector3(0, 0.02f, 0), new Vector3(0.34f, 0.16f, 0.2f), uniform);
            legL = Leg(hips, -0.1f, uniform, boot, out kneeL);
            legR = Leg(hips, 0.1f, uniform, boot, out kneeR);

            torso = Pivot(hips, new Vector3(0, 0.08f, 0));
            Part(torso, new Vector3(0, 0.27f, 0), new Vector3(0.37f, 0.5f, 0.22f), uniform);
            Part(torso, new Vector3(0, 0.3f, 0.01f), new Vector3(0.41f, 0.36f, 0.28f), plate);
            for (int i = -1; i <= 1; i++) Part(torso, new Vector3(i * 0.11f, 0.2f, 0.16f), new Vector3(0.09f, 0.12f, 0.06f), plate);
            Part(torso, new Vector3(0, 0.33f, -0.18f), new Vector3(0.3f, 0.34f, 0.12f), plate); // back pack
            Part(torso, new Vector3(0, 0.58f, 0), new Vector3(0.1f, 0.08f, 0.1f), skin, PrimitiveType.Cylinder);
            Part(torso, new Vector3(0, 0.71f, 0.01f), new Vector3(0.2f, 0.23f, 0.22f), skin, PrimitiveType.Sphere);
            Part(torso, new Vector3(0, 0.775f, -0.005f), new Vector3(0.25f, 0.17f, 0.27f), helmet, PrimitiveType.Sphere);
            Part(torso, new Vector3(0, 0.86f, 0.02f), new Vector3(0.06f, 0.025f, 0.06f), band);           // IR patch so teams read at range
            Part(torso, new Vector3(0, 0.79f, 0.13f), new Vector3(0.06f, 0.05f, 0.04f), glove);           // NVG mount

            // arms posed holding the rifle
            Transform shR = Pivot(torso, new Vector3(0.24f, 0.46f, 0));
            shR.localRotation = Quaternion.Euler(-62f, 0f, 8f);
            Part(shR, new Vector3(0, -0.14f, 0), new Vector3(0.11f, 0.3f, 0.11f), uniform);
            Transform elR = Pivot(shR, new Vector3(0, -0.28f, 0));
            elR.localRotation = Quaternion.Euler(-48f, 0, 0);
            Part(elR, new Vector3(0, -0.13f, 0), new Vector3(0.1f, 0.27f, 0.1f), uniform);
            Part(elR, new Vector3(0, -0.28f, 0), new Vector3(0.09f, 0.09f, 0.09f), glove);

            Transform shL = Pivot(torso, new Vector3(-0.24f, 0.46f, 0));
            shL.localRotation = Quaternion.Euler(-78f, 0f, -32f);
            Part(shL, new Vector3(0, -0.14f, 0), new Vector3(0.11f, 0.3f, 0.11f), uniform);
            Part(shL, new Vector3(0, -0.12f, 0), new Vector3(0.115f, 0.06f, 0.115f), band);              // armband
            Transform elL = Pivot(shL, new Vector3(0, -0.28f, 0));
            elL.localRotation = Quaternion.Euler(-22f, 0, 0);
            Part(elL, new Vector3(0, -0.13f, 0), new Vector3(0.1f, 0.27f, 0.1f), uniform);
            Part(elL, new Vector3(0, -0.28f, 0), new Vector3(0.09f, 0.09f, 0.09f), glove);

            Transform rifle = WeaponModels.BuildRifle(torso, 1f, !blue);
            rifle.localPosition = new Vector3(0.1f, 0.33f, 0.3f);
            foreach (Renderer r in rifle.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            Muzzle = rifle.Find("Muzzle");
        }

        static Transform Pivot(Transform parent, Vector3 pos)
        {
            var t = new GameObject("Pivot").transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            return t;
        }

        static void Part(Transform parent, Vector3 pos, Vector3 scale, Material m, PrimitiveType type = PrimitiveType.Cube)
        {
            GameObject g = GameObject.CreatePrimitive(type);
            Object.DestroyImmediate(g.GetComponent<Collider>());
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = m;
        }

        Transform Leg(Transform hip, float x, Material uniform, Material boot, out Transform knee)
        {
            Transform thigh = Pivot(hip, new Vector3(x, 0, 0));
            Part(thigh, new Vector3(0, -0.22f, 0), new Vector3(0.15f, 0.45f, 0.17f), uniform);
            knee = Pivot(thigh, new Vector3(0, -0.45f, 0));
            Part(knee, new Vector3(0, 0, 0.07f), new Vector3(0.12f, 0.1f, 0.05f), boot);  // knee pad
            Part(knee, new Vector3(0, -0.21f, 0), new Vector3(0.13f, 0.42f, 0.14f), uniform);
            Part(knee, new Vector3(0, -0.45f, 0.04f), new Vector3(0.14f, 0.1f, 0.27f), boot);
            return thigh;
        }

        /// <summary>Called every frame with ground speed in m/s.</summary>
        public void Animate(float speed, bool crouched, float dt)
        {
            if (deathT >= 0f)
            {
                deathT += dt;
                float k = Mathf.Clamp01(deathT / 0.45f);
                k = 1f - (1f - k) * (1f - k);
                visual.localRotation = Quaternion.Euler(0, 0, deathSide * 88f * k);
                visual.localPosition = new Vector3(deathSide * -0.35f * k, -0.05f * k, 0);
                return;
            }
            float a = Mathf.Clamp01(speed / 4.5f);
            phase += dt * (2f + speed * 2.1f);
            float s = Mathf.Sin(phase);
            legL.localRotation = Quaternion.Euler(s * 38f * a, 0, 0);
            legR.localRotation = Quaternion.Euler(-s * 38f * a, 0, 0);
            kneeL.localRotation = Quaternion.Euler(Mathf.Max(0, s) * 45f * a + (crouched ? 70f : 0f), 0, 0);
            kneeR.localRotation = Quaternion.Euler(Mathf.Max(0, -s) * 45f * a + (crouched ? 70f : 0f), 0, 0);
            float bob = Mathf.Abs(Mathf.Cos(phase)) * 0.035f * a;
            hips.localPosition = new Vector3(0, (crouched ? 0.62f : 0.95f) + bob, 0);
            torso.localRotation = Quaternion.Euler(a * 7f + (crouched ? 12f : 0f), 0, 0);
        }

        public void Die(Vector3 hitDirection)
        {
            deathT = 0f;
            deathSide = Vector3.Dot(hitDirection, transform.right) >= 0 ? -1f : 1f;
        }

        public void Revive()
        {
            deathT = -1f;
            visual.localRotation = Quaternion.identity;
            visual.localPosition = Vector3.zero;
        }
    }
}
