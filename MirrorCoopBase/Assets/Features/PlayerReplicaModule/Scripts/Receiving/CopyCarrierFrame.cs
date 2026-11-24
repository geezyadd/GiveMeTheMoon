using Features.FloatingControllerModule;
using Mirror;
using UnityEngine;

namespace Features.PlayerReplicaModule.Receiving {
    // The axes a copy reads its owner's packet in: this peer's copy of the carrier the packet names, or the still world.
    // The carrier is found by its network id among the spawned objects, cached while the id stays the same. A carrier
    // this peer does not have (not spawned yet) is missing: the copy then leaves its body alone.
    public sealed class CopyCarrierFrame {
        private const uint WORLD_CARRIER = 0;

        private uint _carrierId;
        private ICarrier _carrier;

        public ICarrier Carrier => _carrier;
        public bool IsMissing => _carrierId != WORLD_CARRIER && _carrier == null;

        // The carrier's pose of the last physics step (where the bodies on it were carried to), or the world's.
        public Pose Frame => _carrier != null ? _carrier.PoseBefore : Pose.identity;
        public Quaternion Heading => _carrier != null ? CarrierMotion.HeadingOf(_carrier.PoseBefore) : Quaternion.identity;

        public void Resolve(uint carrierId) {
            if (carrierId == _carrierId && (_carrier != null || carrierId == WORLD_CARRIER))
                return;

            _carrierId = carrierId;
            _carrier = carrierId == WORLD_CARRIER ? null : Find(carrierId);
        }

        public Vector3 ToWorldPoint(Vector3 point) =>
            CarrierMotion.ToWorldPoint(Frame, point);

        public Vector3 ToWorldDirection(Vector3 direction) =>
            CarrierMotion.ToWorldDirection(Frame, direction);

        private static ICarrier Find(uint carrierId) {
            NetworkIdentity identity;
            bool found = NetworkServer.active
                ? NetworkServer.spawned.TryGetValue(carrierId, out identity)
                : NetworkClient.spawned.TryGetValue(carrierId, out identity);

            return found && identity != null ? identity.GetComponent<ICarrier>() : null;
        }
    }
}
