#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using Features.ShipModule.Scripts;

namespace Tests.Coop.Shared {
    // The ship's helm as both processes find it: the socket whose installed slot view steers the ship.
    public static class CoopHelm {
        public static ShipSocket FindSocket(ShipBase ship) {
            foreach (ShipSocket socket in ship.Sockets) {
                if (socket != null && socket.Station is IShipSteeringStation)
                    return socket;
            }

            throw new InvalidOperationException($"{ship.name} has no socket with a steering station (the helm).");
        }

        public static IShipSteeringStation FindStation(ShipBase ship) =>
            (IShipSteeringStation)FindSocket(ship).Station;
    }
}
#endif
