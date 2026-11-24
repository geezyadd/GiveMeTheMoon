using Features.CharacterMovableModule.Scripts.Body;
using Features.CharacterMovableModule.Scripts.MovementControls;
using Features.CharacterMovableModule.Scripts.PlayerStats;
using Features.FloatingControllerModule;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.CharacterMovableModule.Scripts {
    public abstract class CharacterMovableBase : NetworkBehaviour {
        private const int LAYER_COUNT = 32;

        [Header("Body")]
        [SerializeField] private Rigidbody _rb;
        [SerializeField] private CapsuleCollider _capsuleCollider;
        [SerializeField] private FloatingController _floatingController;
        [SerializeField] private Transform _rotatablePart;
        [SerializeField] private PlayerStatEntity _stats;

        [Header("Proxy Shove")]
        [SerializeField] private float _proxyReach = 0.2f;
        [SerializeField] private float _proxyPadding = 0.1f;

        [SyncVar] private float _pushStrength;
        [SyncVar] private Vector3 _pushHeading;
        [SyncVar] private float _pushSpeed;
        [SyncVar] private float _facingYaw;

        private static readonly Collider[] OverlapScratch = new Collider[32];

        [Inject]
        private CharacterInputBuffer _input;
        private IMovementControls _ownInput;
        private IMovementControls _remoteInput;
        private bool _hasRemoteLook;
        private float _remoteLookYaw;
        private float _moveSpeed;
        private readonly JumpAndAirGravity _jump = new JumpAndAirGravity();
        private int _shoveMask;
        private IMovableBody _capsuleBody;
        private IMovableBody _drivenBody;

        public Rigidbody Body => _rb;

        // What the movement pushes: the capsule, or another body put in by SetDrivenBody.
        private IMovableBody DrivenBody =>
            _drivenBody ?? (_capsuleBody ??= new CapsuleMovableBody(_rb, _floatingController));

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

        protected virtual void Awake() {
            CacheShoveMask();
        }

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

            // The server copy of a client-owned player that is not simulated is kinematic (its NetworkRigidbody follows the
            // owner), so it never pushes by contact: the proxy shove has to run for it. A simulated copy pushes by contact.
            IMovementControls source = ActiveInput;
            if (source == null) {
                if (isServer && ControlsSelf == false)
                    ShoveNearbyBodies();
                return;
            }

            if (DrivenBody.IsKinematic) {
                if (ControlsSelf)
                    StopPushing();
                return;
            }

            Simulate(source);
        }

        // A kinematic owner (bound to the ship) does not simulate: the last walk would stay in the synced push and the
        // server would keep shoving bodies next to a player who stands still.
        private void StopPushing() {
            _pushStrength = 0f;
            _pushSpeed = 0f;
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

            // The push is the owner's to sync; a remote copy only moves its own body.
            if (ControlsSelf == false)
                return;

            _pushStrength = drive.magnitude * DrivenBody.Mass;
            _pushHeading = wish;
            _pushSpeed = steering ? _moveSpeed : 0f;
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

        private void ShoveNearbyBodies() {
            // Gated on the push intent, not on speed: a client walking into an item is stopped by its own copy of it,
            // so its speed drops to zero exactly while it pushes.
            if (_pushHeading.sqrMagnitude < 0.0001f || _pushStrength < 0.01f || _capsuleCollider == null)
                return;

            Vector3 shove = _pushHeading * Mathf.Max(_pushStrength, 0.1f);
            Vector3 shoveDirection = shove.normalized;
            Vector3 center = _capsuleCollider.transform.TransformPoint(_capsuleCollider.center);
            float radius = _capsuleCollider.radius;
            float height = _capsuleCollider.height;
            Vector3 up = _capsuleCollider.transform.up;
            Vector3 lead = Vector3.ClampMagnitude(shove, 1f) * _proxyReach;
            if (connectionToClient != null)
                lead += lead.normalized * (float)connectionToClient.rtt * 0.5f;

            Vector3 origin = center + lead;
            Vector3 top = origin + up * (height * 0.5f - radius);
            Vector3 bottom = origin - up * (height * 0.5f - radius);
            int count = Physics.OverlapCapsuleNonAlloc(
                top,
                bottom,
                radius + _proxyPadding,
                OverlapScratch,
                _shoveMask,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++) {
                Rigidbody other = OverlapScratch[i].attachedRigidbody;
                if (other == null || other.isKinematic || other == _rb || other.TryGetComponent(out CharacterMovableBase _))
                    continue;

                Vector3 contact = OverlapScratch[i].ClosestPoint(center);
                Vector3 away = (contact - center).normalized;
                if (Vector3.Dot(away, shoveDirection) <= 0.1f)
                    continue;

                // Bounded like a body contact: the drive force is shared by player and item by mass, and the item is never
                // pushed faster than the player moves.
                Vector3 impulse = Vector3.Project(shove, away) * (other.mass / (other.mass + _rb.mass));
                if (Vector3.Dot(other.linearVelocity, shoveDirection) < _pushSpeed)
                    other.AddForceAtPosition(impulse, contact);
            }
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
            return DrivenBody.HasSupport;
        }

        private float ReadStat(PlayerStatType type) =>
            _stats.Read(type);

        private int ExtraAirJumps =>
            Mathf.RoundToInt(ReadStat(PlayerStatType.ExtraAirJumps));

        // The shove stands in for the body contact of a client-owned player, so it reaches every layer that body collides
        // with (items lie on Interactable), not only the ground the float probe stands on. Players never shove players.
        private void CacheShoveMask() {
            int layer = _capsuleCollider != null ? _capsuleCollider.gameObject.layer : gameObject.layer;
            _shoveMask = 0;
            for (int other = 0; other < LAYER_COUNT; other++) {
                if (Physics.GetIgnoreLayerCollision(layer, other) == false)
                    _shoveMask |= 1 << other;
            }
        }
    }
}
