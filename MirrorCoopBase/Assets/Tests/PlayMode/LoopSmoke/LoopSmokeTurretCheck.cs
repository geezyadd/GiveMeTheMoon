#if UNITY_INCLUDE_TESTS
using System.Collections;
using Features.CharacterMovableModule.Scripts;
using Features.GrabModule.Scripts;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Debug;
using Features.ShipModule.Scripts.Turret;
using Mirror;
using NUnit.Framework;
using Tests.Shared;
using UnityEngine;

namespace Tests.PlayMode.LoopSmoke {
    // The minigun turret: installed at a station, then in cruise the host sits down through the seat's use command,
    // aims the turret at the debug dummy (the station's test hook stands in for the mouse) and holds the trigger
    // until the server has accepted hits on the dummy, then stands up again.
    public sealed class LoopSmokeTurretCheck {
        private const string MINIGUN_MODULE_ID = "minigun";
        private const string TURRET_SOCKET_ID = "turret-front";
        private const int MIN_ACCEPTED_HITS = 5;
        private const float SEAT_TIMEOUT_SECONDS = 3f;
        private const float FIRE_TIMEOUT_SECONDS = 6f;
        private const float BOARD_SETTLE_SECONDS = 0.6f;
        private const float DROP_HEIGHT = 0.5f;
        // Behind the turret socket on the deck, within reach of its seat.
        private static readonly Vector3 _gunnerSpot = new Vector3(-2.6f, 0.3f, 1.3f);

        private readonly LoopSmokeSession _session;

        private ShipSocket Socket => FindSocket(_session.Ship);
        private static NetworkIdentity Player => SessionQueries.LocalPlayer;

        public LoopSmokeTurretCheck(LoopSmokeSession session) =>
            _session = session;

        // The minigun goes in at the station and the host walks over to it before the launch.
        public IEnumerator InstallMinigunAndStandByItCoroutine() {
            Assert.IsTrue(Socket.ServerTryInstall(MINIGUN_MODULE_ID), "Turret: the minigun was not installed at the station.");
            Assert.IsNotNull(Socket.GetComponentInChildren<ShipTurretStation>(), "Turret: the installed minigun shows no turret station.");
            ShipDeckBoarding.PutAbove(Player.GetComponent<CharacterMovableBase>(), _session.Ship, _gunnerSpot, DROP_HEIGHT);

            yield return new WaitForSeconds(BOARD_SETTLE_SECONDS);
            Assert.IsTrue(SessionQueries.IsOnDeck(_session.Ship, Player.transform.position), "Turret: the host did not stay on the deck by the minigun.");
        }

        public IEnumerator FireAtDebugTargetInCruiseCoroutine() {
            IWeaponDebugTargets targets = SessionQueries.Resolve<IWeaponDebugTargets>();
            WeaponDebugTarget target = targets.DebugSpawnTarget();
            Assert.IsNotNull(target, "Turret: the debug target did not spawn in cruise.");
            ShipTurretStation station = Socket.GetComponentInChildren<ShipTurretStation>();
            ShipSitInteractable seat = Socket.GetComponentInChildren<ShipSitInteractable>();
            UseController use = Player.GetComponent<UseController>();

            use.RequestUse(seat);
            yield return _session.WaitForCoroutine(() => station.IsOccupiedByLocalPlayer, SEAT_TIMEOUT_SECONDS, "Turret: the host seated at the minigun");
            Assert.AreEqual(Player.netId, Socket.OccupantNetId, "Turret: the socket does not hold the host as its occupant.");

            station.DebugHoldTrigger(true);
            yield return _session.WaitForCoroutine(() => AimAndCountHits(station, target), FIRE_TIMEOUT_SECONDS, $"Turret: {MIN_ACCEPTED_HITS} hits the server accepted on the debug target");
            station.DebugHoldTrigger(false);

            use.RequestUse(seat);
            yield return _session.WaitForCoroutine(() => Socket.OccupantNetId == 0 && station.IsOccupiedByLocalPlayer == false, SEAT_TIMEOUT_SECONDS, "Turret: the host stood up from the minigun");
            Assert.IsTrue(Player.GetComponent<ShipRider>().IsRiding, "Turret: the host does not ride the ship after standing up in cruise.");
            targets.DebugDespawnTarget();
        }

        // The dummy sways from side to side: the turret follows it every frame, as a player would.
        private static bool AimAndCountHits(ShipTurretStation station, WeaponDebugTarget target) {
            station.DebugAimAt(target.transform.position);
            return target.Hits >= MIN_ACCEPTED_HITS;
        }

        private static ShipSocket FindSocket(ShipBase ship) {
            foreach (ShipSocket socket in ship.Sockets) {
                if (socket.SocketId == TURRET_SOCKET_ID)
                    return socket;
            }

            Assert.Fail($"Turret: the ship has no socket '{TURRET_SOCKET_ID}'.");
            return null;
        }
    }
}
#endif
