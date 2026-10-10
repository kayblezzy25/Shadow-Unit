using UnityEngine;

namespace ShadowUnit
{
    /// <summary>Pooled combat effects: muzzle flashes, tracers and impact particles.</summary>
    public class EffectsManager : MonoBehaviour
    {
        public static EffectsManager I { get; private set; }

        class Flash { public Transform tr; public Light light; public float until; }

        Flash[] flashes;
        int flashNext;
        LineRenderer[] tracers;
        float[] tracerUntil;
        int tracerNext;
        ParticleSystem dust, blood, sparks;
        Camera cam;

        public static EffectsManager Create()
        {
            var go = new GameObject("EffectsManager");
            I = go.AddComponent<EffectsManager>();
            I.Init();
            return I;
        }

        void Init()
        {
            Material flashMat = MaterialLibrary.Unlit("flash", new Color(1f, 0.82f, 0.45f, 1f), MaterialLibrary.Soft);
            flashes = new Flash[12];
            for (int i = 0; i < flashes.Length; i++)
            {
                GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                DestroyImmediate(q.GetComponent<Collider>());
                q.name = "MuzzleFlash";
                q.transform.SetParent(transform);
                q.GetComponent<Renderer>().sharedMaterial = flashMat;
                q.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                Light l = q.AddComponent<Light>();
                l.type = LightType.Point;
                l.range = 7f;
                l.intensity = 3f;
                l.color = new Color(1f, 0.75f, 0.4f);
                l.shadows = LightShadows.None;
                q.SetActive(false);
                flashes[i] = new Flash { tr = q.transform, light = l };
            }

            Material lineMat = MaterialLibrary.Unlit("tracer", Color.white);
            tracers = new LineRenderer[30];
            tracerUntil = new float[tracers.Length];
            for (int i = 0; i < tracers.Length; i++)
            {
                var lr = new GameObject("Tracer").AddComponent<LineRenderer>();
                lr.transform.SetParent(transform);
                lr.sharedMaterial = lineMat;
                lr.positionCount = 2;
                lr.startWidth = 0.025f;
                lr.endWidth = 0.012f;
                lr.startColor = new Color(1f, 0.85f, 0.5f, 0.9f);
                lr.endColor = new Color(1f, 0.7f, 0.3f, 0.2f);
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lr.enabled = false;
                tracers[i] = lr;
            }

            dust = MakeSystem("Dust", 0.6f);
            blood = MakeSystem("Blood", 1.2f);
            sparks = MakeSystem("Sparks", 1.5f);
        }

        ParticleSystem MakeSystem(string name, float gravity)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.maxParticles = 400;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = gravity;
            ParticleSystem.EmissionModule em = ps.emission;
            em.enabled = false;
            ParticleSystem.ShapeModule sh = ps.shape;
            sh.enabled = false;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = MaterialLibrary.Unlit("particle", Color.white, MaterialLibrary.Soft);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return ps;
        }

        void Emit(ParticleSystem ps, Vector3 pos, Vector3 normal, int count, Color c, float speed, float size, float life)
        {
            var ep = new ParticleSystem.EmitParams();
            for (int i = 0; i < count; i++)
            {
                ep.position = pos + normal * 0.03f;
                ep.velocity = (normal * Random.Range(0.4f, 1f) + Random.insideUnitSphere * 0.7f) * speed * Random.Range(0.5f, 1.2f);
                ep.startSize = size * Random.Range(0.6f, 1.4f);
                ep.startLifetime = life * Random.Range(0.6f, 1.3f);
                ep.startColor = c;
                ps.Emit(ep, 1);
            }
        }

        public void MuzzleFlash(Vector3 pos, float scale = 1f)
        {
            Flash f = flashes[flashNext];
            flashNext = (flashNext + 1) % flashes.Length;
            f.tr.position = pos;
            f.tr.localScale = Vector3.one * Random.Range(0.25f, 0.4f) * scale;
            f.tr.gameObject.SetActive(true);
            f.until = Time.time + 0.045f;
        }

        public void Tracer(Vector3 from, Vector3 to)
        {
            LineRenderer lr = tracers[tracerNext];
            tracerUntil[tracerNext] = Time.time + 0.05f;
            tracerNext = (tracerNext + 1) % tracers.Length;
            lr.SetPosition(0, from);
            lr.SetPosition(1, to);
            lr.enabled = true;
        }

        public void Impact(Vector3 pos, Vector3 normal, bool flesh)
        {
            if (flesh)
            {
                Emit(blood, pos, normal, 10, new Color(0.45f, 0.03f, 0.02f, 0.9f), 2.2f, 0.08f, 0.45f);
            }
            else
            {
                Emit(dust, pos, normal, 9, new Color(0.62f, 0.58f, 0.5f, 0.7f), 1.6f, 0.14f, 0.7f);
                Emit(sparks, pos, normal, 4, new Color(1f, 0.8f, 0.4f, 1f), 4f, 0.03f, 0.18f);
            }
        }

        void LateUpdate()
        {
            if (cam == null) cam = Camera.main;
            float now = Time.time;
            foreach (Flash f in flashes)
            {
                if (!f.tr.gameObject.activeSelf) continue;
                if (now > f.until) { f.tr.gameObject.SetActive(false); continue; }
                if (cam != null) f.tr.rotation = cam.transform.rotation;
            }
            for (int i = 0; i < tracers.Length; i++)
                if (tracers[i].enabled && now > tracerUntil[i]) tracers[i].enabled = false;
        }
    }
}
