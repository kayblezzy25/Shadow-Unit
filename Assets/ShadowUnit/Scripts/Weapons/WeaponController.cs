using System;
using System.Collections;
using UnityEngine;

namespace ShadowUnit
{
    /// <summary>
    /// First-person weapon: hitscan firing with spread, recoil, aim-down-sights, reload, viewmodel sway and effects.
    /// Stats come from a WeaponData asset so new weapons need no code changes.
    /// </summary>
    public class WeaponController : MonoBehaviour
    {
        public WeaponData data;

        public int Ammo { get; private set; }
        public int Reserve { get; private set; }
        public bool Reloading { get; private set; }
        public bool IsAiming { get; private set; }
        public float AimBlend { get; private set; }
        /// <summary>Current cone half-angle in degrees, for the crosshair.</summary>
        public float CurrentSpread { get; private set; }

        /// <summary>(killed, headshot)</summary>
        public event Action<bool, bool> HitConfirmed;

        Camera cam;
        PlayerController player;
        Soldier owner;
        Transform viewRoot, model, muzzle;
        float nextFire;
        float extraSpread;
        float kick;
        Vector2 sway;
        bool triggerWasDown;
        Coroutine reloadRoutine;

        static readonly Vector3 HipPos = new Vector3(0.17f, -0.17f, 0.32f);
        // optic sits 0.056 m above the receiver origin, so this puts the sight on the eye line
        static readonly Vector3 AdsPos = new Vector3(0f, -0.056f, 0.2f);

        public void Init(Camera camera, PlayerController pc, Soldier soldier, WeaponData weaponData)
        {
            cam = camera;
            player = pc;
            owner = soldier;
            data = weaponData != null ? weaponData : WeaponData.CreateDefaultRifle();
            if (viewRoot == null)
            {
                viewRoot = new GameObject("ViewModel").transform;
                viewRoot.SetParent(cam.transform, false);
                model = WeaponModels.BuildRifle(viewRoot);
                muzzle = model.Find("Muzzle");
            }
            ResetWeapon();
        }

        public void ResetWeapon()
        {
            if (reloadRoutine != null) StopCoroutine(reloadRoutine);
            Ammo = data.magazineSize;
            Reserve = data.reserveAmmo;
            Reloading = false;
            IsAiming = false;
            AimBlend = 0f;
            extraSpread = 0f;
            kick = 0f;
            if (cam != null) cam.fieldOfView = data.hipFov;
        }

        public void AddReserve(int amount) { Reserve += amount; }

        void Update()
        {
            if (cam == null || owner == null || !owner.Alive || MatchManager.I == null || MatchManager.I.State != MatchState.Playing) return;
            MobileInputController input = MobileInputController.I;
            if (input == null) return;

            IsAiming = input.AimActive && !Reloading && !player.IsSprinting;
            float target = IsAiming ? 1f : 0f;
            AimBlend = Mathf.MoveTowards(AimBlend, target, Time.deltaTime / Mathf.Max(0.01f, data.adsTime));
            float eased = AimBlend * AimBlend * (3f - 2f * AimBlend);
            cam.fieldOfView = Mathf.Lerp(data.hipFov, data.adsFov, eased);

            bool trigger = input.FireHeld;
            bool canFire = !Reloading && !player.IsSprinting && Time.time >= nextFire;
            if (trigger && canFire && (data.fireMode == FireMode.Automatic || !triggerWasDown))
            {
                if (Ammo > 0) Fire();
                else if (!triggerWasDown) { AudioManager.I.Play2D(AudioLibrary.DryFire, 0.6f); StartReload(); }
            }
            triggerWasDown = trigger;

            if (input.ConsumeReload()) StartReload();
            if (Ammo == 0 && !Reloading && Reserve > 0 && !trigger) StartReload();

            extraSpread = Mathf.MoveTowards(extraSpread, 0f, data.spreadRecovery * Time.deltaTime);
            float baseSpread = Mathf.Lerp(data.hipSpread, data.adsSpread, eased);
            float moveFactor = Mathf.Lerp(1f, data.moveSpreadMultiplier, Mathf.Clamp01(player.HorizontalSpeed / 4f));
            if (!player.Grounded) moveFactor *= 2f;
            CurrentSpread = baseSpread * moveFactor + extraSpread;

            AnimateViewModel(eased, input);
        }

        void Fire()
        {
            nextFire = Time.time + data.SecondsPerShot;
            Ammo--;
            Vector3 origin = cam.transform.position;
            for (int p = 0; p < Mathf.Max(1, data.pellets); p++)
            {
                Vector3 dir = SpreadDirection(cam.transform.forward, CurrentSpread);
                RaycastHit hit;
                Vector3 end = origin + dir * data.maxRange;
                if (Ballistics.Raycast(origin, dir, data.maxRange, owner.transform, out hit))
                {
                    end = hit.point;
                    float falloff = Mathf.Lerp(1f, data.minDamageFalloff, Mathf.InverseLerp(data.effectiveRange, data.maxRange, hit.distance));
                    bool hitSoldier, killed, headshot;
                    Ballistics.ApplyHit(hit, dir, data.damage * falloff, data.headshotMultiplier, owner, out hitSoldier, out killed, out headshot);
                    if (hitSoldier)
                    {
                        Soldier victim = hit.collider.GetComponentInParent<Soldier>();
                        if (victim != null && victim.team != owner.team)
                        {
                            victim.spottedUntil = Time.time + 3f;
                            HitConfirmed?.Invoke(killed, headshot);
                            AudioManager.I.Play2D(killed ? AudioLibrary.KillMarker : AudioLibrary.HitMarker, 0.7f);
                        }
                    }
                }
                if (p == 0 && EffectsManager.I != null) EffectsManager.I.Tracer(muzzle.position, end);
            }

            extraSpread = Mathf.Min(data.maxExtraSpread, extraSpread + data.spreadPerShot);
            float recoilMul = IsAiming ? data.adsRecoilMultiplier : 1f;
            player.AddRecoil(data.recoilVertical * recoilMul * UnityEngine.Random.Range(0.8f, 1.2f), UnityEngine.Random.Range(-data.recoilHorizontal, data.recoilHorizontal) * recoilMul);
            kick = 1f;
            if (EffectsManager.I != null) EffectsManager.I.MuzzleFlash(muzzle.position, 0.8f);
            AudioManager.I.Play2D(data.fireSound != null ? data.fireSound : AudioLibrary.RifleShot, 0.55f, UnityEngine.Random.Range(0.95f, 1.05f));
            SoldierAI.ReportGunfire(owner, owner.transform.position);
        }

        public static Vector3 SpreadDirection(Vector3 forward, float spreadDegrees)
        {
            if (spreadDegrees <= 0f) return forward;
            Vector2 r = UnityEngine.Random.insideUnitCircle * Mathf.Tan(spreadDegrees * Mathf.Deg2Rad);
            Quaternion look = Quaternion.LookRotation(forward);
            return (look * new Vector3(r.x, r.y, 1f)).normalized;
        }

        public void StartReload()
        {
            if (Reloading || Ammo >= data.magazineSize || Reserve <= 0) return;
            reloadRoutine = StartCoroutine(ReloadRoutine());
        }

        IEnumerator ReloadRoutine()
        {
            Reloading = true;
            AudioManager.I.Play2D(data.reloadSound != null ? data.reloadSound : AudioLibrary.Reload, 0.7f);
            yield return new WaitForSeconds(data.reloadTime);
            int need = data.magazineSize - Ammo;
            int take = Mathf.Min(need, Reserve);
            Ammo += take;
            Reserve -= take;
            Reloading = false;
            reloadRoutine = null;
        }

        void AnimateViewModel(float aim, MobileInputController input)
        {
            Vector2 look = input.LastLookDelta;
            sway = Vector2.Lerp(sway, new Vector2(-look.x, -look.y) * 0.00035f * (1f - aim * 0.8f), Time.deltaTime * 10f);
            float bobAmt = player.Grounded ? Mathf.Clamp01(player.HorizontalSpeed / 5f) * (1f - aim * 0.85f) : 0f;
            float t = Time.time * (player.IsSprinting ? 13f : 9f);
            Vector3 bob = new Vector3(Mathf.Cos(t * 0.5f) * 0.012f, Mathf.Abs(Mathf.Sin(t * 0.5f)) * 0.012f, 0f) * bobAmt;
            kick = Mathf.MoveTowards(kick, 0f, Time.deltaTime * 14f);
            Vector3 pos = Vector3.Lerp(HipPos, AdsPos, aim) + bob + new Vector3(sway.x, sway.y, -kick * 0.045f);
            float reloadDip = Reloading ? 1f : 0f;
            pos += new Vector3(0, -0.06f, 0) * reloadDip;
            Quaternion rot = Quaternion.Euler(-kick * 4f + reloadDip * 25f + (player.IsSprinting ? 12f : 0f), player.IsSprinting ? -28f : 0f, reloadDip * -20f);
            viewRoot.localPosition = Vector3.Lerp(viewRoot.localPosition, pos, Time.deltaTime * 18f);
            viewRoot.localRotation = Quaternion.Slerp(viewRoot.localRotation, rot, Time.deltaTime * 12f);
        }

        public void SetVisible(bool v)
        {
            if (viewRoot != null) viewRoot.gameObject.SetActive(v);
        }
    }
}
