using System.Collections.Generic;
using Features.NetworkModelModule.Scripts;

namespace Features.ShipModule.Scripts {
    // Every player's rider on this peer by its stable key, riding or not: a seat's occupant may stand anywhere or have
    // just spawned. Each rider registers itself while it is spawned on this peer.
    public sealed class ShipRiderKeyModel {
        private readonly Dictionary<string, ShipRider> _riders = new();

        public bool TryGet(PlayerKey key, out ShipRider rider) {
            rider = null;
            return key.IsEmpty == false && _riders.TryGetValue(key.Id, out rider) && rider != null;
        }

        internal void Register(ShipRider rider) {
            PlayerKey key = rider.Key;
            if (key.IsEmpty == false)
                _riders[key.Id] = rider;
        }

        internal void Unregister(ShipRider rider) {
            PlayerKey key = rider.Key;
            if (key.IsEmpty == false && _riders.TryGetValue(key.Id, out ShipRider current) && current == rider)
                _riders.Remove(key.Id);
        }
    }
}
