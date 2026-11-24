using System;
using Features.PlayerReplicaModule.Configuration;
using Features.PlayerReplicaModule.Packets;
using Features.PlayerReplicaModule.Receiving;
using Features.PlayerReplicaModule.Sending;
using Features.CharacterMovableModule.Scripts;
using Features.FloatingControllerModule;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.PlayerReplicaModule {
    // Carries one player's state packets. The owner sends them unreliably to the server; the server stamps when the
    // owner took each one (its time minus half the owner's round trip) and forwards it unreliably to everyone else.
    // Every peer that does not own the body keeps the newest packet it got. Packets are always whole, so a lost one
    // only means the next one arrives a little later.
    public sealed class BodyStateChannel : NetworkBehaviour {
        [SerializeField] private CharacterMovableBase _movable;
        [SerializeField] private FloatingController _floatingController;

        private readonly BodySendSchedule _schedule = new();
        private readonly PacketSequenceFilter _sequenceFilter = new();
        private readonly BodyStateReceiveStats _receiveStats = new();

        private ReplicaConfiguration _configuration;
        private BodyStateChannelRegistry _registry;
        private BodyStateSampler _sampler;
        private ushort _nextSequence;

        public bool HasLatest { get; private set; }
        public BodyStatePacket Latest { get; private set; }
        public BodyStateReceiveStats ReceiveStats => _receiveStats;

        // On a peer that does not own the body, for every packet newer than the last one.
        public event Action<BodyStatePacket> OnPacketAccepted;

        [Inject]
        private void Construct(ReplicaConfiguration configuration, BodyStateChannelRegistry registry) {
            _configuration = configuration;
            _registry = registry;
        }

        private void Awake() =>
            _sampler = new BodyStateSampler(_movable, _floatingController, GetComponent<ICarrierRider>());

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

        // The server's time as this peer sees it now, on the clock the packets' ServerTime is in.
        public static double ReadServerNow() =>
            NetworkServer.active ? NetworkTime.localTime : NetworkTime.predictedTime;
    }
}
