using System.Collections.Generic;
using Mirror;

namespace Features.ShopModule.Scripts.Data {
    // Server side: the kiosk each player last opened the shop at, so a purchase is checked against and delivered to
    // that kiosk. Session-only: a rejoining player opens the shop again.
    public sealed class ShopVisitModel {
        private readonly Dictionary<NetworkIdentity, ShopKioskVisit> _visits = new();

        public void SetVisit(NetworkIdentity player, ShopKioskVisit visit) =>
            _visits[player] = visit;

        public void RemoveVisit(NetworkIdentity player) =>
            _visits.Remove(player);

        public bool TryGetVisit(NetworkIdentity player, out ShopKioskVisit visit) =>
            _visits.TryGetValue(player, out visit);
    }
}
