using System;
using UnityEngine;

namespace ShadowUnit
{
    /// <summary>
    /// Capture zone. Progress runs from -1 (Red owns) to +1 (Blue owns); standing inside with no enemies present moves it.
    /// </summary>
    public class CapturePoint : MonoBehaviour
    {
        public string label = "A";
        [Tooltip("Map location shown in announcements, e.g. Market District.")]
        public string placeName = "";
        public float radius = 7f;
        [Tooltip("Seconds for one soldier to take a neutral point.")]
        public float captureTime = 9f;

        /// <summary>-1 neutral, otherwise (int)Team.</summary>
        public int owner = -1;
        public float progress;
        public int blueInside, redInside;

        public bool Contested => blueInside > 0 && redInside > 0;
        public bool IsOwnedBy(Team t) => owner == (int)t;

        /// <summary>"A", or "A · Market District" when the point has a place name.</summary>
        public string FullName => string.IsNullOrEmpty(placeName) ? label : label + " · " + placeName;

        public static event Action<CapturePoint, Team> Captured;
        public static event Action<CapturePoint> Neutralized;

        // drop handlers from a previous Editor play session (domain reload off)
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Captured = null; Neutralized = null; }

        Transform flag;
        Renderer flagRenderer;
        LineRenderer ring;
        Material flagMat;

        public void Setup(string pointLabel, string place = "")
        {
            label = pointLabel;
            placeName = place;
            Material pole = MaterialLibrary.Lit("pole", new Color(0.35f, 0.35f, 0.33f), null, null, 0.5f, 0.6f);
            GameObject p = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            p.name = "FlagPole";
            p.transform.SetParent(transform, false);
            p.transform.localPosition = new Vector3(0, 3.5f, 0);
            p.transform.localScale = new Vector3(0.09f, 3.5f, 0.09f);
            p.GetComponent<Renderer>().sharedMaterial = pole;

            GameObject f = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.DestroyImmediate(f.GetComponent<Collider>());
            f.name = "Flag";
            f.transform.SetParent(transform, false);
            f.transform.localScale = new Vector3(1.5f, 0.95f, 0.04f);
            flag = f.transform;
            flagRenderer = f.GetComponent<Renderer>();
            flagMat = new Material(MaterialLibrary.Lit("flag", Color.white, MaterialLibrary.Tex("camo"), null, 0.05f));
            flagRenderer.sharedMaterial = flagMat;

            ring = new GameObject("Ring").AddComponent<LineRenderer>();
            ring.transform.SetParent(transform, false);
            ring.useWorldSpace = false;
            ring.loop = true;
            ring.positionCount = 64;
            ring.startWidth = ring.endWidth = 0.18f;
            ring.sharedMaterial = MaterialLibrary.Unlit("ring", Color.white);
            ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            for (int i = 0; i < 64; i++)
            {
                float a = i / 64f * Mathf.PI * 2f;
                ring.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0.06f, Mathf.Sin(a) * radius));
            }
            ResetPoint();
        }

        public void ResetPoint()
        {
            owner = -1;
            progress = 0f;
            blueInside = redInside = 0;
            UpdateVisuals();
        }

        void Update()
        {
            if (MatchManager.I == null || MatchManager.I.State != MatchState.Playing) return;
            blueInside = redInside = 0;
            Vector3 c = transform.position;
            foreach (Soldier s in Soldier.All)
            {
                if (!s.Alive) continue;
                Vector3 d = s.transform.position - c;
                d.y = 0f;
                if (d.sqrMagnitude > radius * radius) continue;
                if (s.team == Team.Blue) blueInside++; else redInside++;
            }

            if (blueInside > 0 != redInside > 0)
            {
                Team t = blueInside > 0 ? Team.Blue : Team.Red;
                int count = Mathf.Min(3, t == Team.Blue ? blueInside : redInside);
                float dir = t == Team.Blue ? 1f : -1f;
                float rate = (0.6f + 0.4f * count) / captureTime;
                float before = progress;
                progress = Mathf.Clamp(progress + dir * rate * Time.deltaTime, -1f, 1f);

                if (owner != -1 && Mathf.Sign(before) != Mathf.Sign(progress) && owner != (int)t)
                {
                    owner = -1;
                    Neutralized?.Invoke(this);
                }
                if (Mathf.Abs(progress) >= 1f && owner != (int)t)
                {
                    owner = (int)t;
                    Captured?.Invoke(this, t);
                }
            }
            UpdateVisuals();
        }

        void UpdateVisuals()
        {
            if (flag == null) return;
            Color neutral = new Color(0.85f, 0.85f, 0.8f);
            Color side = progress >= 0 ? Team.Blue.UiColor() : Team.Red.UiColor();
            Color c = Color.Lerp(neutral, side, Mathf.Abs(progress));
            flagMat.color = c;
            flag.localPosition = new Vector3(0.78f, 1.4f + Mathf.Abs(progress) * 5.1f, 0f);
            Color rc = owner == -1 ? new Color(1f, 1f, 1f, 0.55f) : ((Team)owner).UiColor();
            if (Contested) rc = Color.Lerp(rc, Color.yellow, 0.5f + 0.5f * Mathf.Sin(Time.time * 8f));
            ring.startColor = ring.endColor = rc;
        }
    }
}
