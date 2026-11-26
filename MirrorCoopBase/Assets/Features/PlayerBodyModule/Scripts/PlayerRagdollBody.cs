using Features.ActiveRagdollModule;
using Features.CameraModule.Scripts.Models;
using Features.CharacterMovableModule.Scripts;
using Features.CharacterMovableModule.Scripts.Drift;
using Features.FloatingControllerModule;
using Features.GameCoreModule.Contracts;
using Features.PlayerReplicaModule;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace Features.PlayerBodyModule {
    // The player's own movement walks with the ragdoll instead of the capsule. Asleep the ragdoll is a kinematic,
    // collision-free part of the model inside the capsule. Awake it leaves the capsule (physical limbs cannot live
    // inside a moving rigidbody), the movement pushes it, and the capsule turns kinematic, without its colliders, and
    // follows its root limb, so the camera, the network and everything else that looks at the capsule keep working.
    // The owner's body is awake, and so is a copy of another player's body while this peer simulates it from that
    // player's packets; otherwise (the old network body) it stays asleep. A suspended body (a dead player) sleeps and
    // is not drawn.
    // While a ride or a seat holds the player, the capsule leads instead: the ragdoll stands where the capsule is.
    // Awake, the ragdoll lives in the persistent scene like the players (the player destroys it with itself), and a
    // teleport of the capsule (a spawn, a map change) takes it along. Runs before everything that reads the body this
    // step (the movement, the network), after the carriers have taken this step's pose.
    [DefaultExecutionOrder(-190)]
    public sealed class PlayerRagdollBody : MonoBehaviour {
        // While the ragdoll leads, only it moves the capsule, and the capsule ends each step exactly where it was put.
        // Found farther than this from there, someone else moved it (a respawn, a map change, a world shift).
        private const float TeleportDistance = 0.01f;

        [SerializeField] private CharacterMovableBase _movable;
        [SerializeField] private Rigidbody _capsule;
        [SerializeField] private FloatingController _floatingController;
        [SerializeField] private RagdollBody _ragdoll;
        [SerializeField] private BodyCopyDriver _copyDriver;
        [SerializeField] private CharacterDrift _drift;

        private readonly CharacterDriftHeadFirstTurn _headFirstTurn = new();
        private RagdollBodyParts _parts;
        private RagdollMovableBody _movableBody;
        private CapsuleColliders _capsuleColliders;
        private Renderer[] _modelRenderers;
        private Transform _home;
        private Vector3 _homeLocalPosition;
        private Quaternion _homeLocalRotation;
        private Vector3 _homeLocalScale;
        private Vector3 _capsuleOffset;
        private Vector3 _lastPlacedCapsule;
        private bool _isAwake;
        private bool _isSuspended;
        private bool _wasHeld;
        private CameraCarryTurnModel _cameraCarryTurnModel;
        private bool _hasDrawnYaw;
        private float _lastDrawnYaw;

        [Inject]
        private void Construct(CameraCarryTurnModel cameraCarryTurnModel) =>
            _cameraCarryTurnModel = cameraCarryTurnModel;

        private void Awake() {
            _parts = new RagdollBodyParts(_ragdoll);
            _movableBody = new RagdollMovableBody(_ragdoll, _parts.Ground, _parts.Ride);
            _capsuleColliders = new CapsuleColliders(_capsule.transform, _ragdoll.transform);
            _modelRenderers = _ragdoll.GetComponentsInChildren<Renderer>(true);
            Transform ragdollTransform = _ragdoll.transform;
            _home = ragdollTransform.parent;
            _homeLocalPosition = ragdollTransform.localPosition;
            _homeLocalRotation = ragdollTransform.localRotation;
            _homeLocalScale = ragdollTransform.localScale;
            _parts.SetRunning(false);
            _ragdoll.SetKinematic(true);
            _ragdoll.SetCollisions(false);
            _ragdoll.SetInterpolation(false);
        }

        // A player object that is turned off (not destroyed) must not leave its ragdoll standing in the world.
        private void OnEnable() {
            if (_isAwake && _ragdoll != null)
                _ragdoll.gameObject.SetActive(true);
        }

        private void OnDisable() {
            if (_isAwake && _ragdoll != null)
                _ragdoll.gameObject.SetActive(false);
        }

        private void OnDestroy() {
            // Awake, the ragdoll lives outside the player and would stay behind.
            if (_isAwake && _ragdoll != null)
                Destroy(_ragdoll.gameObject);
        }

        // A suspended body (a dead player) sleeps inside the capsule and is not drawn. It falls asleep at once, so the
        // caller can set up the capsule (colliders, freezing) right after without the sleep undoing it.
        public void SetSuspended(bool isSuspended) {
            _isSuspended = isSuspended;
            if (isSuspended && _isAwake && _ragdoll != null)
                FallAsleep();
            foreach (Renderer modelRenderer in _modelRenderers)
                if (modelRenderer != null)
                    modelRenderer.enabled = isSuspended == false;
        }

        private void Update() {
            if (_isAwake && _ragdoll == null)
                LoseRagdoll();

            bool simulated = _movable.isOwned || (_copyDriver != null && _copyDriver.IsSimulating);
            bool wanted = simulated && _isSuspended == false && _ragdoll != null;
            if (wanted != _isAwake) {
                if (wanted)
                    WakeUp();
                else
                    FallAsleep();
            }

            if (_isAwake)
                DrivePose();
            TurnCameraWithCarrier();
        }

        // The local player's view turns with the carrier the ragdoll rides, as the carrier is drawn each frame. A held
        // player's capsule is turned by its ride, and the view with it, so nothing is added then. A carrier destroyed
        // under the ride (the ship's scene unloaded when the crew goes back to the lobby) counts as none: through the
        // interface a destroyed object is not null.
        private void TurnCameraWithCarrier() {
            bool riding = _isAwake && _movable.IsHeld == false && _parts.Ride != null;
            ICarrier carrier = riding ? _parts.Ride.Carrier : null;
            if (carrier is Object carrierObject && carrierObject == null)
                carrier = null;
            if (carrier == null || _movable.isLocalPlayer == false || _cameraCarryTurnModel == null) {
                _hasDrawnYaw = false;
                return;
            }

            float yaw = carrier.DrawnRotation.eulerAngles.y;
            if (_hasDrawnYaw)
                _cameraCarryTurnModel.AddTurn(Mathf.DeltaAngle(_lastDrawnYaw, yaw));

            _lastDrawnYaw = yaw;
            _hasDrawnYaw = true;
        }

        private void FixedUpdate() {
            if (_isAwake == false)
                return;

            if (_ragdoll == null) {
                LoseRagdoll();
                return;
            }

            _capsuleColliders.KeepOff();
            // A seat or a ride holds the player: the ragdoll is put on the seat's spot once, then stands there carried
            // by the ship like anyone on the deck (it does not walk while held), gently pulled back so it never drifts
            // off.
            if (_movable.IsHeld) {
                if (_wasHeld == false)
                    StepOntoHeldSpot();
                else
                    PullTowardHeldSpot();
                _wasHeld = true;
                return;
            }

            if (_wasHeld)
                _lastPlacedCapsule = _capsule.position;
            _wasHeld = false;

            FollowCapsuleTeleport();
            // Others may turn the capsule dynamic again; while the ragdoll leads it only follows.
            _capsule.isKinematic = true;
            _lastPlacedCapsule = _ragdoll.Root.Rigidbody.position + _capsuleOffset;
            _capsule.MovePosition(_lastPlacedCapsule);
        }

        // The seat places the capsule once a frame on the carrier as drawn, the ragdoll is carried once a physics step:
        // the spot is read in the drawn pose and put in the step's pose the ragdoll stands in now. Only horizontally:
        // the ragdoll stands at its own height (no sitting).
        private void StepOntoHeldSpot() =>
            _ragdoll.MoveAllBy(HeldSpotOffset(out _, out _));

        // A soft spring with damping toward the held spot, in the carrier's plane, as a push for this step: the body
        // may sway a little but keeps coming back. Its speed is taken relative to the carrier it rides.
        private void PullTowardHeldSpot() {
            RagdollStandSettings settings = _ragdoll.Profile.Stand;
            Vector3 offset = HeldSpotOffset(out ICarrier carrier, out Vector3 up);
            Vector3 velocity = _ragdoll.AverageVelocity;
            if (carrier != null)
                velocity -= CarrierMotion.PointVelocity(carrier.PoseBefore, carrier.PoseNow,
                    _ragdoll.Root.Rigidbody.position, Time.fixedDeltaTime);
            velocity = Vector3.ProjectOnPlane(velocity, up);

            Vector3 pull = offset.magnitude > settings.HeldSpotTolerance
                ? offset * settings.HeldSpotSpring
                : Vector3.zero;
            _ragdoll.AddAccelerationToAll(pull - velocity * settings.HeldSpotDamping);
        }

        // From the root limb to the held spot, in the carrier's plane. The seat places the capsule once a frame on
        // the carrier as drawn, while the ragdoll is carried once a physics step: the spot is read in the drawn pose
        // and put in the step's pose the ragdoll stands in now.
        private Vector3 HeldSpotOffset(out ICarrier carrier, out Vector3 up) {
            Vector3 spot = _capsule.position - _capsuleOffset;
            up = Vector3.up;
            carrier = _parts.Ride != null ? _parts.Ride.Carrier : null;
            if (carrier != null) {
                Pose drawn = new(carrier.DrawnPosition, carrier.DrawnRotation);
                spot = CarrierMotion.ToWorldPoint(carrier.PoseBefore, CarrierMotion.ToLocalPoint(drawn, spot));
                up = carrier.PoseBefore.rotation * Vector3.up;
            }

            return Vector3.ProjectOnPlane(spot - _ragdoll.Root.Rigidbody.position, up);
        }

        private void FollowCapsuleTeleport() {
            Vector3 moved = _capsule.position - _lastPlacedCapsule;
            if (moved.sqrMagnitude <= TeleportDistance * TeleportDistance)
                return;

            _ragdoll.MoveAllBy(moved);
            _ragdoll.StopAllMotion();
        }

        // A scene root of its own now: it goes where the players live, so a map unload does not take it.
        private static void MoveToPersistentScene(GameObject ragdoll) {
            Scene persistent = SceneManager.GetSceneByName(SceneNames.Global);
            if (persistent.IsValid() && persistent.isLoaded)
                SceneManager.MoveGameObjectToScene(ragdoll, persistent);
            else
                DontDestroyOnLoad(ragdoll);
        }

        // The ragdoll was destroyed under a live player (it should not be): the capsule takes over again for good.
        private void LoseRagdoll() {
            _movable.SetDrivenBody(null);
            _capsuleColliders.Restore();
            _capsule.isKinematic = false;
            _floatingController.HoverEnabled = true;
            _isAwake = false;
            Debug.LogWarning($"Ragdoll: the body of {name} was lost; it walks as a capsule now.", this);
        }

        private void WakeUp() {
            Vector3 velocity = _capsule.isKinematic ? Vector3.zero : _capsule.linearVelocity;
            _capsuleOffset = _capsule.position - _ragdoll.Root.Rigidbody.position;

            _parts.Animator.enabled = true;
            _ragdoll.transform.SetParent(null, true);
            MoveToPersistentScene(_ragdoll.gameObject);
            _lastPlacedCapsule = _capsule.position;
            _wasHeld = false;
            _ragdoll.SetInterpolation(true);
            _ragdoll.SetKinematic(false);
            _ragdoll.SetCollisions(true);
            _ragdoll.StopAllMotion();
            _ragdoll.AddVelocityChangeToAll(velocity);
            _parts.SetRunning(true);
            _parts.HandLocomotionToOwner();

            _capsuleColliders.TurnOff();
            if (_movable.IsHeld == false)
                _capsule.isKinematic = true;
            _floatingController.HoverEnabled = false;
            _movable.SetDrivenBody(_movableBody);
            _isAwake = true;
        }

        private void FallAsleep() {
            Vector3 velocity = _ragdoll.AverageVelocity;
            _movable.SetDrivenBody(null);
            _headFirstTurn.Reset();
            _parts.Sampler.Tilt = Quaternion.identity;
            _parts.SetRunning(false);
            _ragdoll.SetKinematic(true);
            _ragdoll.SetCollisions(false);
            _ragdoll.SetInterpolation(false);

            Transform ragdollTransform = _ragdoll.transform;
            ragdollTransform.SetParent(_home, false);
            ragdollTransform.localPosition = _homeLocalPosition;
            ragdollTransform.localRotation = _homeLocalRotation;
            ragdollTransform.localScale = _homeLocalScale;
            _parts.ResetPose();

            _capsuleColliders.Restore();
            _isAwake = false;
            // A ride or a seat keeps holding the capsule; only a free capsule walks on by itself.
            if (_movable.IsHeld)
                return;

            _capsule.isKinematic = false;
            _capsule.linearVelocity = velocity;
            _floatingController.HoverEnabled = true;
        }

        // How the pose is turned: toward the shown look, and head first while drifting. The animator's parameters are
        // PlayerAnimationModule's (PlayerAnimationDriver).
        private void DrivePose() {
            _parts.Sampler.FacingYaw = _movable.ShownLookYaw;
            TurnHeadFirstWhileDrifting();
        }

        // A drifting body turns smoothly to fly head first; caught by a carrier or back on its feet it stands upright
        // again. Each peer shows it on its own from the body's velocity.
        private void TurnHeadFirstWhileDrifting() {
            bool isFlying = _drift.IsDrifting && (_parts.Ride == null || _parts.Ride.Carrier == null);
            _parts.Sampler.Tilt = _headFirstTurn.Step(isFlying, _ragdoll.AverageVelocity, _drift.Settings, Time.deltaTime);
        }
    }
}
