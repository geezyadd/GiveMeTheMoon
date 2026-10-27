#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using Features.ShipModule.Scripts;
using Features.TurretModule.Scripts;
using UnityEngine;

namespace Tests.Coop.Shared {
    // The ship's turret socket as both processes find it, and where a gunner stands behind the gun to sit down.
    public static class CoopTurret {
        public const string SOCKET_ID = "turret-front";
        public const string MINIGUN_MODULE_ID = "minigun";
        public const string LIGHT_GUN_MODULE_ID = "light-machine-gun";
        // Ship-local, behind the gun: the seat's volume is under the crosshair from there (LoopSmokeTurretCheck's spot).
        public static readonly Vector3 GunnerSpot = new Vector3(-2.6f, 0.3f, 1.3f);

        public static ShipSocket FindSocket(ShipBase ship) {
            foreach (ShipSocket socket in ship.Sockets) {
                if (socket.SocketId == SOCKET_ID)
                    return socket;
            }

            throw new InvalidOperationException($"{ship.name} has no socket '{SOCKET_ID}'.");
        }

        // False while no gun stands in the socket: the station lives on the installed gun's slot view.
        public static bool TryFindStation(ShipBase ship, out ShipTurretStation station) {
            station = FindSocket(ship).GetComponentInChildren<ShipTurretStation>();
            return station != null;
        }
    }
}
#endif
