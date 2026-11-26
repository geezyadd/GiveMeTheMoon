using Features.PlayerReplicaModule.Configuration;
using Features.PlayerReplicaModule.Correction;
using Features.PlayerReplicaModule.Packets;
using Features.PlayerReplicaModule.Receiving;
using Features.PlayerReplicaModule.Visual;
using Features.CharacterMovableModule.Scripts;
using Features.CharacterMovableModule.Scripts.Body;
using Features.FloatingControllerModule;
using UnityEngine;
using Zenject;

namespace Features.PlayerReplicaModule {
    // On a peer that does not own this player: turns the player into a simulated copy. Until the first packet the body
    // is held still (kinematic) where it spawned; then it becomes dynamic, and the float spring and the movement run on
    // it from the owner's controls. The copy lives a little in the past (the copy delay): each packet becomes current
    // when the show time reaches its stamp, so the controls and the position the copy is pulled to belong to the same
    // moment and a copy stops where its owner stopped. After each physics step it is pulled toward the current packet: a
    // share of the position error (gentle, with a dead zone, while the owner presses no walk key), the velocity
    // reconciled once per packet, a snap past the snap distance. The target is moved ahead along the packet's velocity
    // only while the owner walks. The look follows the current packet too. A jump of the body is hidden by
    // ReplicaVisualLag. While a seat holds the body (kinematic, no collisions), the copy leaves it alone.
    // When the server moves the body (BodyStateChannel.OnPlaced), the copy is held at the new place until the owner's
    // first packet sent after the move (PlacementFence) and starts over from it.
    // A packet sent on a carrier (a moving platform) is read in this peer's copy of that carrier (CopyCarrierFrame): the
    // copy rides it (ICarrierRider), its target is the owner's spot on it, and the walk and look follow its heading.
    // Runs after the float probe and before the movement (execution order): a packet's controls and the reconciled
    // velocity are in place before the movement computes this step's drive, so a stop reaches the copy on the very
    // step it is due. All of it happens before the physics step moves the body.
    // The copy is read and corrected through the body the movement drives (the capsule, or the ragdoll once it wakes
    // for the copy), and rides through that body's rider; holding and taking over are done on the capsule.
    [DefaultExecutionOrder(-50)]
    public sealed class BodyCopyDriver : MonoBehaviour {
        [SerializeField] private BodyStateChannel _channel;
        [SerializeField] private CharacterMovableBase _movable;
        [SerializeField] private FloatingController _floatingController;
        [SerializeField] private ReplicaVisualLag _visualLag;

        private readonly PacketMovementControls _input = new();
        private readonly PacketDelayBuffer _delayBuffer = new();
        private readonly GroundMismatchTimer _groundTimer = new();
        private readonly CopyCarrierFrame _carrierFrame = new();
        private readonly PlacementFence _placementFence = new();

        private ICarrierRider _copyRider;
        private ReplicaConfiguration _configuration;
        private bool _isDriving;
        private bool _isSimulating;
        private bool _holdsBody;
        private bool _hasReconciled;
        private ushort _reconciledSequence;

        [Inject]
        private void Construct(ReplicaConfiguration configuration) =>
            _configuration = configuration;

        // Whether this peer simulates the body as a copy right now (the body is dynamic and driven by packets).
        public bool IsSimulating => _isSimulating;

        // The owner's attack count in the packet the copy shows now (see IAttackPresses); 0 while not driving.
        public byte ShownAttackCount => _isDriving ? _delayBuffer.Current.AttackCount : (byte)0;

        private void OnEnable() {
            _channel.OnPacketAccepted += OnPacketAccepted;
            _channel.OnPlaced += OnPlaced;
        }

        private void OnDisable() {
            _channel.OnPacketAccepted -= OnPacketAccepted;
            _channel.OnPlaced -= OnPlaced;
        }

        private void FixedUpdate() {
            if (_channel.isOwned || _configuration == null)
                return;

            Rigidbody capsule = _movable.Body;
            if (_isDriving == false) {
                if (CanTakeOver(capsule) == false) {
                    HoldStill(capsule);
                    return;
                }

                TakeOver(capsule);
            }

            double showTime = BodyStateChannel.ReadServerNow() - _configuration.CopyDelaySeconds;
            if (_delayBuffer.Advance(showTime))
                ApplyCurrent();

            FollowCarrier();

            IMovableBody body = _movable.DrivenBody;
            if (body.IsKinematic) {
                StopSimulating();
                return;
            }

            StartSimulating();
            SyncRider();
            if (_carrierFrame.IsMissing == false)
                Correct(body, showTime);
        }

        // The carrier of the current packet: the copy rides it, and its controls and look follow its heading this step.
        private void FollowCarrier() {
            BodyStatePacket packet = _delayBuffer.Current;
            _carrierFrame.Resolve(packet.CarrierId);
            _movable.DrivenBody.Rider?.SetCopyCarrier(_carrierFrame.Carrier);
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

        // A copy starts from a packet the owner sent after the last move of the body. A body held by a seat
        // (kinematic, collisions off) is waited for; one this driver holds still is taken.
        private bool CanTakeOver(Rigidbody body) {
            if (_channel.HasLatest == false)
                return false;

            if (_placementFence.Blocks(_channel.Latest.PlaceCount, Time.timeAsDouble, _configuration.PlacementWaitSeconds))
                return false;

            return body.isKinematic == false || _holdsBody && body.detectCollisions;
        }

        // Not driven yet (no packet, or waiting after a move): a dynamic body would fall or drift on its own.
        private void HoldStill(Rigidbody body) {
            if (body.isKinematic)
                return;

            body.isKinematic = true;
            _holdsBody = true;
        }

        private void TakeOver(Rigidbody body) {
            body.isKinematic = false;
            _holdsBody = false;
            BodyStatePacket packet = _channel.Latest;
            _delayBuffer.Restart(packet);
            _input.Reset();
            ApplyCurrent();
            _hasReconciled = false;
            _isDriving = true;
            _carrierFrame.Resolve(packet.CarrierId);
            Snap(_movable.DrivenBody, _carrierFrame.ToWorldPoint(packet.RelativePosition),
                _carrierFrame.ToWorldDirection(packet.RelativeVelocity), false);
        }

        // The server moved the body: it waits at the new place for the owner's packets from after the move.
        private void OnPlaced(byte placeCount) {
            if (_channel.isOwned)
                return;

            _placementFence.Begin(placeCount, Time.timeAsDouble);
            if (_isDriving == false)
                return;

            StopSimulating();
            _delayBuffer.Clear();
            _isDriving = false;
        }

        private void StartSimulating() {
            if (_isSimulating)
                return;

            _isSimulating = true;
            _groundTimer.Reset();
            _floatingController.SimulatesCopy = true;
            _movable.SetRemoteInput(_input);
            SyncRider();
        }

        private void StopSimulating() {
            if (_isSimulating == false)
                return;

            _isSimulating = false;
            _floatingController.SimulatesCopy = false;
            _movable.SetRemoteInput(null);
            SyncRider();
        }

        // The rider of the body being simulated rides as a copy; the body can change (the ragdoll wakes for the copy).
        private void SyncRider() {
            ICarrierRider wanted = _isSimulating ? _movable.DrivenBody.Rider : null;
            if (wanted == _copyRider)
                return;

            _copyRider?.RideAsCopy(false);
            _copyRider = wanted;
            _copyRider?.RideAsCopy(true);
        }

        private void Correct(IMovableBody body, double showTime) {
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
            Vector3 error = target - body.Position;
            _channel.ReceiveStats.RecordError(error.magnitude, Time.unscaledTimeAsDouble);
            PositionCorrectionStep step = PositionCorrection.Compute(error, ownerSteers, isStuck, settings);
            if (step.IsSnap) {
                Snap(body, target, ownerVelocity, true);
                return;
            }

            MoveBy(body, step.Offset);
            if (_hasReconciled && packet.Number == _reconciledSequence)
                return;

            body.SetVelocity(VelocityReconciliation.Reconcile(body.Velocity, ownerVelocity, ownerSteers));
            _reconciledSequence = packet.Number;
            _hasReconciled = true;
        }

        // A step of the pull long enough to see (a large error) is hidden like a snap.
        private void MoveBy(IMovableBody body, Vector3 offset) {
            body.MoveBy(offset);
            if (_visualLag != null)
                _visualLag.HideJump(offset);
        }

        private void Snap(IMovableBody body, Vector3 position, Vector3 velocity, bool count) {
            Vector3 offset = position - body.Position;
            if (_visualLag != null)
                _visualLag.HideJump(offset);

            body.MoveBy(offset);
            body.SetVelocity(velocity);
            if (count)
                _channel.ReceiveStats.RecordSnap(Time.unscaledTimeAsDouble);
        }
    }
}
