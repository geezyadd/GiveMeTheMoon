#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Collections.Generic;
using Features.CharacterMovableModule.Scripts;
using Features.FlightFieldModule.Scripts.Structures;
using Features.GameCoreModule.Contracts;
using Features.LobbyModule.Scripts;
using Features.MenuModule.Scripts;
using Features.PlayerLifeModule.Scripts;
using Features.PlayerProfileModule.Scripts;
using Features.ShipModule.Scripts;
using Features.ShopModule.Scripts.Network;
using Features.ShipModule.Scripts.Generated;
using Game.Connection;
using Mirror;
using NUnit.Framework;
using Tests.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Zenject;
using Object = UnityEngine.Object;

namespace Tests.PlayMode.LoopSmoke {
    public sealed class LoopSmokeTest {
        private const int TEST_TIMEOUT_MS = 180000;
        private const float MENU_TIMEOUT_SECONDS = 60f;
        private const float LOBBY_TIMEOUT_SECONDS = 30f;
        private const float GAME_TIMEOUT_SECONDS = 60f;
        private const float FLIGHT_TIMEOUT_SECONDS = 30f;
        private const float LEAVE_TIMEOUT_SECONDS = 30f;
        // Covers PlayerLifeConfiguration.AllDeadReturnDelay plus the lobby load and the GameScene unload.
        private const float RETURN_TO_LOBBY_TIMEOUT_SECONDS = 30f;
        private const float BOARD_SETTLE_SECONDS = 1.5f;
        private const float FALL_SAMPLE_SECONDS = 1f;
        private const float MAX_FALL_DROP = 0.25f;
        private const float DECK_ON_PAD_TOLERANCE = 0.05f;
        private const float PLAYER_ON_SURFACE_TOLERANCE = 0.5f;
        // GameCore has no kill volume yet: a player this far below the pad top counts as lost.
        private const float KILL_DEPTH_BELOW_PAD = 2f;
        // Five flights in a row in run 2: the field pool and the Mirror field objects must not grow.
        private const int RUN_TWO_LOOPS = 5;
        // The run 2 flight that shifts the world in cruise.
        private const int WORLD_SHIFT_LOOP = 3;
        private const float MILLISECONDS_PER_SECOND = 1000f;

        private static readonly ShipRunPhase[] _expectedLoopPhases = {
            ShipRunPhase.Build,
            ShipRunPhase.Takeoff,
            ShipRunPhase.Cruise,
            ShipRunPhase.Landing,
            ShipRunPhase.Build
        };

        private readonly List<ShipRunPhase> _phases = new List<ShipRunPhase>();
        private readonly HashSet<uint> _firstRunObjects = new HashSet<uint>();

        private LoopSmokeErrorLog _errorLog;
        private LoopSmokeSteamLobbySpy _steamLobby;
        private int _lobbyRuntimeObjects;
        private ShipRunConfig _fastConfig;
        private FieldStructureCatalog _fieldCatalog;
        private IReadOnlyShipRunModel _model;
        private ShipBase _ship;
        private LoopSmokeFieldCheck _fieldCheck;
        private float _longestTakeoffFrame;

        [SetUp]
        public void SetUp() {
            Assert.IsFalse(ProjectContext.HasInstance, "ProjectContext exists before the test: the config override cannot be bound.");
            _errorLog = new LoopSmokeErrorLog();
            _steamLobby = new LoopSmokeSteamLobbySpy();
            _steamLobby.BindIntoSceneContexts();
            ProjectContext.PostInstall += OnProjectContextPostInstall;
        }

        [TearDown]
        public void TearDown() {
            ProjectContext.PostInstall -= OnProjectContextPostInstall;
            _errorLog.Dispose();
            _steamLobby.Dispose();
            if (_fastConfig != null)
                Object.Destroy(_fastConfig);

            if (_fieldCatalog != null)
                Object.Destroy(_fieldCatalog);
        }

        [UnityTest, Timeout(TEST_TIMEOUT_MS)]
        public IEnumerator WhenHostFliesTwoLoops_AndAllDieAndStartAgain_ThenSecondRunStartsCleanAndMenuReturns() {
            // LoopSmokeErrorLog fails the test with the full list and its allow-list instead.
            LogAssert.ignoreFailingMessages = true;
            yield return EnterGameAsHostCoroutine();
            LoopSmokeHostSteps.InstallRequiredEngines(_ship);
            yield return BoardPlayerCoroutine();
            int startStationItems = CountSceneObjects<ShipItem>();
            Assert.Greater(startStationItems, 0, "Run 1 has no items at the start station.");
            yield return new LoopSmokeHandCheck(_ship).ReinstallRadarByHandCoroutine();

            ShipLandingPad startPad = FindPadUnderShip();
            yield return FlyLoopCoroutine(1);
            ShipLandingPad firstPad = FindPadUnderShip();
            yield return AssertLandedSafelyCoroutine(startPad, firstPad);
            int firstLandingSpawns = CountRuntimeNonPlayerObjects(NetworkServer.spawned.Values);
            LogSpawnedCounts("run 1 after the first landing");
            Assert.Greater(firstLandingSpawns, 0, "Run 1 spawned no pad or drops on landing.");
            yield return new LoopSmokeReviveCheck(_ship).BuyBackCrewCoroutine(firstPad);

            LoopSmokeRejoinCheck rejoin = new LoopSmokeRejoinCheck(_model);
            yield return rejoin.JoinAtStationAndDieCoroutine();
            yield return FlyLoopCoroutine(2, rejoin.LeaveAndKnockInFlightCoroutine);
            yield return AssertLandedSafelyCoroutine(firstPad, FindPadUnderShip());
            yield return rejoin.ComeBackAtStationCoroutine();

            RememberFirstRunObjects();
            LogSpawnedCounts("run 1 before returning to the lobby");
            yield return ReturnToLobbyByAllDeadCoroutine();
            LogSpawnedCounts("lobby after run 1");
            AssertFirstRunObjectsAreGone();

            yield return StartGameCoroutine();
            LoopSmokeHostSteps.InstallRequiredEngines(_ship);
            yield return BoardPlayerCoroutine();
            Assert.AreEqual(startStationItems, CountSceneObjects<ShipItem>(), "Run 2 does not start with the same station items as run 1.");
            ShipLandingPad secondRunStartPad = FindPadUnderShip();
            LoopSmokeCrewInviteCheck crewInvite = new LoopSmokeCrewInviteCheck(_steamLobby);
            yield return FlyLoopCoroutine(1, crewInvite.CheckClosedInFlightCoroutine);
            yield return AssertLandedSafelyCoroutine(secondRunStartPad, FindPadUnderShip());
            yield return crewInvite.CheckAtStationCoroutine(FindPadUnderShip());
            LogSpawnedCounts("run 2 after the first landing");
            Assert.AreEqual(firstLandingSpawns, CountRuntimeNonPlayerObjects(NetworkServer.spawned.Values), "Run 2 does not spawn the same pad and drops on landing as run 1.");
            // The crew invite check left the player at the pad's terminal: back on the deck before the next flights.
            yield return BoardPlayerCoroutine();
            yield return FlyMoreLoopsCoroutine(2, RUN_TWO_LOOPS);
            _fieldCheck.AssertNetworkedPathWasChecked();
            yield return new LoopSmokeShopCheck().BuyAtKioskAndWalkAwayCoroutine(FindPadUnderShip());

            yield return LeaveToMenuCoroutine();
            yield return new LoopSmokeRejectCheck().JoinRejectedInFlightCoroutine();
            Assert.AreEqual(0, _errorLog.Count, $"Errors were logged during the loop:\n{_errorLog.Describe()}");
        }

        private IEnumerator EnterGameAsHostCoroutine() {
            SceneManager.LoadScene(SceneNames.Bootstrap);

            yield return WaitForCoroutine(SessionQueries.IsMenuReady, MENU_TIMEOUT_SECONDS, "the menu with a bound Host button");
            Object.FindAnyObjectByType<MenuSessionViewBase>().OnHostClicked.Invoke();

            yield return WaitForCoroutine(LoopSmokeHostSteps.IsLobbyReady, LOBBY_TIMEOUT_SECONDS, "the lobby with an authenticated host client and a bound Start button");
            _lobbyRuntimeObjects = CountRuntimeNonPlayerObjects(NetworkServer.spawned.Values);
            LogSpawnedCounts("lobby before run 1");
            yield return new LoopSmokeCrewInviteCheck(_steamLobby).CheckInLobbyCoroutine();
            yield return StartGameCoroutine();
            Assert.AreSame(_fastConfig, ProjectContext.Instance.Container.Resolve<ShipRunConfig>(), "The test ShipRunConfig override is not bound.");
            _fieldCheck = new LoopSmokeFieldCheck(ProjectContext.Instance.Container);
        }

        private IEnumerator StartGameCoroutine() {
            LoopSmokeHostSteps.ClickStart();
            yield return WaitForCoroutine(IsGameReady, GAME_TIMEOUT_SECONDS, "GameScene with the ship, the local player and the Build phase");
        }

        // The real way back: PlayerLifeSystem's all-dead rule sends everyone to the lobby.
        private IEnumerator ReturnToLobbyByAllDeadCoroutine() {
            LocalPlayer().GetComponent<IDamageable>().ServerKill(DamageType.Generic);
            yield return WaitForCoroutine(IsBackInLobby, RETURN_TO_LOBBY_TIMEOUT_SECONDS, "the lobby after everyone died, with GameScene unloaded");
            _ship = null;
            _model = null;
        }

        private void RememberFirstRunObjects() {
            _firstRunObjects.Clear();
            foreach (NetworkIdentity identity in NetworkServer.spawned.Values) {
                if (IsRuntimeNonPlayer(identity))
                    _firstRunObjects.Add(identity.netId);
            }

            Assert.Greater(_firstRunObjects.Count, _lobbyRuntimeObjects, "Run 1 spawned no runtime objects: the leak check would prove nothing.");
        }

        private void AssertFirstRunObjectsAreGone() {
            List<string> leftovers = new List<string>();
            CollectFirstRunLeftovers(NetworkServer.spawned.Values, "server", leftovers);
            CollectFirstRunLeftovers(NetworkClient.spawned.Values, "client", leftovers);
            Assert.IsEmpty(leftovers, $"Run 1 objects survived the return to the lobby: {string.Join(", ", leftovers)}");
            Assert.AreEqual(_lobbyRuntimeObjects, CountRuntimeNonPlayerObjects(NetworkServer.spawned.Values), "Server runtime objects in the lobby differ from before run 1.");
            Assert.AreEqual(_lobbyRuntimeObjects, CountRuntimeNonPlayerObjects(NetworkClient.spawned.Values), "Client runtime objects in the lobby differ from before run 1.");
            Assert.AreEqual(0, CountSceneObjects<ShipLandingPad>(), "Landing pads survived the return to the lobby.");
            Assert.AreEqual(0, CountSceneObjects<ShipItem>(), "Ship items survived the return to the lobby.");
        }

        private void CollectFirstRunLeftovers(IEnumerable<NetworkIdentity> spawned, string side, List<string> leftovers) {
            foreach (NetworkIdentity identity in spawned) {
                if (identity != null && _firstRunObjects.Contains(identity.netId))
                    leftovers.Add($"{side}:{identity.name}#{identity.netId}");
            }
        }

        private static int CountSceneObjects<T>() where T : Component =>
            Object.FindObjectsByType<T>(FindObjectsSortMode.None).Length;

        private static int CountRuntimeNonPlayerObjects(IEnumerable<NetworkIdentity> spawned) {
            int count = 0;
            foreach (NetworkIdentity identity in spawned) {
                if (IsRuntimeNonPlayer(identity))
                    count++;
            }

            return count;
        }

        private static bool IsRuntimeNonPlayer(NetworkIdentity identity) =>
            identity != null && identity.sceneId == 0 && identity.GetComponent<PlayerProfile>() == null;

        private static void LogSpawnedCounts(string moment) =>
            Debug.Log($"[LoopSmoke] {moment}: server spawned {NetworkServer.spawned.Count} "
                      + $"(runtime non-player {CountRuntimeNonPlayerObjects(NetworkServer.spawned.Values)}), "
                      + $"client spawned {NetworkClient.spawned.Count} "
                      + $"(runtime non-player {CountRuntimeNonPlayerObjects(NetworkClient.spawned.Values)})");

        // Stands in for walking aboard: the player is put above the deck and lands on it under physics.
        private IEnumerator BoardPlayerCoroutine() {
            ShipDeckBoarding.PutAboveDeck(LocalPlayer().GetComponent<CharacterMovableBase>(), _ship);

            yield return new WaitForSeconds(BOARD_SETTLE_SECONDS);
            Assert.IsTrue(SessionQueries.ContainsXZ(_ship.DeckGeometry.Bounds, LocalPlayer().transform.position, PLAYER_ON_SURFACE_TOLERANCE), "The player did not stay on the deck before launch.");
        }

        private IEnumerator FlyLoopCoroutine(int expectedLoopIndex, Func<IEnumerator> inCruise = null) {
            Assert.AreEqual(expectedLoopIndex - 1, _model.LoopIndex, "LoopIndex before launch.");
            _phases.Clear();
            RecordPhase();
            _longestTakeoffFrame = 0f;
            LoopSmokeHostSteps.PullLaunchLever();

            yield return WaitForCoroutine(IsCruisingAfterTakeoffFrame, FLIGHT_TIMEOUT_SECONDS, $"Cruise of loop {expectedLoopIndex}");
            Debug.Log($"[LoopSmoke] loop {expectedLoopIndex} takeoff: longest frame {_longestTakeoffFrame * MILLISECONDS_PER_SECOND:0.0} ms");
            Assert.IsTrue(LocalPlayer().GetComponent<ShipRider>().IsRiding, "The player is not riding the ship in cruise.");
            yield return _fieldCheck.AssertNewFieldStandsCoroutine(expectedLoopIndex);
            if (inCruise != null)
                yield return inCruise();


            yield return WaitForCoroutine(IsBackInBuild, FLIGHT_TIMEOUT_SECONDS, $"Build after landing of loop {expectedLoopIndex}");
            CollectionAssert.AreEqual(_expectedLoopPhases, _phases, $"Phase sequence of loop {expectedLoopIndex}: {string.Join(" -> ", _phases)}");
            Assert.AreEqual(expectedLoopIndex, _model.LoopIndex, "LoopIndex after landing.");
            yield return _fieldCheck.AssertFieldClearedCoroutine(expectedLoopIndex);
        }

        private IEnumerator FlyMoreLoopsCoroutine(int firstLoopIndex, int lastLoopIndex) {
            for (int loopIndex = firstLoopIndex; loopIndex <= lastLoopIndex; loopIndex++) {
                ShipLandingPad launchPad = FindPadUnderShip();
                yield return FlyLoopCoroutine(loopIndex, loopIndex == WORLD_SHIFT_LOOP ? ShiftWorldInCruiseCoroutine : null);
                yield return AssertLandedSafelyCoroutine(launchPad, FindPadUnderShip());
            }
        }

        private IEnumerator ShiftWorldInCruiseCoroutine() =>
            _fieldCheck.AssertFieldFollowsWorldShiftCoroutine(_ship.transform, SessionQueries.Resolve<IShipWorldShiftDebug>());

        private IEnumerator AssertLandedSafelyCoroutine(ShipLandingPad launchPad, ShipLandingPad landingPad) {
            Assert.AreEqual(LoopSmokeHostSteps.REQUIRED_ENGINES, LoopSmokeHostSteps.CountInstalledEngines(_ship), "Engines were lost on landing.");
            Assert.AreNotSame(launchPad, landingPad, "The ship is not on a new pad after landing.");
            Assert.AreEqual(ShipRunAbortReason.None, _model.LastAbortReason, "The flight ended as a wreck.");
            Assert.IsNotNull(landingPad.GetComponentInChildren<ShopKioskInteractable>(), "The new station has no shop kiosk.");
            Assert.AreEqual(CountSceneObjects<ShipLandingPad>(), CountSceneObjects<ShopKioskInteractable>(), "Shop kiosks do not match the pads one to one.");
            LoopSmokeCrewInviteCheck.AssertOneTerminalPerPad();

            Bounds padBounds = PadBounds(landingPad);
            Bounds deck = _ship.DeckGeometry.Bounds;
            Assert.IsTrue(SessionQueries.ContainsXZ(padBounds, deck.min, DECK_ON_PAD_TOLERANCE) && SessionQueries.ContainsXZ(padBounds, deck.max, DECK_ON_PAD_TOLERANCE),
                $"The deck {deck} is not inside the pad {padBounds}.");

            float startY = LocalPlayer().transform.position.y;
            yield return new WaitForSeconds(FALL_SAMPLE_SECONDS);
            Vector3 player = LocalPlayer().transform.position;
            bool isOnDeck = SessionQueries.ContainsXZ(deck, player, PLAYER_ON_SURFACE_TOLERANCE);
            bool isOnPad = SessionQueries.ContainsXZ(padBounds, player, PLAYER_ON_SURFACE_TOLERANCE);
            Assert.IsTrue(isOnDeck || isOnPad, $"The player at {player} is neither on the deck {deck} nor on the pad {padBounds}.");
            Assert.Greater(player.y, padBounds.max.y - KILL_DEPTH_BELOW_PAD, "The player is below the kill depth.");
            Assert.Less(startY - player.y, MAX_FALL_DROP, $"The player is falling: {startY} -> {player.y}.");
        }

        private IEnumerator LeaveToMenuCoroutine() {
            Object.FindAnyObjectByType<LeaveSessionViewBase>().OnLeaveClicked.Invoke();
            yield return WaitForCoroutine(SessionQueries.IsBackInMenu, LEAVE_TIMEOUT_SECONDS, "the menu after Leave");
        }

        private IEnumerator WaitForCoroutine(Func<bool> condition, float timeoutSeconds, string target) {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (true) {
                RecordPhase();
                if (condition())
                    yield break;

                if (Time.realtimeSinceStartup > deadline)
                    Assert.Fail($"Timed out after {timeoutSeconds} s waiting for {target}. Phases: {string.Join(" -> ", _phases)}. Errors:\n{_errorLog.Describe()}");

                yield return null;
            }
        }

        private void RecordPhase() {
            if (_model == null)
                return;

            if (_phases.Count == 0 || _phases[_phases.Count - 1] != _model.Phase)
                _phases.Add(_model.Phase);
        }

        private bool IsGameReady() {
            if (SessionQueries.IsGameAtStation() == false)
                return false;

            _ship = Object.FindAnyObjectByType<ShipBase>();
            _model = ProjectContext.Instance.Container.Resolve<IReadOnlyShipRunModel>();
            return true;
        }

        private static bool IsBackInLobby() {
            ConnectionNetworkManager manager = SessionQueries.NetworkManager;
            if (manager.IsMapLoaded == false || manager.IsChangingMap)
                return false;

            Scene game = SceneManager.GetSceneByName(manager.GameSceneName);
            bool isGameUnloaded = game.IsValid() == false || game.isLoaded == false;
            return isGameUnloaded && SceneManager.GetActiveScene().name == manager.LobbySceneName && NetworkClient.localPlayer != null;
        }

        private bool IsCruising() =>
            _model.Phase == ShipRunPhase.Cruise;

        private bool IsCruisingAfterTakeoffFrame() {
            _longestTakeoffFrame = Mathf.Max(_longestTakeoffFrame, Time.unscaledDeltaTime);
            return IsCruising();
        }

        private bool IsBackInBuild() =>
            _phases.Count > 1 && _model.Phase == ShipRunPhase.Build;

        private ShipLandingPad FindPadUnderShip() {
            ShipLandingPad closest = null;
            float best = float.MaxValue;
            foreach (ShipLandingPad pad in Object.FindObjectsByType<ShipLandingPad>(FindObjectsSortMode.None)) {
                float distance = (pad.LandingPoint.position - _ship.transform.position).sqrMagnitude;
                if (distance >= best)
                    continue;

                best = distance;
                closest = pad;
            }

            Assert.IsNotNull(closest, "No landing pad in the scene.");
            return closest;
        }

        private static Bounds PadBounds(ShipLandingPad pad) {
            Bounds bounds = new Bounds(pad.LandingPoint.position, Vector3.zero);
            foreach (Collider collider in pad.transform.root.GetComponentsInChildren<Collider>()) {
                if (collider.isTrigger == false)
                    bounds.Encapsulate(collider.bounds);
            }

            return bounds;
        }

        private static NetworkIdentity LocalPlayer() =>
            SessionQueries.LocalPlayer;

        // The field catalog gets the debug networked beacon, so every flight also spawns Mirror field objects.
        private void OnProjectContextPostInstall() {
            DiContainer container = ProjectContext.Instance.Container;
            _fastConfig = LoopSmokeHostSteps.BindFastTimings(container);
            _fieldCatalog = LoopSmokeFieldCheck.CreateCatalogWithNetworkedStructure(container.Resolve<FieldStructureCatalog>());
            container.Unbind<FieldStructureCatalog>();
            container.Bind<FieldStructureCatalog>().FromInstance(_fieldCatalog);
        }
    }
}
#endif
