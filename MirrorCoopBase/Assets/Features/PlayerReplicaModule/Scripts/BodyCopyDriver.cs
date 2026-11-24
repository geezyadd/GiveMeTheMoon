using Features.PlayerReplicaModule.Configuration;
using Features.PlayerReplicaModule.Correction;
using Features.PlayerReplicaModule.Models;
using Features.PlayerReplicaModule.Packets;
using Features.PlayerReplicaModule.Receiving;
using Features.PlayerReplicaModule.Visual;
using Features.CharacterMovableModule.Scripts;
using Features.FloatingControllerModule;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.PlayerReplicaModule {
    // On a peer that does not own this player, while the sync mode says so: turns the player into a simulated copy.
    // The old network body steps aside, the body becomes dynamic, and the float spring and the movement run on it from
    // the owner's controls. The copy lives a little in the past (the copy delay): each packet becomes current when the
    // show time reaches its stamp, so the controls and the position the copy is pulled to belong to the same moment
    // and a copy stops where its owner stopped. After each physics step it is pulled toward the current packet: a share
    // of the position error (gentle, with a dead zone, while the owner presses no walk key), the velocity reconciled
    // once per packet, a snap past the snap distance. The target is moved ahead along the packet's velocity only while
    // the owner walks. The look follows the current packet too. A jump of the body is hidden by ReplicaVisualLag. While a
    // ride or a seat holds the body (kinematic), the copy leaves it alone.
    // A packet sent on a carrier (a moving platform) is read in this peer's copy of that carrier (CopyCarrierFrame): the
    // copy rides it (ICarrierRider), its target is the owner's spot on it, and the walk and look follow its heading.
    // Runs after the float probe and before the movement (execution order): a packet's controls and the reconciled
    // velocity are in place before the movement computes this step's drive, so a stop reaches the copy on the very
    // step it is due. All of it happens before the physics step moves the body.
    [DefaultExecutionOrder(-50)]
    public sealed class BodyCopyDriver : MonoBehaviour {
        [SerializeField] private BodyStateChannel _channel;
        [SerializeField] private CharacterMovableBase _movable;
        [SerializeField] private FloatingController _floatingController;
        [SerializeField] private NetworkRigidbodyUnreliable _networkBody;
        [SerializeField] private ReplicaVisualLag _visualLag;

        private readonly PacketMovementControls _input = new();
        private readonly PacketDelayBuffer _delayBuffer = new();
        private readonly GroundMismatchTimer _groundTimer = new();
        private readonly CopyCarrierFrame _carrierFrame = new();

        private ICarrierRider _rider;
        private ReplicaConfiguration _configuration;
        private ReplicaModeModel _modeModel;
        private bool _isDriving;
        private bool _isSimulating;
        private bool _hasReconciled;
        private ushort _reconciledSequence;

        [Inject]
        private void Construct(ReplicaConfiguration configuration, ReplicaModeModel modeModel) {
            _configuration = configuration;
            _modeModel = modeModel;
        }

        private void Awake() =>
            _rider = GetComponent<ICarrierRider>();

        private void OnEnable() =>
            _channel.OnPacketAccepted += OnPacketAccepted;

        private void OnDisable() =>
            _channel.OnPacketAccepted -= OnPacketAccepted;

        private void FixedUpdate() {
            if (_channel.isOwned || _configuration == null || _modeModel == null)
                return;

            Rigidbody body = _movable.Body;
            if (_modeModel.SimulatesCopies == false || _channel.HasLatest == false) {
                if (_isDriving && body.isKinematic == false)
                    Release();
                return;
            }

            if (_isDriving == false && TryTakeOver(body) == false)
                return;

            // A ride that ended gives the old network body back; the copy keeps it silent.
            if (_networkBody != null && _networkBody.enabled)
                _networkBody.enabled = false;

            double showTime = BodyStateChannel.ReadServerNow() - _configuration.CopyDelaySeconds;
            if (_delayBuffer.Advance(showTime))
                ApplyCurrent();

            FollowCarrier();

            if (body.isKinematic) {
                StopSimulating();
                return;
            }

            StartSimulating();
            if (_carrierFrame.IsMissing == false)
                Correct(body, showTime);
        }

        // The carrier of the current packet: the copy rides it, and its controls and look follow its heading this step.
        private void FollowCarrier() {
            BodyStatePacket packet = _delayBuffer.Current;
            _carrierFrame.Resolve(packet.CarrierId);
            _rider?.SetCopyCarrier(_carrierFrame.Carrier);
            Quaternion heading = _carrierFrame.Heading;
            _input.SetHeading(heading);
            _movable.SetRemoteLook(YawCompression.Unpack(packet.LookYaw) + heading.eulerAngles.y);
        }

        private void ApplyCurrent() {
            BodyStatePacket packet = _delayBuffer.Current;
            _input.Apply(packet);
        }

        private void OnPacketAccepted(BodyStatePacket packet) {
            if (_isDriving)
                _delayBuffer.Add(packet);
        }

        // The old network body makes a copy kinematic; a ride or a seat does too, but silences that body first. Only the
        // old network body's hold is taken over: a ridden copy is waited for.
        private bool TryTakeOver(Rigidbody body) {
            bool heldByRideOrSeat = body.isKinematic && (_networkBody == null || _networkBody.enabled == false);
            if (heldByRideOrSeat)
                return false;

            if (_networkBody != null)
                _networkBody.enabled = false;

            body.isKinematic = false;
            BodyStatePacket packet = _channel.Latest;
            _delayBuffer.Restart(packet);
            _input.Reset();
            ApplyCurrent();
            _hasReconciled = false;
            _isDriving = true;
            _carrierFrame.Resolve(packet.CarrierId);
            Snap(body, _carrierFrame.ToWorldPoint(packet.RelativePosition), _carrierFrame.ToWorldDirection(packet.RelativeVelocity), false);
            return true;
        }

        private void Release() {
            StopSimulating();
            if (_networkBody != null)
                _networkBody.enabled = true;

            _delayBuffer.Clear();
            _movable.ClearRemoteLook();
            _isDriving = false;
        }

        private void StartSimulating() {
            if (_isSimulating)
                return;

            _isSimulating = true;
            _groundTimer.Reset();
            _floatingController.SimulatesCopy = true;
            _movable.SetRemoteInput(_input);
            _rider?.RideAsCopy(true);
        }

        private void StopSimulating() {
            if (_isSimulating == false)
                return;

            _isSimulating = false;
            _floatingController.SimulatesCopy = false;
            _movable.SetRemoteInput(null);
            _rider?.RideAsCopy(false);
        }

        private void Correct(Rigidbody body, double showTime) {
            BodyStatePacket packet = _delayBuffer.Current;
            BodyCorrectionSettings settings = _configuration.CorrectionSettings;
            // Ahead along the velocity only while the owner walks: a braking owner's velocity would carry the target
            // past the place they stop at, and the copy would be pulled back.
            bool ownerSteers = packet.WalkDirection != Vector2.zero;
            double age = ownerSteers ? showTime - packet.ServerTime : 0d;
            Vector3 localTarget = CorrectionTarget.Predict(packet.RelativePosition, packet.RelativeVelocity, age, settings.LeadLimitSeconds);
            Vector3 target = _carrierFrame.ToWorldPoint(localTarget);
            // Relative to the carrier like the copy's own velocity between the rider's steps, in world axes.
            Vector3 ownerVelocity = _carrierFrame.ToWorldDirection(packet.RelativeVelocity);

            _groundTimer.Tick(packet.Has(BodyStateFlags.Grounded), _movable.IsSupported, Time.fixedDeltaTime);
            bool isStuck = _groundTimer.IsStuck(settings.GroundMismatchSeconds);
            PositionCorrectionStep step = PositionCorrection.Compute(target - body.position, ownerSteers, isStuck, settings);
            if (step.IsSnap) {
                Snap(body, target, ownerVelocity, true);
                return;
            }

            MoveBy(body, step.Offset);
            if (_hasReconciled && packet.Number == _reconciledSequence)
                return;

            body.linearVelocity = VelocityReconciliation.Reconcile(body.linearVelocity, ownerVelocity, ownerSteers);
            _reconciledSequence = packet.Number;
            _hasReconciled = true;
        }

        // A step of the pull long enough to see (a large error) is hidden like a snap.
        private void MoveBy(Rigidbody body, Vector3 offset) {
            body.position += offset;
            if (_visualLag != null)
                _visualLag.HideJump(offset);
        }

        private void Snap(Rigidbody body, Vector3 position, Vector3 velocity, bool count) {
            if (_visualLag != null)
                _visualLag.HideJump(position - body.position);

            body.position = position;
            body.linearVelocity = velocity;
            if (count)
                _channel.ReceiveStats.RecordSnap(Time.unscaledTimeAsDouble);
        }
    }
}
