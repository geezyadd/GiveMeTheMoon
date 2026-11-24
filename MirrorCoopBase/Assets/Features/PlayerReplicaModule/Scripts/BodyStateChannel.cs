using System;
using Features.PlayerReplicaModule.Configuration;
using Features.PlayerReplicaModule.Packets;
using Features.PlayerReplicaModule.Receiving;
using Features.PlayerReplicaModule.Sending;
using Features.CharacterMovableModule.Scripts;
using Features.FloatingControllerModule;
using Features.GameCoreModule.Contracts;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.PlayerReplicaModule {
    // Carries one player's state packets. The owner sends them unreliably to the server; the server stamps when the
    // owner took each one (its time minus half the owner's round trip) and forwards it unreliably to everyone else.
    // Every peer that does not own the body keeps the newest packet it got. Packets are always whole, so a lost one
    // only means the next one arrives a little later.
    // The server also moves the body here on every peer (IPlayerBodyPlacement): each move has a number the owner puts
    // into its packets from then on, so a copy waits for the first packet sent after the move.
    public sealed class BodyStateChannel : NetworkBehaviour, IPlayerBodyPlacement {
        [SerializeField] private CharacterMovableBase _movable;

        private readonly BodySendSchedule _schedule = new();
        private readonly PacketSequenceFilter _sequenceFilter = new();
        private readonly BodyStateReceiveStats _receiveStats = new();

        private ReplicaConfiguration _configuration;
        private BodyStateChannelRegistry _registry;
        private BodyStateSampler _sampler;
        private ICarrierRider _carrierRider;
        private ushort _nextSequence;
        private byte _placeCount;

        public bool HasLatest { get; private set; }
        public BodyStatePacket Latest { get; private set; }
        public BodyStateReceiveStats ReceiveStats => _receiveStats;

        // On a peer that does not own the body, for every packet newer than the last one.
        public event Action<BodyStatePacket> OnPacketAccepted;

        // On every peer, when the server moved the body: the number of that move.
        public event Action<byte> OnPlaced;

        [Inject]
        private void Construct(ReplicaConfiguration configuration, BodyStateChannelRegistry registry) {
            _configuration = configuration;
            _registry = registry;
        }

        private void Awake() {
            _carrierRider = GetComponent<ICarrierRider>();
            _sampler = new BodyStateSampler(_movable);
        }

        public override void OnStartClient() =>
            _registry?.Add(this);

        public override void OnStartServer() =>
            _registry?.Add(this);

        private void OnDestroy() =>
            _registry?.Remove(this);

        private void FixedUpdate() {
            if (isOwned == false || _configuration == null || _movable.Body == null)
                return;

            double now = Time.fixedTimeAsDouble;
            BodyStatePacket packet = _sampler.Sample(_nextSequence);
            packet.PlaceCount = _placeCount;
            if (_schedule.IsDue(packet, now, _configuration.PacketInterval, _configuration.RestingPacketInterval, _configuration.RestingSpeed) == false)
                return;

            _schedule.MarkSent(packet, now);
            _nextSequence = unchecked((ushort)(_nextSequence + 1));
            if (isServer)
                ServerForward(packet, NetworkTime.localTime);
            else
                CmdSend(packet);
        }

        [Command(channel = Channels.Unreliable)]
        private void CmdSend(BodyStatePacket packet) {
            double halfRoundTrip = connectionToClient != null ? connectionToClient.rtt * 0.5d : 0d;
            ServerForward(packet, NetworkTime.localTime - halfRoundTrip);
        }

        [Server]
        private void ServerForward(BodyStatePacket packet, double takenAt) {
            packet.ServerTime = takenAt;
            if (isOwned == false)
                Receive(packet, NetworkTime.localTime);

            RpcReceive(packet);
        }

        [ClientRpc(channel = Channels.Unreliable, includeOwner = false)]
        private void RpcReceive(BodyStatePacket packet) {
            // The host's client half shares the server's copy, which got the packet in ServerForward already.
            if (isServer)
                return;

            Receive(packet, NetworkTime.predictedTime);
        }

        private void Receive(BodyStatePacket packet, double serverNow) {
            double now = Time.unscaledTimeAsDouble;
            if (_sequenceFilter.TryAccept(packet.Number) == false) {
                _receiveStats.RecordDropped(now);
                return;
            }

            Latest = packet;
            HasLatest = true;
            _receiveStats.RecordAccepted(serverNow - packet.ServerTime, now);
            OnPacketAccepted?.Invoke(packet);
        }

        [Server]
        public void ServerPlace(Pose pose) {
            _placeCount = unchecked((byte)(_placeCount + 1));
            ApplyPlace(pose.position, pose.rotation, _placeCount);
            RpcPlace(pose.position, pose.rotation, _placeCount);
        }

        [Server]
        public void ServerShift(Vector3 delta) {
            _placeCount = unchecked((byte)(_placeCount + 1));
            ApplyShift(delta, _placeCount);
            RpcShift(delta, _placeCount);
        }

        // Reliable and to the owner too: the owner moves the body it simulates.
        [ClientRpc]
        private void RpcPlace(Vector3 position, Quaternion rotation, byte placeCount) {
            // The host's client half shares the server's body, which ServerPlace moved already.
            if (isServer)
                return;

            ApplyPlace(position, rotation, placeCount);
        }

        [ClientRpc]
        private void RpcShift(Vector3 delta, byte placeCount) {
            if (isServer)
                return;

            ApplyShift(delta, placeCount);
        }

        private void ApplyPlace(Vector3 position, Quaternion rotation, byte placeCount) {
            Rigidbody body = _movable.Body;
            MoveBody(body, position, rotation);
            body.StopIfDynamic();
            _carrierRider?.DropCarrier();
            // A ragdoll body rides on its own rider; it forgets its carrier too.
            ICarrierRider drivenRider = _movable.DrivenBody.Rider;
            if (drivenRider != _carrierRider)
                drivenRider?.DropCarrier();
            FinishPlace(placeCount);
        }

        private void ApplyShift(Vector3 delta, byte placeCount) {
            Rigidbody body = _movable.Body;
            MoveBody(body, body.position + delta, body.rotation);
            FinishPlace(placeCount);
        }

        // An interpolated body would blend in from where it was until the next physics step, even with the transform
        // set: switching the interpolation off and back drops that old pose.
        private static void MoveBody(Rigidbody body, Vector3 position, Quaternion rotation) {
            RigidbodyInterpolation interpolation = body.interpolation;
            body.interpolation = RigidbodyInterpolation.None;
            body.position = position;
            body.rotation = rotation;
            body.transform.SetPositionAndRotation(position, rotation);
            body.interpolation = interpolation;
        }

        // The owner's packets carry the number from now on; a copy waits for them.
        private void FinishPlace(byte placeCount) {
            _placeCount = placeCount;
            OnPlaced?.Invoke(placeCount);
        }

        // The server's time as this peer sees it now, on the clock the packets' ServerTime is in.
        public static double ReadServerNow() =>
            NetworkServer.active ? NetworkTime.localTime : NetworkTime.predictedTime;
    }
}
