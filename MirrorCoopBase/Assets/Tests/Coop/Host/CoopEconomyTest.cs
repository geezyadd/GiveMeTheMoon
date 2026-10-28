#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using Features.GrabModule.Scripts;
using Features.HandWeaponModule.Scripts;
using Features.ShipDamageModule.Scripts.Debug;
using Features.ShipDamageModule.Scripts.Repair;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Debug;
using Features.ShipModule.Scripts.Generated;
using Features.ShopModule.Scripts.Configurations;
using Features.ShopModule.Scripts.Generated;
using Features.TurretModule.Scripts;
using NUnit.Framework;
using Tests.Coop.Shared;
using Tests.Shared;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using Steps = Tests.Coop.Shared.CoopWeaponsEconomySteps;

namespace Tests.Coop.Host {
    // The crew economy with a real second client process doing every action (CoopWeaponsEconomyClientScenarios) and the
    // host checking it: a normal run starts with 300 and no later station drops a free module; at the kiosk the client cannot
    // buy an engine it cannot afford (the button is off, a purchase sent anyway is refused), buys a propeller, tops the
    // wallet up from its debug window, wheels the list down and buys an engine, a turret gun, the wrench and the pistol,
    // and cannot buy the heavy gun; it installs the engine, the propeller and the gun from its hand (the flight speed
    // changes on both peers), repairs a module the host broke with its bought wrench and patches a breach with the
    // rack's hammer by holding interact (the host sees the hold running and the repairs), and both ride a loop to a
    // station that drops nothing. The wallet, the ship and its condition are compared on both peers at every step.
    [PrebuildSetup("Tests.Coop.Editor.CoopClientBuild")]
    public sealed class CoopEconomyTest {
        private const int TEST_TIMEOUT_MS = 300000;
        private const long NORMAL_RUN_START_BALANCE = 300;
        private const float HULL_HIT = 5f;
        private const float TOLERANCE = 0.01f;

        private CoopEconomyHostHarness _host;
        private int _stationModuleItems;

        [SetUp]
        public void SetUp() =>
            _host = new CoopEconomyHostHarness();

        [TearDown]
        public void TearDown() =>
            _host.Dispose();

        [UnityTest, Timeout(TEST_TIMEOUT_MS)]
        public IEnumerator WhenClientBuysAtKiosk_AndInstallsAndRepairs_ThenHostSeesTheSameWalletShipAndRepairs() {
            // LoopSmokeErrorLog fails the test with the full list instead.
            LogAssert.ignoreFailingMessages = true;
            yield return _host.HostAndStartWithClientCoroutine(Steps.SCENARIO_ECONOMY);
            AssertNormalRunStart();
            yield return WatchClientShopCoroutine();
            yield return WatchClientInstallCoroutine();
            yield return WatchClientRepairCoroutine();
            yield return RideLoopTogetherCoroutine();
            yield return _host.FinishCoroutine();
        }

        private void AssertNormalRunStart() {
            _host.BeginStep("a normal run starts");
            Assert.AreEqual(NORMAL_RUN_START_BALANCE, SessionQueries.Resolve<WalletConfiguration>().StartingBalance, "A normal run does not start with 300.");
            Assert.AreEqual(NORMAL_RUN_START_BALANCE, Wallet.Balance, "The crew wallet does not start at 300 on the host.");
            Assert.AreEqual(NORMAL_RUN_START_BALANCE, _host.LastReport.Balance, "The crew wallet does not start at 300 on the client.");
            // GameScene places loose modules at the start station (not a station drop): the same on both peers, and no
            // later station adds any.
            _stationModuleItems = _host.LastReport.LooseModuleItems;
            Debug.Log($"[CoopTest] the start station of a normal run has {_stationModuleItems} loose modules placed in GameScene");
            _host.AssertSameView(false, true);
        }

        private IEnumerator WatchClientShopCoroutine() {
            _host.BeginStep("client is refused an engine it cannot afford");
            yield return WatchRefusedCoroutine(Steps.ROCKET_ENGINE_ID);
            _host.BeginStep("client buys a propeller");
            yield return WatchPurchaseCoroutine(Steps.PROPELLER_ID);
            yield return _host.CommandCoroutine(Steps.SHOP_CLOSED, Steps.COMMAND_LEAVE_SHOP);

            _host.BeginStep("client tops the wallet up from its debug window");
            long before = Wallet.Balance;
            yield return _host.CommandCoroutine(Steps.MONEY_GRANTED, Steps.COMMAND_GRANT_MONEY);
            Assert.AreEqual(before + SessionQueries.Resolve<WalletConfiguration>().DebugGrantAmount, Wallet.Balance, "The client's \"+1000\" did not add the debug amount once on the host.");
            _host.AssertSameView(false, false);

            _host.BeginStep("client buys an engine, a turret gun, the wrench and the pistol");
            yield return WatchPurchaseCoroutine(Steps.ROCKET_ENGINE_ID);
            yield return WatchPurchaseCoroutine(Steps.LIGHT_GUN_ID);
            yield return WatchPurchaseCoroutine(Steps.WRENCH_ID);
            yield return WatchPurchaseCoroutine(Steps.PISTOL_ID);
            _host.BeginStep("client is refused the heavy gun");
            yield return WatchRefusedCoroutine(Steps.HEAVY_GUN_ID);
            yield return _host.CommandCoroutine(Steps.SHOP_CLOSED, Steps.COMMAND_LEAVE_SHOP);
        }

        private IEnumerator WatchRefusedCoroutine(string entryId) {
            long before = Wallet.Balance;
            int itemsBefore = CountItems(entryId);
            yield return _host.CommandCoroutine(Steps.PURCHASE_REFUSED, Steps.COMMAND_TRY_UNAFFORDABLE, entryId);
            Assert.AreEqual(before, Wallet.Balance, $"The server took money for {entryId} the crew cannot afford.");
            Assert.AreEqual(itemsBefore, CountItems(entryId), $"The server delivered {entryId} the crew cannot afford.");
            _host.AssertSameView(false, false);
        }

        private IEnumerator WatchPurchaseCoroutine(string entryId) {
            long before = Wallet.Balance;
            int itemsBefore = CountItems(entryId);
            yield return _host.CommandCoroutine(Steps.BOUGHT, Steps.COMMAND_BUY, entryId);
            Assert.AreEqual(before - FindPrice(entryId), Wallet.Balance, $"The client's purchase of {entryId} did not take its price once on the host.");
            Assert.AreEqual(itemsBefore + 1, CountItems(entryId), $"The client's purchase of {entryId} did not deliver one item on the host.");
            _host.AssertSameView(false, false);
        }

        // The gun first: the bought items lie in a pile at the kiosk, the last bought on top.
        private IEnumerator WatchClientInstallCoroutine() {
            _host.BeginStep("client installs the turret gun");
            yield return WatchInstallCoroutine(Steps.LIGHT_GUN_ID, Steps.TURRET_SOCKET);
            Assert.IsNotNull(FindSocket(Steps.TURRET_SOCKET).GetComponentInChildren<ShipTurretStation>(), "The client's light gun shows no turret station on the host.");

            IReadOnlyShipStatsModel stats = SessionQueries.Resolve<IReadOnlyShipStatsModel>();
            _host.BeginStep("client installs the engine");
            float speed = stats.FlightSpeed;
            yield return WatchInstallCoroutine(Steps.ROCKET_ENGINE_ID, Steps.ENGINE_LEFT_SOCKET);
            Assert.Greater(stats.FlightSpeed, speed + TOLERANCE, "The client's engine did not raise the flight speed on the host.");

            _host.BeginStep("client installs the propeller");
            speed = stats.FlightSpeed;
            yield return WatchInstallCoroutine(Steps.PROPELLER_ID, Steps.ENGINE_RIGHT_SOCKET);
            Assert.AreNotEqual(speed, stats.FlightSpeed, "The client's propeller did not change the flight speed on the host.");
            Assert.IsTrue(_host.Ship.CanLaunch, "The ship cannot launch with the client's engine and propeller.");
            Assert.AreEqual(_stationModuleItems, CoopWeaponsEconomyObserver.Capture(Steps.INSTALLED).LooseModuleItems, "A bought module still lies loose after the installs.");
        }

        private IEnumerator WatchInstallCoroutine(string moduleId, string socketId) {
            yield return _host.CommandCoroutine(Steps.INSTALLED, Steps.COMMAND_INSTALL, moduleId, socketId);
            Assert.AreEqual(moduleId, FindSocket(socketId).InstalledModuleId, $"The host does not see the client's {moduleId} in {socketId}.");
            _host.AssertSameView(false, true);
        }

        // The host breaks the client's engine and opens a breach (the debug window's entries); the client repairs both.
        private IEnumerator WatchClientRepairCoroutine() {
            _host.BeginStep("host breaks the engine and opens a breach");
            IReadOnlyShipStatsModel stats = SessionQueries.Resolve<IReadOnlyShipStatsModel>();
            float workingSpeed = stats.FlightSpeed;
            IShipDamageDebug damage = SessionQueries.Resolve<IShipDamageDebug>();
            Assert.IsTrue(damage.DebugBreakModule(Steps.ENGINE_LEFT_SOCKET), "The debug break did not break the client's engine.");
            SessionQueries.Resolve<IShipHullDebug>().DebugDamage(HULL_HIT);
            Assert.IsTrue(damage.DebugOpenBreach(Steps.REPAIRED_BREACH), "The debug breach did not open.");
            yield return _host.CommandCoroutine(Steps.DAMAGE_SEEN, Steps.COMMAND_SEE_DAMAGE, Steps.ENGINE_LEFT_SOCKET, Steps.REPAIRED_BREACH);
            Assert.Less(stats.FlightSpeed, workingSpeed - TOLERANCE, "The broken engine did not lower the flight speed.");
            _host.AssertSameView(false, true);

            _host.BeginStep("client repairs the engine with its bought wrench");
            yield return WatchHoldCoroutine(Steps.COMMAND_REPAIR_MODULE, Steps.ENGINE_LEFT_SOCKET);
            Assert.AreEqual(workingSpeed, stats.FlightSpeed, TOLERANCE, "The client's repair did not bring the flight speed back on the host.");

            _host.BeginStep("client patches the breach with the rack's hammer");
            yield return WatchHoldCoroutine(Steps.COMMAND_PATCH_BREACH, Steps.REPAIRED_BREACH);
            Assert.AreEqual(stats.HullMax, stats.Hull, TOLERANCE, "The client's patch did not give the small hit back on the host.");
        }

        // The hold runs on the server: the host sees the client's progress grow while the client reports it holding.
        private IEnumerator WatchHoldCoroutine(string command, string repaired) {
            UseController clientUse = CoopEconomyHostHarness.ClientPlayer().GetComponent<UseController>();
            float largestProgress = 0f;
            _host.SendCommand(command, repaired);
            yield return _host.WaitForCoroutine(() => TrackProgress(clientUse, ref largestProgress) && _host.TryTakeReport(Steps.REPAIR_HOLDING), "the client's 'repair-holding' report");
            Assert.Greater(_host.LastReport.HoldProgress, 0f, "The client does not see its own hold running.");
            yield return _host.WaitForCoroutine(() => TrackProgress(clientUse, ref largestProgress) && _host.TryTakeReport(Steps.REPAIRED), "the client's 'repaired' report");
            Assert.Greater(largestProgress, 0f, $"The host never saw the client's hold on {repaired} running.");
            Assert.AreEqual(0f, clientUse.HoldProgress, "The client's hold is still running on the host after the repair.");
            Assert.AreEqual(0f, _host.LastReport.HoldProgress, "The client's hold is still running on its own peer after the repair.");
            Assert.IsEmpty(SessionQueries.Resolve<IReadOnlyShipConditionModel>().BrokenSockets, "A module is still broken on the host after the client's repair.");
            Debug.Log($"[CoopTest] the host saw the client's hold on {repaired} reach {largestProgress:0.00}");
            _host.AssertSameView(false, true);
        }

        // Always true: it only records the largest progress while the wait goes on.
        private static bool TrackProgress(UseController use, ref float largest) {
            largest = Mathf.Max(largest, use.HoldProgress);
            return true;
        }

        // A normal run's station drops no free module: after the landing only the bought items lie around.
        private IEnumerator RideLoopTogetherCoroutine() {
            _host.BeginStep("loop 1 with the client's engines");
            yield return _host.CommandCoroutine(Steps.BOARDED, Steps.COMMAND_BOARD);
            _host.SendCommand(Steps.COMMAND_RIDE);
            _host.Flight.PullLaunchLever(1);
            yield return _host.WaitForCoroutine(() => _host.Session.Model.Phase == ShipRunPhase.Cruise, "the cruise of loop 1");
            _host.Flight.FaceDestination();
            yield return _host.WaitForReportCoroutine(Steps.IN_CRUISE);
            _host.AssertSameView(true, true);
            _host.SendCommand(Steps.COMMAND_LAND);
            yield return _host.WaitForCoroutine(() => SessionQueries.IsAtStationAfterLoop(1), "Build after loop 1");
            yield return _host.WaitForReportCoroutine(Steps.LANDED);
            Assert.AreEqual(_stationModuleItems, _host.LastReport.LooseModuleItems, "The station of a normal run dropped a free module.");
            Assert.IsTrue(_host.IsClientOnDeckOnServer(), "The client is not on the deck by its server position after the landing.");
            _host.AssertSameView(false, true);
        }

        private ShipSocket FindSocket(string socketId) {
            foreach (ShipSocket socket in _host.Ship.Sockets) {
                if (socket.SocketId == socketId)
                    return socket;
            }

            throw new InvalidOperationException($"The ship has no socket {socketId}.");
        }

        private static IReadOnlyWalletModel Wallet =>
            SessionQueries.Resolve<IReadOnlyWalletModel>();

        private static long FindPrice(string entryId) {
            ShopCatalog catalog = SessionQueries.Resolve<ShopCatalog>();
            foreach (ModuleDefinition module in catalog.Modules) {
                if (module.Id == entryId)
                    return module.Price;
            }

            foreach (RepairToolDefinition tool in catalog.Tools) {
                if (tool.Id == entryId)
                    return tool.Price;
            }

            foreach (HandWeaponDefinition weapon in catalog.HandWeapons) {
                if (weapon.Id == entryId)
                    return weapon.Price;
            }

            throw new InvalidOperationException($"The shop does not sell {entryId}.");
        }

        // The loose items of a catalog entry in the host's scene: modules, tools and hand weapons.
        private static int CountItems(string entryId) {
            int count = 0;
            foreach (Grabbable item in Object.FindObjectsByType<Grabbable>(FindObjectsSortMode.None)) {
                if (item.CanBeGrabbed && IsEntryItem(item, entryId))
                    count++;
            }

            return count;
        }

        private static bool IsEntryItem(Grabbable item, string entryId) =>
            (item.TryGetComponent(out ShipItem module) && module.Module.Id == entryId)
            || (item.TryGetComponent(out RepairTool tool) && tool.Definition.Id == entryId)
            || (item.TryGetComponent(out HandWeaponItem weapon) && weapon.Definition.Id == entryId);
    }
}
#endif
