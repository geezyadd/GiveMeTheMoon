using Features.BodySyncModule.Configuration;
using Features.BodySyncModule.Correction;
using Features.BodySyncModule.Models;
using Features.BodySyncModule.Packets;
using Features.BodySyncModule.Receiving;
using Features.CharacterMovableModule.Scripts;
using Features.FloatingControllerModule;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.BodySyncModule {
    // On a peer that does not own this player, while the sync mode says so: turns the player into a simulated copy.
    // The old network body steps aside, the body becomes dynamic, and the float spring and the movement run on it from
    // the owner's controls. The copy lives a little in the past (the copy delay): each packet becomes current when the
    // show time reaches its stamp, so the controls and the position the copy is pulled to belong to the same moment
    // and a copy stops where its owner stopped. After each physics step it is pulled toward the current packet: a share
    // of the position error (gentle, with a dead zone, while the owner presses no walk key), the velocity reconciled
    // once per packet, a snap past the snap distance. The target is moved ahead along the packet's velocity only while
    // the owner walks. While a ride or a seat holds the body (kinematic), the copy leaves it alone.
    // Runs after the float probe and before the movement (execution order): a packet's controls and the reconciled
    // velocity are in place before the movement computes this step's drive, so a stop reaches the copy on the very
    // step it is due. All of it happens before the physics step moves the body.
    [DefaultExecutionOrder(-50)]
    public sealed class BodyCopyDriver : MonoBehaviour {
        [SerializeField] private BodyStateChannel _channel;
        [SerializeField] private CharacterMovableBase _movable;
        [SerializeField] private FloatingController _floatingController;
        [SerializeField] private NetworkRigidbodyUnreliable _networkBody;

        private readonly PacketMoveInputSource _input = new();
        private readonly PacketDelayBuffer _delayBuffer = new();
        private readonly GroundMismatchTimer _groundTimer = new();

        private BodySyncConfiguration _configuration;
        private BodySyncModeModel _modeModel;
        private bool _isDriving;
        private bool _isSimulating;
        private bool _hasReconciled;
        private ushort _reconciledSequence;

        [Inject]
        private void Construct(BodySyncConfiguration configuration, BodySyncModeModel modeModel) {
            _configuration = configuration;
            _modeModel = modeModel;
        }

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
                _input.Apply(_delayBuffer.Current);

            if (body.isKinematic) {
                StopSimulating();
                return;
            }

            StartSimulating();
            Correct(body, showTime);
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
            _input.Apply(packet);
            _hasReconciled = false;
            _isDriving = true;
            Snap(body, packet.LocalPosition, packet.LocalVelocity, false);
            return true;
        }

        private void Release() {
            StopSimulating();
            if (_networkBody != null)
                _networkBody.enabled = true;

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
        }

        private void StopSimulating() {
            if (_isSimulating == false)
                return;

            _isSimulating = false;
            _floatingController.SimulatesCopy = false;
            _movable.SetRemoteInput(null);
        }

        private void Correct(Rigidbody body, double showTime) {
            BodyStatePacket packet = _delayBuffer.Current;
            BodyCorrectionSettings settings = _configuration.CorrectionSettings;
            // Ahead along the velocity only while the owner walks: a braking owner's velocity would carry the target
            // past the place they stop at, and the copy would be pulled back.
            bool ownerSteers = packet.Wish != Vector2.zero;
            double age = ownerSteers ? showTime - packet.ServerTime : 0d;
            Vector3 target = CorrectionTarget.Predict(packet.LocalPosition, packet.LocalVelocity, age, settings.MaxExtrapolationSeconds);

            _groundTimer.Tick(packet.Has(BodyStateFlags.Grounded), _movable.IsSupported, Time.fixedDeltaTime);
            bool isStuck = _groundTimer.IsStuck(settings.GroundMismatchSeconds);
            PositionCorrectionStep step = PositionCorrection.Compute(target - body.position, ownerSteers, isStuck, settings);
            if (step.IsSnap) {
                Snap(body, target, packet.LocalVelocity, true);
                return;
            }

            body.position += step.Offset;
            if (_hasReconciled && packet.Sequence == _reconciledSequence)
                return;

            body.linearVelocity = VelocityReconciliation.Reconcile(body.linearVelocity, packet.LocalVelocity, ownerSteers);
            _reconciledSequence = packet.Sequence;
            _hasReconciled = true;
        }

        private void Snap(Rigidbody body, Vector3 position, Vector3 velocity, bool count) {
            body.position = position;
            body.linearVelocity = velocity;
            if (count)
                _channel.ReceiveStats.RecordSnap(Time.unscaledTimeAsDouble);
        }
    }
}
