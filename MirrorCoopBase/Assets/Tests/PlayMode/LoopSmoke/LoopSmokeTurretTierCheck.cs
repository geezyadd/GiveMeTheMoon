#if UNITY_INCLUDE_TESTS
using System.Collections;
using Features.CameraModule.Scripts;
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
    // Turret tier 1 the way a crew upgrades: at the station after loop 2 of run 2 the host buys the light machine gun
    // at the kiosk through the real aim and the window, picks it up by hand, takes the minigun off the turret socket
    // and installs the light gun from the hand in its place. The socket is used through its use commands (what E
    // sends): from behind the gun the seat's volume covers the socket plate under the crosshair. In the cruise of
    // flight 5 the host sits at the light gun through the seat's use command and fires at the debug dummy (the
    // station's test hook stands in for the mouse) until the server has accepted hits, then stands up; after that
    // landing tier 3, the heavy gun, goes in and fires the same way in cruise 6.
    public sealed class LoopSmokeTurretTierCheck {
        private const string LIGHT_GUN_MODULE_ID = "light-machine-gun";
        private const string MINIGUN_MODULE_ID = "minigun";
        private const string HEAVY_GUN_MODULE_ID = "heavy-gun";
        private const string LIGHT_GUN_SCREENSHOT = "turret-tier1-light-machine-gun.png";
        private const string HEAVY_GUN_SCREENSHOT = "turret-tier3-heavy-gun.png";
        private const int MIN_ACCEPTED_HITS = 3;
        private const float DROP_HEIGHT = 0.5f;
        private const float STAND_TOLERANCE = 1.5f;
        private const float BOARD_SETTLE_SECONDS = 0.6f;
        private const float COMMAND_TIMEOUT_SECONDS = 3f;
        private const float FIRE_TIMEOUT_SECONDS = 6f;

        private readonly LoopSmokeSession _session;
        private readonly LoopSmokeFlight _flight;

        private static NetworkIdentity Player => NetworkClient.localPlayer;
        private static LocalPlayerInteraction Interaction => Player.GetComponent<LocalPlayerInteraction>();
        private static GrabController Hand => Player.GetComponent<GrabController>();

        public LoopSmokeTurretTierCheck(LoopSmokeContext context) {
            _session = context.Session;
            _flight = context.Flight;
        }

        // Before the deck item check of this station (order 10) fills the hand.
        [LoopSmokeStep(LoopSmokePhase.BeforeTakeoff, 2, 3, 5)]
        public IEnumerator BuyAndInstallLightGunCoroutine() {
            ShipSocket socket = LoopSmokeTurretCheck.FindSocket(_session.Ship);
            Assert.AreEqual(MINIGUN_MODULE_ID, socket.InstalledModuleId, "Turret tier: the minigun is not in the turret socket before the upgrade.");
            Assert.IsTrue(SessionQueries.Resolve<ModuleCatalog>().TryGet(LIGHT_GUN_MODULE_ID, out ModuleDefinition lightGun), "Turret tier: the module catalog has no light gun.");
            try {
                CameraLookDriver.DebugPitchOverride = true;
                ShipItem bought = null;
                yield return LoopSmokeKiosk.BuyCoroutine<ShipItem>(_flight, LoopSmokeKiosk.FindModuleEntry(LIGHT_GUN_MODULE_ID), lightGun.Price, IsLooseLightGun, item => bought = item);
                yield return TakeOffInstalledGunCoroutine(socket);
                yield return LoopSmokeKiosk.PickUpCoroutine(bought.GetComponent<Grabbable>());
                yield return InstallFromHandCoroutine(socket);
            } finally {
                CameraLookDriver.DebugPitchOverride = false;
                CameraLookDriver.DebugPitch = 0f;
            }

            Assert.IsNotNull(socket.GetComponentInChildren<ShipTurretStation>(), "Turret tier: the installed light gun shows no turret station.");
            yield return _flight.BoardPlayerCoroutine();
        }

        // The light gun fires in the cruise of flight 5, the first one the host starts by the turret (flight 3 carries
        // a deck item, flight 4 starts at the throttle handle). The pistol check (order 20) drops the pistol first:
        // the host sits only with an empty hand.
        [LoopSmokeStep(LoopSmokePhase.InCruise, 2, 5, 30)]
        public IEnumerator SitAndFireLightGunCoroutine() =>
            SitFireAndStandCoroutine(LIGHT_GUN_MODULE_ID, LIGHT_GUN_SCREENSHOT);

        // Tier 3 goes in at the next station, after the shop check (order 20) (test setup: it is not bought again,
        // the purchase path is the light gun's) and stays in the turret socket for the rest of the run.
        [LoopSmokeStep(LoopSmokePhase.AfterLanding, 2, 5, 30)]
        public IEnumerator InstallHeavyGunCoroutine() {
            ShipSocket socket = LoopSmokeTurretCheck.FindSocket(_session.Ship);
            yield return TakeOffInstalledGunCoroutine(socket);
            Assert.IsTrue(socket.ServerTryInstall(HEAVY_GUN_MODULE_ID), "Turret tier: the heavy gun was not installed at the station.");
            Assert.IsNotNull(socket.GetComponentInChildren<ShipTurretStation>(), "Turret tier: the installed heavy gun shows no turret station.");
            yield return _flight.BoardPlayerCoroutine();
        }

        [LoopSmokeStep(LoopSmokePhase.BeforeTakeoff, 2, 5, 10)]
        public IEnumerator StandByLightGunCoroutine() =>
            StandByTurretCoroutine();

        // The hull loss check stands the host by the turret before this flight and sits at it (order 10): the heavy
        // gun fires first.
        [LoopSmokeStep(LoopSmokePhase.InCruise, 2, 6, 5)]
        public IEnumerator SitAndFireHeavyGunCoroutine() =>
            SitFireAndStandCoroutine(HEAVY_GUN_MODULE_ID, HEAVY_GUN_SCREENSHOT);

        private IEnumerator SitFireAndStandCoroutine(string moduleId, string screenshot) {
            ShipSocket socket = LoopSmokeTurretCheck.FindSocket(_session.Ship);
            Assert.AreEqual(moduleId, socket.InstalledModuleId, $"Turret tier: {moduleId} is not in the turret socket in cruise.");
            ShipTurretStation station = socket.GetComponentInChildren<ShipTurretStation>();
            IWeaponDebugTargets targets = SessionQueries.Resolve<IWeaponDebugTargets>();
            WeaponDebugTarget target = targets.DebugSpawnTarget();
            Assert.IsNotNull(target, "Turret tier: the debug target did not spawn in cruise.");

            // In cruise the host already stands there (a move in flight leaves the player behind the flying ship).
            if (_session.Ship.IsFlying)
                Assert.IsTrue(IsByTurret(Player.GetComponent<ShipRider>()), $"Turret tier: the host does not ride by the turret in cruise, ship-local {ShipLocal(Player.transform.position)}.");
            else
                yield return StandByTurretCoroutine();

            ShipSitInteractable seat = socket.GetComponentInChildren<ShipSitInteractable>();
            UseController use = Player.GetComponent<UseController>();
            Assert.IsTrue(use.CanUse(seat), $"Turret tier: the seat is not usable (sittable {socket.IsSittable}, occupant {socket.HasOccupant}, hand {Hand.IsHolding}).");
            use.RequestUse(seat);
            ShipRider rider = Player.GetComponent<ShipRider>();
            float deadline = Time.realtimeSinceStartup + COMMAND_TIMEOUT_SECONDS;
            while (station.IsOccupiedByLocalPlayer == false) {
                if (Time.realtimeSinceStartup > deadline)
                    Assert.Fail($"Turret tier: the host is not seated at {moduleId} (socket occupant {socket.HasOccupant}, held by the host {socket.IsHeldBy(rider.Key)}, riding {rider.IsRiding}, seated {rider.IsSeated}, at {Player.transform.position} ship {_session.Ship.transform.position}).");

                yield return null;
            }

            station.DebugHoldTrigger(true);
            yield return _session.WaitForCoroutine(() => AimAndCountHits(station, target), FIRE_TIMEOUT_SECONDS, $"Turret tier: {MIN_ACCEPTED_HITS} hits of {moduleId} the server accepted on the debug target");
            yield return LoopSmokeReviveCheck.CaptureScreenshotCoroutine(screenshot);
            station.DebugHoldTrigger(false);
            targets.DebugDespawnTarget();

            use.RequestUse(seat);
            yield return _session.WaitForCoroutine(() => station.IsOccupiedByLocalPlayer == false, COMMAND_TIMEOUT_SECONDS, $"Turret tier: the host stood up from {moduleId}");
            Assert.AreEqual(moduleId, socket.InstalledModuleId, $"Turret tier: {moduleId} left the socket in cruise.");
        }

        private IEnumerator TakeOffInstalledGunCoroutine(ShipSocket socket) {
            yield return StandByTurretCoroutine();
            Player.GetComponent<UseController>().RequestUse(socket.GetComponent<ShipUninstallInteractable>());
            yield return _session.WaitForCoroutine(() => Hand.Held != null && socket.IsOccupied == false, COMMAND_TIMEOUT_SECONDS, "Turret tier: the installed gun taken off into the hand");
            // Dropped from the hand, then removed (test cleanup): a loose crate may lie in a later check's crosshair.
            Grabbable taken = Hand.Held;
            yield return LoopSmokeKiosk.StandOnPadCoroutine(_flight);
            Interaction.ReleaseHeld();
            yield return _session.WaitForCoroutine(() => Hand.IsHolding == false, COMMAND_TIMEOUT_SECONDS, "Turret tier: the gun taken off dropped from the hand");
            NetworkServer.Destroy(taken.gameObject);
        }

        private IEnumerator InstallFromHandCoroutine(ShipSocket socket) {
            yield return StandByTurretCoroutine();
            Player.GetComponent<UseController>().RequestUse(socket.GetComponent<ShipInstallInteractable>());
            yield return _session.WaitForCoroutine(() => socket.InstalledModuleId == LIGHT_GUN_MODULE_ID && Hand.IsHolding == false, COMMAND_TIMEOUT_SECONDS, "Turret tier: the light gun installed from the hand");
        }

        private IEnumerator StandByTurretCoroutine() {
            ShipDeckBoarding.PutAbove(Player.GetComponent<CharacterMovableBase>(), _session.Ship, LoopSmokeTurretCheck.GunnerSpot, DROP_HEIGHT);
            yield return new WaitForSeconds(BOARD_SETTLE_SECONDS);
            Assert.IsTrue(IsByTurret(Player.GetComponent<ShipRider>()), $"Turret tier: the host does not stand by the turret, ship-local {ShipLocal(Player.transform.position)}.");
        }

        private bool IsByTurret(ShipRider rider) {
            Vector3 offset = ShipLocal(Player.transform.position) - LoopSmokeTurretCheck.GunnerSpot;
            offset.y = 0f;
            bool isOnDeck = SessionQueries.IsOnDeck(_session.Ship, Player.transform.position);
            bool isRidingInFlight = _session.Ship.IsFlying == false || rider.IsRiding;
            return isOnDeck && isRidingInFlight && offset.magnitude < STAND_TOLERANCE;
        }

        private Vector3 ShipLocal(Vector3 world) =>
            Quaternion.Inverse(_session.Ship.transform.rotation) * (world - _session.Ship.transform.position);

        private static bool IsLooseLightGun(ShipItem item) =>
            item.Module.Id == LIGHT_GUN_MODULE_ID && item.GetComponent<Grabbable>().CanBeGrabbed;

        private static bool AimAndCountHits(ShipTurretStation station, WeaponDebugTarget target) {
            station.DebugAimAt(target.transform.position);
            return target.Hits >= MIN_ACCEPTED_HITS;
        }
    }
}
#endif
