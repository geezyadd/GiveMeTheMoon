#if UNITY_INCLUDE_TESTS
using System.Collections;
using Features.CharacterMovableModule.Scripts;
using Features.GrabModule.Scripts;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Debug;
using Features.TurretModule.Scripts;
using Mirror;
using NUnit.Framework;
using Tests.Shared;
using UnityEngine;

namespace Tests.PlayMode.LoopSmoke {
    // The minigun turret: installed at a station, then in cruise the host sits down through the seat's use command,
    // aims the turret at the debug dummy (the station's test hook stands in for the mouse) and holds the trigger
    // until the server has accepted hits on the dummy. The seated host then shoots a field rock
    // (LoopSmokeRockShootCheck) and a target balloon (LoopSmokeTargetCheck), and stands up again.
    public sealed class LoopSmokeTurretCheck {
        private const string MINIGUN_MODULE_ID = "minigun";
        private const string MINIGUN_SCREENSHOT = "turret-tier2-minigun.png";
        private const string TURRET_SOCKET_ID = "turret-front";
        private const int MIN_ACCEPTED_HITS = 5;
        private const float SEAT_TIMEOUT_SECONDS = 3f;
        private const float FIRE_TIMEOUT_SECONDS = 6f;
        private const float BOARD_SETTLE_SECONDS = 0.6f;
        private const float DROP_HEIGHT = 0.5f;
        private const float BELOW_DECK_TOLERANCE = 0.25f;
        // Behind the turret socket on the deck, within reach of its seat.
        public static readonly Vector3 GunnerSpot = new Vector3(-2.6f, 0.3f, 1.3f);

        private readonly LoopSmokeSession _session;

        private ShipSocket Socket => FindSocket(_session.Ship);
        private static NetworkIdentity Player => SessionQueries.LocalPlayer;

        public LoopSmokeTurretCheck(LoopSmokeContext context) =>
            _session = context.Session;

        // The turret station the host sits at, for the checks that shoot from it between sitting down and standing up.
        public static ShipTurretStation FindSeatedStation(ShipBase ship) {
            ShipTurretStation station = FindSocket(ship).GetComponentInChildren<ShipTurretStation>();
            Assert.IsNotNull(station, "Turret: the ship has no minigun station.");
            Assert.IsTrue(station.IsOccupiedByLocalPlayer, "Turret: the host does not sit at the minigun.");
            return station;
        }

        // The minigun goes in at the station and the host walks over to it before the launch.
        [LoopSmokeStep(LoopSmokePhase.BeforeTakeoff, 2, 2, 10)]
        public IEnumerator InstallMinigunAndStandByItCoroutine() {
            Assert.IsTrue(Socket.ServerTryInstall(MINIGUN_MODULE_ID), "Turret: the minigun was not installed at the station.");
            Assert.IsNotNull(Socket.GetComponentInChildren<ShipTurretStation>(), "Turret: the installed minigun shows no turret station.");
            ShipDeckBoarding.PutAbove(Player.GetComponent<CharacterMovableBase>(), _session.Ship, GunnerSpot, DROP_HEIGHT);

            yield return new WaitForSeconds(BOARD_SETTLE_SECONDS);
            AssertPlayerStandsOnDeck(_session.Ship);
        }

        [LoopSmokeStep(LoopSmokePhase.InCruise, 2, 2, 10)]
        public IEnumerator SitAndFireAtDebugTargetCoroutine() {
            IWeaponDebugTargets targets = SessionQueries.Resolve<IWeaponDebugTargets>();
            WeaponDebugTarget target = targets.DebugSpawnTarget();
            Assert.IsNotNull(target, "Turret: the debug target did not spawn in cruise.");
            ShipTurretStation station = Socket.GetComponentInChildren<ShipTurretStation>();

            Player.GetComponent<UseController>().RequestUse(Socket.GetComponentInChildren<ShipSitInteractable>());
            yield return _session.WaitForCoroutine(() => station.IsOccupiedByLocalPlayer, SEAT_TIMEOUT_SECONDS, "Turret: the host seated at the minigun");
            Assert.IsTrue(Socket.IsHeldBy(Player.GetComponent<ShipRider>().Key), "Turret: the socket does not hold the host's key as its occupant.");

            station.DebugHoldTrigger(true);
            yield return _session.WaitForCoroutine(() => AimAndCountHits(station, target), FIRE_TIMEOUT_SECONDS, $"Turret: {MIN_ACCEPTED_HITS} hits the server accepted on the debug target");
            yield return LoopSmokeReviveCheck.CaptureScreenshotCoroutine(MINIGUN_SCREENSHOT);
            station.DebugHoldTrigger(false);
            targets.DebugDespawnTarget();
        }

        // After the rock and the balloon shot.
        [LoopSmokeStep(LoopSmokePhase.InCruise, 2, 2, 40)]
        public IEnumerator StandUpCoroutine() {
            ShipTurretStation station = FindSeatedStation(_session.Ship);
            Player.GetComponent<UseController>().RequestUse(Socket.GetComponentInChildren<ShipSitInteractable>());
            yield return _session.WaitForCoroutine(() => Socket.HasOccupant == false && station.IsOccupiedByLocalPlayer == false, SEAT_TIMEOUT_SECONDS, "Turret: the host stood up from the minigun");
            Assert.IsTrue(Player.GetComponent<ShipRider>().IsRiding, "Turret: the host does not ride the ship after standing up in cruise.");
        }

        // The dummy sways from side to side: the turret follows it every frame, as a player would.
        private static bool AimAndCountHits(ShipTurretStation station, WeaponDebugTarget target) {
            station.DebugAimAt(target.transform.position);
            return target.Hits >= MIN_ACCEPTED_HITS;
        }

        // In ship space: the world box of the deck collider grows and shifts with the ship's heading at the station.
        private static void AssertPlayerStandsOnDeck(ShipBase ship) {
            Vector3 local = Quaternion.Inverse(ship.transform.rotation) * (Player.transform.position - ship.transform.position);
            Assert.IsTrue(ship.DeckGeometry.ContainsDeckWalk(local, 0f), $"Turret: the host at ship-local {local} did not stay on the deck by the minigun.");
            Assert.IsTrue(ship.DeckGeometry.TryGetDeckSurfaceY(local, out float surfaceY), $"Turret: no deck under the host at ship-local {local}.");
            Assert.Greater(local.y, surfaceY - BELOW_DECK_TOLERANCE, $"Turret: the host at ship-local {local} is below the deck ({surfaceY:0.00}).");
        }

        public static ShipSocket FindSocket(ShipBase ship) {
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
