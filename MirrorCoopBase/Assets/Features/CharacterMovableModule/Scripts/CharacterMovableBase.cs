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
        private float _moveSpeed;
        private readonly JumpAndAirGravity _jump = new JumpAndAirGravity();
        private Vector3 _weight;
        private int _shoveMask;

        public Rigidbody Body => _rb;

        private bool ControlsSelf => isOwned;

        protected virtual void Awake() {
            CacheWeight();
            CacheShoveMask();
        }

        public override void OnStartLocalPlayer() {
            _moveSpeed = ReadStat(PlayerStatType.WalkSpeed);
            _jump.RefillAirJumps(ExtraAirJumps);
            CacheWeight();
        }

        private void Update() {
            if (ControlsSelf == false)
                return;

            _jump.ReadInput(_input.JumpHeld, HasSupport(), Time.deltaTime);
        }

        private void LateUpdate() {
            if (ControlsSelf && _input.IsBlocked == false)
                CaptureFacingYaw();

            ApplyRotatableLook();
        }

        private void FixedUpdate() {
            if (_rb == null)
                return;

            // The server copy of a client-owned player is kinematic (its NetworkRigidbody follows the owner), so it
            // never pushes by contact: the proxy shove has to run for it, while only the owner simulates a dynamic body.
            if (ControlsSelf == false) {
                if (isServer)
                    ShoveNearbyBodies();
                return;
            }

            if (_rb.isKinematic) {
                StopPushing();
                return;
            }

            SimulateOwner();
        }

        // A kinematic owner (bound to the ship) does not simulate: the last walk would stay in the synced push and the
        // server would keep shoving bodies next to a player who stands still.
        private void StopPushing() {
            _pushStrength = 0f;
            _pushSpeed = 0f;
        }

        private void SimulateOwner() {
            if (_input == null)
                return;

            Vector3 planar = Flatten(_rb.linearVelocity);
            _moveSpeed = _input.SprintHeld ? ReadStat(PlayerStatType.SprintSpeed) : ReadStat(PlayerStatType.WalkSpeed);

            Vector3 wish = ReadWishDirection();
            DrivePlanar(wish, planar);
            ApplyJumpAndAirGravity();
        }

        private Vector3 ReadWishDirection() {
            Vector2 stick = _input.MoveStick;
            Vector3 wish = _stats.CameraRelative
                ? CameraPlanar(stick)
                : new Vector3(stick.x, 0f, stick.y);

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

            _pushStrength = force.magnitude;
            _pushHeading = wish;
            _pushSpeed = steering ? _moveSpeed : 0f;
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
            }

            _jump.Land(tuning, supported, _rb.linearVelocity.y);
            if (supported)
                return;

            float extra = _jump.AirGravityExtra(tuning, _rb.linearVelocity.y);
            if (extra > 0f)
                _rb.AddForce(_weight * extra);
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

            Quaternion target = Quaternion.Euler(0f, _facingYaw, 0f);
            float t = 1f - Mathf.Exp(-ReadStat(PlayerStatType.LookTurnRate) * Time.deltaTime);
            _rotatablePart.rotation = Quaternion.Slerp(_rotatablePart.rotation, target, t);
        }

        private static Vector3 CameraPlanar(Vector2 stick) {
            Transform cameraTransform = Camera.main != null ? Camera.main.transform : null;
            if (cameraTransform == null || stick.sqrMagnitude < 0.0001f)
                return Vector3.zero;

            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;
            forward.y = 0f;
            right.y = 0f;
            if (forward.sqrMagnitude < 0.0001f || right.sqrMagnitude < 0.0001f)
                return new Vector3(stick.x, 0f, stick.y);

            Vector3 world = forward.normalized * stick.y + right.normalized * stick.x;
            if (world.sqrMagnitude > 1f)
                world.Normalize();

            return world;
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

        private void CacheWeight() {
            if (_rb != null)
                _weight = Physics.gravity * _rb.mass;
        }
    }
}
