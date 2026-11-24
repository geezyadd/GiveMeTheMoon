using Features.CharacterMovableModule.Scripts.Body;
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
        private IMovableBody _capsuleBody;
        private IMovableBody _drivenBody;

        public Rigidbody Body => _rb;

        // What the movement pushes and the network copy corrects: the capsule, or another body put in by SetDrivenBody.
        public IMovableBody DrivenBody =>
            _drivenBody ?? (_capsuleBody ??= new CapsuleMovableBody(
                _rb, _floatingController, GetComponent<ICarrierRider>()));

        // The controls the body is simulated from on this peer: the keyboard of the owner, a copy's packets, or null.
        public IMovementControls ActiveControls => ActiveInput;

        // The look the body shows on this peer, degrees around the up axis: a copy's packet look while it has one.
        public float ShownLookYaw => _hasRemoteLook && ControlsSelf == false ? _remoteLookYaw : _facingYaw;

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

        // A ride or a seat holds the player in place (it moves the capsule itself): the movement does not walk the
        // body, and a body other than the capsule follows the capsule instead of leading it.
        public bool IsHeld { get; private set; }

        public void SetHeld(bool isHeld) =>
            IsHeld = isHeld;

        // Makes the movement drive another body instead of the capsule (null: the capsule again).
        public void SetDrivenBody(IMovableBody body) =>
            _drivenBody = body;

        public override void OnStartLocalPlayer() {
            _moveSpeed = ReadStat(PlayerStatType.WalkSpeed);
            _jump.RefillAirJumps(ExtraAirJumps);
        }

        // On a peer that does not own this body: simulate it as a copy from these controls (null: stop, the body is
        // moved by someone else again). The body must be dynamic for the simulation to move it.
        public void SetRemoteInput(IMovementControls source) {
            _remoteInput = source;
            if (source == null)
                return;

            _jump.RefillAirJumps(ExtraAirJumps);
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
            if (source == null || DrivenBody.IsKinematic || IsHeld)
                return;

            Simulate(source);
        }

        // One physics step of the body from its controls: walk or brake, then jump and the extra air gravity.
        private void Simulate(IMovementControls source) {
            if (source == null)
                return;

            Vector3 planar = Flatten(DrivenBody.Velocity);
            _moveSpeed = source.SprintHeld ? ReadStat(PlayerStatType.SprintSpeed) : ReadStat(PlayerStatType.WalkSpeed);

            Vector3 wish = ReadWishDirection(source);
            DrivePlanar(wish, planar);
            ApplyJumpAndAirGravity();
        }

        private Vector3 ReadWishDirection(IMovementControls source) {
            Vector3 wish = source.ReadWish();
            if (HasSupport())
                wish = Vector3.ProjectOnPlane(wish, DrivenBody.SupportNormal);

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
            Vector3 drive = Vector3.ClampMagnitude(accel, forceCap);
            DrivenBody.AddAcceleration(drive);
        }

        private void ApplyJumpAndAirGravity() {
            IMovableBody body = DrivenBody;
            if (body.SensesGround == false)
                return;

            JumpTuning tuning = JumpTuning.Read(_stats);
            bool supported = HasSupport();
            if (_jump.TryTakeoff(tuning, supported, body.Velocity.y, Physics.gravity.y, Time.fixedDeltaTime,
                    out float speedChange)) {
                body.PauseSupport();
                body.AddVelocityChange(new Vector3(0f, speedChange, 0f));
                JumpCount = unchecked((byte)(JumpCount + 1));
            }

            _jump.Land(tuning, supported, body.Velocity.y);
            if (supported)
                return;

            float extra = _jump.AirGravityExtra(tuning, body.Velocity.y);
            if (extra > 0f)
                body.AddAcceleration(Physics.gravity * extra);
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

            float yaw = ShownLookYaw;
            Quaternion target = Quaternion.Euler(0f, yaw, 0f);
            float t = 1f - Mathf.Exp(-ReadStat(PlayerStatType.LookTurnRate) * Time.deltaTime);
            _rotatablePart.rotation = Quaternion.Slerp(_rotatablePart.rotation, target, t);
        }

        private static Vector3 Flatten(Vector3 velocity) {
            return new Vector3(velocity.x, 0f, velocity.z);
        }

        private bool HasSupport() {
            return DrivenBody.HasSupport;
        }

        private float ReadStat(PlayerStatType type) =>
            _stats.Read(type);

        private int ExtraAirJumps =>
            Mathf.RoundToInt(ReadStat(PlayerStatType.ExtraAirJumps));
    }
}
