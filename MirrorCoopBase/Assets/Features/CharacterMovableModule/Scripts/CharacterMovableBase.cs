using Features.CharacterMovableModule.Scripts.MovementControls;
using Features.CharacterMovableModule.Scripts.PlayerStats;
using Features.FloatingControllerModule;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.CharacterMovableModule.Scripts {
    public abstract class CharacterMovableBase : NetworkBehaviour {
        [Header("Body")]
        [SerializeField] private Rigidbody _rb;
        [SerializeField] private FloatingController _floatingController;
        [SerializeField] private Transform _rotatablePart;
        [SerializeField] private PlayerStatEntity _stats;

        [SyncVar] private float _facingYaw;

        [Inject]
        private CharacterInputBuffer _input;
        private IMovementControls _ownInput;
        private IMovementControls _remoteInput;
        private bool _hasRemoteLook;
        private float _remoteLookYaw;
        private float _moveSpeed;
        private readonly JumpAndAirGravity _jump = new JumpAndAirGravity();
        private Vector3 _weight;

        public Rigidbody Body => _rb;

        // Grows by one on every takeoff of this body (wraps at 255): the state packet carries it, so a remote copy sees
        // every jump even when the packet of that moment is lost.
        public byte JumpCount { get; private set; }

        // The synced look direction around the up axis, degrees.
        public float LookYaw => _facingYaw;

        // Whether the body stands on something right now (the float probe's support).
        public bool IsSupported => HasSupport();

        private bool ControlsSelf => isOwned;

        // The controls this peer simulates the body from: its own keyboard, a remote copy's source, or none (the body is
        // moved by someone else).
        private IMovementControls ActiveInput => ControlsSelf ? OwnInput : _remoteInput;

        // The keyboard source of the player's own body, made on first use: the input buffer is injected after Awake.
        // Null on peers that do not own this body.
        public IMovementControls OwnInput {
            get {
                if (_ownInput == null && _input != null && ControlsSelf)
                    _ownInput = new KeyboardMovementControls(_input, _stats);

                return _ownInput;
            }
        }

        protected virtual void Awake() =>
            CacheWeight();

        public override void OnStartLocalPlayer() {
            _moveSpeed = ReadStat(PlayerStatType.WalkSpeed);
            _jump.RefillAirJumps(ExtraAirJumps);
            CacheWeight();
        }

        // On a peer that does not own this body: simulate it as a copy from these controls (null: stop, the body is
        // moved by someone else again). The body must be dynamic for the simulation to move it.
        public void SetRemoteInput(IMovementControls source) {
            _remoteInput = source;
            if (source == null)
                return;

            _jump.RefillAirJumps(ExtraAirJumps);
            CacheWeight();
        }

        // On a peer that does not own this body: the look to turn toward instead of the synced one, degrees around the
        // up axis, so a simulated copy turns at the same moment as it walks. ClearRemoteLook goes back to the synced one.
        public void SetRemoteLook(float yaw) {
            _remoteLookYaw = yaw;
            _hasRemoteLook = true;
        }

        public void ClearRemoteLook() =>
            _hasRemoteLook = false;

        private void Update() {
            IMovementControls source = ActiveInput;
            if (source == null)
                return;

            _jump.ReadInput(source.JumpHeld, source.ConsumeJumpPress(), HasSupport(), Time.deltaTime);
        }

        private void LateUpdate() {
            if (ControlsSelf && _input.IsBlocked == false)
                CaptureFacingYaw();

            ApplyRotatableLook();
        }

        private void FixedUpdate() {
            if (_rb == null)
                return;

            // A body nobody simulates on this peer (a copy before its first state packet) or a kinematic one (seated)
            // does not move. Every simulated body, a remote copy too, pushes items by contact.
            IMovementControls source = ActiveInput;
            if (source == null || _rb.isKinematic)
                return;

            Simulate(source);
        }

        // One physics step of the body from its controls: walk or brake, then jump and the extra air gravity.
        private void Simulate(IMovementControls source) {
            if (source == null)
                return;

            Vector3 planar = Flatten(_rb.linearVelocity);
            _moveSpeed = source.SprintHeld ? ReadStat(PlayerStatType.SprintSpeed) : ReadStat(PlayerStatType.WalkSpeed);

            Vector3 wish = ReadWishDirection(source);
            DrivePlanar(wish, planar);
            ApplyJumpAndAirGravity();
        }

        private Vector3 ReadWishDirection(IMovementControls source) {
            Vector3 wish = source.ReadWish();
            if (HasSupport())
                wish = Vector3.ProjectOnPlane(wish, _floatingController.SupportNormal);

            if (wish.sqrMagnitude > 1f)
                wish.Normalize();

            return wish;
        }

        private void DrivePlanar(Vector3 wish, Vector3 planar) {
            bool steering = wish.sqrMagnitude > 0.0001f;
            Vector3 desired;
            float rate;
            float forceCap;

            if (steering) {
                desired = wish * _moveSpeed;
                rate = ReadStat(PlayerStatType.Acceleration);
                forceCap = ReadStat(PlayerStatType.MaxDrive);
            }
            else {
                float speed = planar.magnitude;
                float window = Mathf.Max(ReadStat(PlayerStatType.HaltWindow), 0.01f);
                float deceleration = ReadStat(PlayerStatType.Deceleration);
                float distanceBrake = speed > 0.01f ? (speed * speed) / (2f * window) : deceleration;
                rate = Mathf.Max(deceleration, distanceBrake);
                forceCap = Mathf.Max(ReadStat(PlayerStatType.MaxBrake), rate);
                desired = Vector3.zero;
            }

            if (HasSupport() == false)
                rate *= ReadStat(PlayerStatType.AirControl);

            Vector3 next = Vector3.MoveTowards(planar, desired, rate * Time.fixedDeltaTime);
            Vector3 accel = (next - planar) / Time.fixedDeltaTime;
            accel.y = 0f;
            Vector3 force = Vector3.ClampMagnitude(accel, forceCap) * _rb.mass;
            _rb.AddForce(force);
        }

        private void ApplyJumpAndAirGravity() {
            if (_floatingController == null)
                return;

            JumpTuning tuning = JumpTuning.Read(_stats);
            bool supported = HasSupport();
            if (_jump.TryTakeoff(tuning, supported, _rb.linearVelocity.y, Physics.gravity.y, Time.fixedDeltaTime,
                    out float speedChange)) {
                _floatingController.PauseSupport();
                _rb.AddForce(new Vector3(0f, speedChange, 0f) * _rb.mass, ForceMode.Impulse);
                JumpCount = unchecked((byte)(JumpCount + 1));
            }

            _jump.Land(tuning, supported, _rb.linearVelocity.y);
            if (supported)
                return;

            float extra = _jump.AirGravityExtra(tuning, _rb.linearVelocity.y);
            if (extra > 0f)
                _rb.AddForce(_weight * extra);
        }

        private void CaptureFacingYaw() {
            Transform cameraTransform = Camera.main != null ? Camera.main.transform : null;
            if (cameraTransform == null)
                return;

            Vector3 flat = cameraTransform.forward;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.0001f)
                return;

            float yaw = Quaternion.LookRotation(flat.normalized, Vector3.up).eulerAngles.y;
            if (Mathf.Abs(Mathf.DeltaAngle(_facingYaw, yaw)) < 0.25f)
                return;

            _facingYaw = yaw;
        }

        private void ApplyRotatableLook() {
            if (_rotatablePart == null)
                return;

            float yaw = _hasRemoteLook && ControlsSelf == false ? _remoteLookYaw : _facingYaw;
            Quaternion target = Quaternion.Euler(0f, yaw, 0f);
            float t = 1f - Mathf.Exp(-ReadStat(PlayerStatType.LookTurnRate) * Time.deltaTime);
            _rotatablePart.rotation = Quaternion.Slerp(_rotatablePart.rotation, target, t);
        }

        private static Vector3 Flatten(Vector3 velocity) {
            return new Vector3(velocity.x, 0f, velocity.z);
        }

        private bool HasSupport() {
            return _floatingController != null && _floatingController.HasSupport;
        }

        private float ReadStat(PlayerStatType type) =>
            _stats.Read(type);

        private int ExtraAirJumps =>
            Mathf.RoundToInt(ReadStat(PlayerStatType.ExtraAirJumps));

        private void CacheWeight() {
            if (_rb != null)
                _weight = Physics.gravity * _rb.mass;
        }
    }
}
