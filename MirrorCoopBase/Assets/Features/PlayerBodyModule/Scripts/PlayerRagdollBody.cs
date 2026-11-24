using Features.ActiveRagdollModule;
using Features.CameraModule.Scripts.Models;
using Features.CharacterMovableModule.Scripts;
using Features.CharacterMovableModule.Scripts.MovementControls;
using Features.FloatingControllerModule;
using UnityEngine;
using Zenject;

namespace Features.PlayerBodyModule {
    // The player's own movement walks with the ragdoll instead of the capsule. Asleep the ragdoll is a kinematic,
    // collision-free part of the model inside the capsule. Awake it leaves the capsule (physical limbs cannot live
    // inside a moving rigidbody), the movement pushes it, and the capsule turns kinematic and follows its root limb,
    // so the camera, the network and everything else that looks at the capsule keep working.
    // The owner's body is awake; other players' copies stay asleep capsules for now.
    public sealed class PlayerRagdollBody : MonoBehaviour {
        private const string ForwardParameter = "Forward";
        private const float AnimatorDamping = 0.125f;
        private const float IdleCutoff = 0.125f;

        [SerializeField] private CharacterMovableBase _movable;
        [SerializeField] private Rigidbody _capsule;
        [SerializeField] private Collider _capsuleCollider;
        [SerializeField] private FloatingController _floatingController;
        [SerializeField] private RagdollBody _ragdoll;

        private RagdollBodyParts _parts;
        private RagdollMovableBody _movableBody;
        private Transform _home;
        private Vector3 _homeLocalPosition;
        private Quaternion _homeLocalRotation;
        private Vector3 _homeLocalScale;
        private Vector3 _capsuleOffset;
        private bool _isAwake;
        private CameraCarryTurnModel _cameraCarryTurnModel;
        private bool _hasDrawnYaw;
        private float _lastDrawnYaw;

        [Inject]
        private void Construct(CameraCarryTurnModel cameraCarryTurnModel) =>
            _cameraCarryTurnModel = cameraCarryTurnModel;

        private void Awake() {
            _parts = new RagdollBodyParts(_ragdoll);
            _movableBody = new RagdollMovableBody(_ragdoll, _parts.Ground);
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

        private void OnDestroy() {
            // Awake, the ragdoll lives outside the player and would stay behind.
            if (_isAwake && _ragdoll != null)
                Destroy(_ragdoll.gameObject);
        }

        private void Update() {
            bool wanted = _movable.isOwned;
            if (wanted != _isAwake) {
                if (wanted)
                    WakeUp();
                else
                    FallAsleep();
            }

            if (_isAwake)
                DriveAnimation();
            TurnCameraWithCarrier();
        }

        // The local player's view turns with the carrier the ragdoll rides, as the carrier is drawn each frame.
        private void TurnCameraWithCarrier() {
            ICarrier carrier = _isAwake && _parts.Ride != null ? _parts.Ride.Carrier : null;
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

            // Others (a ride, a seat) may turn the capsule dynamic again; while awake it only follows.
            _capsule.isKinematic = true;
            _capsule.MovePosition(_ragdoll.Root.Rigidbody.position + _capsuleOffset);
        }

        private void WakeUp() {
            Vector3 velocity = _capsule.linearVelocity;
            _capsuleOffset = _capsule.position - _ragdoll.Root.Rigidbody.position;

            _parts.Animator.enabled = true;
            _ragdoll.transform.SetParent(null, true);
            _ragdoll.SetInterpolation(true);
            _ragdoll.SetKinematic(false);
            _ragdoll.SetCollisions(true);
            _ragdoll.IgnoreCollisionsWith(_capsuleCollider, true);
            _ragdoll.StopAllMotion();
            _ragdoll.AddVelocityChangeToAll(velocity);
            _parts.SetRunning(true);
            _parts.HandLocomotionToOwner();

            _capsule.isKinematic = true;
            _capsuleCollider.enabled = false;
            _floatingController.HoverEnabled = false;
            _movable.SetDrivenBody(_movableBody);
            _isAwake = true;
        }

        private void FallAsleep() {
            Vector3 velocity = _ragdoll.AverageVelocity;
            _movable.SetDrivenBody(null);
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

            _capsuleCollider.enabled = true;
            _capsule.isKinematic = false;
            _capsule.linearVelocity = velocity;
            _floatingController.HoverEnabled = true;
            _isAwake = false;
        }

        private void DriveAnimation() {
            _parts.Sampler.FacingYaw = _movable.LookYaw;
            // From the controls, not the measured speed: on a moving, rocking carrier the body always drifts a little
            // and would shuffle. Small values are cut so the idle stays still.
            IMovementControls controls = _movable.OwnInput;
            float forward = controls != null ? Mathf.Clamp01(controls.ReadWish().magnitude) : 0f;
            if (forward < IdleCutoff)
                forward = 0f;
            _parts.Animator.SetFloat(ForwardParameter, forward, AnimatorDamping, Time.deltaTime);
        }
    }
}
