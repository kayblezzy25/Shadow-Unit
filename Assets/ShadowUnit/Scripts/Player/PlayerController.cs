using UnityEngine;

namespace ShadowUnit
{
    /// <summary>First-person movement: walk, sprint, crouch, jump, touch look and recoil.</summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public float walkSpeed = 4.2f;
        public float sprintSpeed = 6.6f;
        public float crouchSpeed = 2.2f;
        public float aimSpeed = 2.7f;
        public float jumpHeight = 1.05f;
        public float gravity = -19f;
        [Tooltip("Degrees of rotation per screen pixel at sensitivity 1.")]
        public float pixelToDegrees = 0.11f;

        public Camera Cam { get; private set; }
        public WeaponController Weapon { get; private set; }
        public Soldier Soldier { get; private set; }
        public bool IsSprinting { get; private set; }
        public bool IsCrouched { get; private set; }
        public bool Grounded => cc != null && cc.isGrounded;
        public float HorizontalSpeed { get; private set; }

        CharacterController cc;
        float yaw, pitch;
        Vector2 recoil, recoilTarget;
        float verticalVelocity;
        float stepTimer;
        float camHeight = 1.65f;
        float bobPhase;

        const float StandHeight = 1.8f, CrouchHeight = 1.15f;

        public static PlayerController Spawn(Camera cam, WeaponData weapon)
        {
            var go = new GameObject("Player");
            var cc = go.AddComponent<CharacterController>();
            cc.height = StandHeight;
            cc.radius = 0.35f;
            cc.center = new Vector3(0, StandHeight / 2f, 0);
            cc.stepOffset = 0.4f;
            cc.slopeLimit = 50f;
            Health h = go.AddComponent<Health>();
            h.max = 100f;
            h.regenDelay = 5f;
            h.regenRate = 18f;
            Soldier s = go.AddComponent<Soldier>();
            s.team = Team.Blue;
            s.isPlayer = true;
            s.displayName = "You";
            PlayerController pc = go.AddComponent<PlayerController>();
            pc.cc = cc;
            pc.Soldier = s;
            pc.Cam = cam;
            cam.transform.SetParent(go.transform, false);
            cam.transform.localPosition = new Vector3(0, 1.65f, 0);
            cam.transform.localRotation = Quaternion.identity;
            cam.nearClipPlane = 0.03f;
            pc.Weapon = cam.gameObject.AddComponent<WeaponController>();
            pc.Weapon.Init(cam, pc, s, weapon);
            return pc;
        }

        void Awake()
        {
            if (cc == null) cc = GetComponent<CharacterController>();
        }

        /// <summary>Re-parents the shared camera after the menu fly-over used it.</summary>
        public void AttachCamera()
        {
            Cam.transform.SetParent(transform, false);
            Cam.transform.localPosition = new Vector3(0, 1.65f, 0);
            Cam.transform.localRotation = Quaternion.identity;
        }

        public void Teleport(Vector3 position, float yawDegrees)
        {
            cc.enabled = false;
            transform.position = position;
            cc.enabled = true;
            yaw = yawDegrees;
            pitch = 0f;
            recoil = recoilTarget = Vector2.zero;
            verticalVelocity = 0f;
            IsCrouched = false;
            IsSprinting = false;
            ApplyRotation();
        }

        public void AddRecoil(float up, float side)
        {
            recoilTarget += new Vector2(side, up);
        }

        void Update()
        {
            if (MatchManager.I == null || MatchManager.I.State != MatchState.Playing || !Soldier.Alive) { HorizontalSpeed = 0f; return; }
            MobileInputController input = MobileInputController.I;
            if (input == null) return;

            // ----- look -----
            Vector2 look = input.ConsumeLook();
            float sens = (Weapon.IsAiming ? GameSettings.AimSensitivity : GameSettings.LookSensitivity) * pixelToDegrees;
            yaw += look.x * sens;
            pitch -= look.y * sens * (GameSettings.InvertY ? -1f : 1f);
            pitch = Mathf.Clamp(pitch, -82f, 82f);

            float rec = Weapon.data != null ? Weapon.data.recoilRecovery : 7f;
            recoil = Vector2.Lerp(recoil, recoilTarget, Time.deltaTime * 25f);
            recoilTarget = Vector2.Lerp(recoilTarget, Vector2.zero, Time.deltaTime * rec * (input.FireHeld ? 0.35f : 1f));
            ApplyRotation();

            // ----- stance -----
            if (input.ConsumeCrouch()) IsCrouched = !IsCrouched;
            if (input.ConsumeSprint()) input.SprintLatched = !input.SprintLatched;
            Vector2 move = input.Move;
            bool wantsSprint = (input.SprintLatched || input.JoystickPushedHard) && move.y > 0.5f;
            IsSprinting = wantsSprint && !Weapon.IsAiming && !input.FireHeld && !Weapon.Reloading;
            if (IsSprinting) IsCrouched = false;
            if (move.sqrMagnitude < 0.01f) input.SprintLatched = false;

            float targetHeight = IsCrouched ? CrouchHeight : StandHeight;
            if (!IsCrouched && cc.height < StandHeight - 0.01f)
            {
                // do not stand up into a ceiling
                if (Physics.SphereCast(transform.position + Vector3.up * cc.height * 0.5f, cc.radius * 0.9f, Vector3.up, out _, StandHeight - cc.height * 0.5f, ~0, QueryTriggerInteraction.Ignore))
                    targetHeight = cc.height;
            }
            cc.height = Mathf.MoveTowards(cc.height, targetHeight, Time.deltaTime * 4f);
            cc.center = new Vector3(0, cc.height / 2f, 0);
            camHeight = cc.height - 0.15f;

            // ----- move -----
            float speed = IsSprinting ? sprintSpeed : IsCrouched ? crouchSpeed : Weapon.IsAiming ? aimSpeed : walkSpeed;
            Vector3 wish = (transform.right * move.x + transform.forward * move.y);
            if (wish.sqrMagnitude > 1f) wish.Normalize();
            Vector3 horizontal = wish * speed;

            if (cc.isGrounded)
            {
                if (verticalVelocity < 0f) verticalVelocity = -2f;
                if (input.ConsumeJump() && !IsCrouched)
                {
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                    AudioManager.I.Play2D(AudioLibrary.Footstep, 0.4f, 0.8f);
                }
            }
            else input.ConsumeJump();
            verticalVelocity += gravity * Time.deltaTime;
            cc.Move((horizontal + Vector3.up * verticalVelocity) * Time.deltaTime);

            Vector3 v = cc.velocity;
            v.y = 0f;
            HorizontalSpeed = v.magnitude;

            // ----- camera height, bob, footsteps -----
            bobPhase += HorizontalSpeed * Time.deltaTime * 1.9f;
            float bob = cc.isGrounded ? Mathf.Sin(bobPhase * 2f) * 0.025f * Mathf.Clamp01(HorizontalSpeed / 5f) * (1f - Weapon.AimBlend * 0.8f) : 0f;
            Vector3 lp = Cam.transform.localPosition;
            lp.y = Mathf.Lerp(lp.y, camHeight + bob, Time.deltaTime * 12f);
            Cam.transform.localPosition = lp;

            if (cc.isGrounded && HorizontalSpeed > 1f)
            {
                stepTimer -= Time.deltaTime * HorizontalSpeed;
                if (stepTimer <= 0f)
                {
                    stepTimer = 2.1f;
                    AudioManager.I.Play2D(AudioLibrary.Footstep, IsCrouched ? 0.12f : 0.3f, Random.Range(0.85f, 1.15f));
                }
            }
        }

        void ApplyRotation()
        {
            transform.rotation = Quaternion.Euler(0f, yaw + recoil.x, 0f);
            if (Cam != null) Cam.transform.localRotation = Quaternion.Euler(pitch - recoil.y, 0f, 0f);
        }

        public float Yaw => yaw;
    }
}
