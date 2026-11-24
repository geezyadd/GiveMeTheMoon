using Features.CameraModule.Scripts.Models;
using Features.FloatingControllerModule;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.MovingPlatformModule {
    // Lets a body ride a moving carrier. The player's own body boards when the float probe stands on a carrier and stays
    // on it in the air while above it (a jump keeps up with the platform). A simulated copy of someone else's body rides
    // the carrier its owner's state packet names (set by the copy driver), so it agrees with its owner.
    // The carry goes through the velocity, so the physics moves the body and its interpolation stays smooth: at the end
    // of each physics step (CarrierRiderStepEnd) the carrier's exact move of the body's point in this step, over the
    // step time, is added to the velocity, and the relative velocity is turned with the carrier; at the start of the
    // next step (here, before the spring and the movement) that addition is taken off again. In between the velocity is
    // relative to the carrier, so walking, braking and jumping work as on the ground. Boarding takes the carrier's speed
    // off the body; leaving gives it back, so the body flies on with the platform's speed. The local player's camera
    // turns with the carrier as it is drawn each frame.
    [DefaultExecutionOrder(-150)]
    public sealed class CarrierRider : MonoBehaviour, ICarrierRider {
        // A carrier that moves farther than this in one step jumped; the body is moved with it at once.
        private const float MAX_CARRY_PER_STEP = 5f;

        [SerializeField] private Rigidbody _body;
        [SerializeField] private FloatingController _floatingController;
        [SerializeField] private NetworkIdentity _identity;

        private CameraCarryTurnModel _cameraCarryTurnModel;
        private ICarrier _carrier;
        private ICarrier _copyCarrier;
        private bool _isCopy;
        private Vector3 _addedCarry;
        private bool _hasDrawnYaw;
        private float _lastDrawnYaw;

        public ICarrier Carrier => _carrier;

        public void RideAsCopy(bool isCopy) {
            _isCopy = isCopy;
            if (isCopy == false)
                _copyCarrier = null;
        }

        public void SetCopyCarrier(ICarrier carrier) =>
            _copyCarrier = carrier;

        [Inject]
        private void Construct(CameraCarryTurnModel cameraCarryTurnModel) =>
            _cameraCarryTurnModel = cameraCarryTurnModel;

        private void FixedUpdate() {
            bool isSimulated = _identity.isOwned || _isCopy;
            if (isSimulated == false || _body.isKinematic) {
                _carrier = null;
                _addedCarry = Vector3.zero;
                return;
            }

            _body.linearVelocity -= _addedCarry;
            _addedCarry = Vector3.zero;

            ICarrier wanted = _identity.isOwned ? PickOwnCarrier() : _copyCarrier;
            if (wanted == _carrier)
                return;

            if (_carrier != null)
                Leave();
            if (wanted != null)
                Board(wanted);
        }

        // What the player's own body stands on, or the carrier it jumped from while it is still above it.
        private ICarrier PickOwnCarrier() {
            ICarrier support = _floatingController.SupportCarrier;
            if (support != null)
                return support;

            return _carrier != null && _carrier.Holds(_body.position) ? _carrier : null;
        }

        // At the end of the physics step: the carrier's move of the body for this step, as velocity.
        internal void AddCarry() {
            if (_carrier == null || _body.isKinematic)
                return;

            Pose before = _carrier.PoseBefore;
            Pose now = _carrier.PoseNow;
            Vector3 position = _body.position;
            Vector3 move = CarrierMotion.CarryPoint(before, now, position) - position;
            // A carrier that jumped (a long stall closed at once) takes the body along at once: carried by velocity, it
            // would sweep through everything on the way.
            if (move.sqrMagnitude > MAX_CARRY_PER_STEP * MAX_CARRY_PER_STEP) {
                _body.position += move;
                _body.linearVelocity = CarrierMotion.TurnOf(before, now) * _body.linearVelocity;
                return;
            }

            _addedCarry = move / Time.fixedDeltaTime;
            _body.linearVelocity = CarrierMotion.TurnOf(before, now) * _body.linearVelocity + _addedCarry;
        }

        private void Update() {
            if (_carrier == null || _identity.isLocalPlayer == false || _cameraCarryTurnModel == null) {
                _hasDrawnYaw = false;
                return;
            }

            float yaw = _carrier.DrawnRotation.eulerAngles.y;
            if (_hasDrawnYaw)
                _cameraCarryTurnModel.AddTurn(Mathf.DeltaAngle(_lastDrawnYaw, yaw));

            _lastDrawnYaw = yaw;
            _hasDrawnYaw = true;
        }

        private void Board(ICarrier carrier) {
            _carrier = carrier;
            _hasDrawnYaw = false;
            _body.linearVelocity -= CarrierPointVelocity();
        }

        private void Leave() {
            _body.linearVelocity += CarrierPointVelocity();
            _carrier = null;
        }

        private Vector3 CarrierPointVelocity() =>
            CarrierMotion.PointVelocity(_carrier.PoseBefore, _carrier.PoseNow, _body.position, Time.fixedDeltaTime);
    }
}
