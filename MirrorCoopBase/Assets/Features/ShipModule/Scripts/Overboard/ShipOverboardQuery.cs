using System.Collections.Generic;
using Features.ShipModule.Scripts.Contracts;
using Mirror;

namespace Features.ShipModule.Scripts.Overboard {
    public sealed class ShipOverboardQuery : IShipOverboardQuery {
        private readonly IReadOnlyComponentRegistry<ShipOverboardRider> _overboardRiders;

        public ShipOverboardQuery(IReadOnlyComponentRegistry<ShipOverboardRider> overboardRiders) =>
            _overboardRiders = overboardRiders;

        public bool IsOverboard(NetworkIdentity player) {
            IReadOnlyList<ShipOverboardRider> riders = _overboardRiders.Items;
            for (int i = 0; i < riders.Count; i++) {
                if (riders[i] != null && riders[i].netIdentity == player)
                    return riders[i].IsOverboard;
            }

            return false;
        }
    }
}
