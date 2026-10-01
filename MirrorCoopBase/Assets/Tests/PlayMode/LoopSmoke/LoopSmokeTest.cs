#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Collections.Generic;
using Features.CharacterMovableModule.Scripts;
using Features.GameCoreModule.Scripts.Constants;
using Features.GameFlowStateMachineModule.Scripts;
using Features.GameFlowStateMachineModule.Scripts.States;
using Features.GrabModule.Scripts;
using Features.LobbyModule.Scripts;
using Features.MenuModule.Scripts;
using Features.ShipModule.Scripts;
using Game.Connection;
using Mirror;
using NUnit.Framework;
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
        private const float BOARD_DROP_HEIGHT = 1f;
        private const float BOARD_SETTLE_SECONDS = 1.5f;
        private const float FALL_SAMPLE_SECONDS = 1f;
        private const float MAX_FALL_DROP = 0.25f;
        private const float DECK_ON_PAD_TOLERANCE = 0.05f;
        private const float PLAYER_ON_SURFACE_TOLERANCE = 0.5f;
        // GameCore has no kill volume yet: a player this far below the pad top counts as lost.
        private const float KILL_DEPTH_BELOW_PAD = 2f;
        private const int REQUIRED_ENGINES = 2;
        // Only timings are shortened; distances, heights and pads stay as shipped.
        private const string FAST_TIMINGS_JSON =
            "{\"_takeoffSeconds\":1.5,\"_cruiseSeconds\":2,\"_landingSeconds\":1.5,\"_perLoopCruiseSeconds\":0}";

        private static readonly ShipRunPhase[] _expectedLoopPhases = {
            ShipRunPhase.Build,
            ShipRunPhase.Takeoff,
            ShipRunPhase.Cruise,
            ShipRunPhase.Landing,
            ShipRunPhase.Build
        };

        private readonly List<ShipRunPhase> _phases = new List<ShipRunPhase>();

        private LoopSmokeErrorLog _errorLog;
        private ShipRunConfig _fastConfig;
        private ShipRunModel _model;
        private ShipBase _ship;

        [SetUp]
        public void SetUp() {
            Assert.IsFalse(ProjectContext.HasInstance, "ProjectContext exists before the test: the config override cannot be bound.");
            _errorLog = new LoopSmokeErrorLog();
            ProjectContext.PostInstall += OnProjectContextPostInstall;
        }

        [TearDown]
        public void TearDown() {
            ProjectContext.PostInstall -= OnProjectContextPostInstall;
            _errorLog.Dispose();
            if (_fastConfig != null)
                Object.Destroy(_fastConfig);
        }

        [UnityTest, Timeout(TEST_TIMEOUT_MS)]
        public IEnumerator WhenHostFliesTwoLoops_AndLeaves_ThenShipKeepsEnginesOnEachPadAndMenuReturns() {
            // LoopSmokeErrorLog fails the test with the full list and its allow-list instead.
            LogAssert.ignoreFailingMessages = true;
            yield return EnterGameAsHostCoroutine();
            InstallRequiredEngines();
            yield return BoardPlayerCoroutine();

            ShipLandingPad startPad = FindPadUnderShip();
            yield return FlyLoopCoroutine(1);
            ShipLandingPad firstPad = FindPadUnderShip();
            yield return AssertLandedSafelyCoroutine(startPad, firstPad);

            yield return FlyLoopCoroutine(2);
            yield return AssertLandedSafelyCoroutine(firstPad, FindPadUnderShip());

            yield return LeaveToMenuCoroutine();
            Assert.AreEqual(0, _errorLog.Count, $"Errors were logged during the loop:\n{_errorLog.Describe()}");
        }

        private IEnumerator EnterGameAsHostCoroutine() {
            SceneManager.LoadScene(SceneNames.Bootstrap);

            yield return WaitForCoroutine(IsMenuReady, MENU_TIMEOUT_SECONDS, "the menu with a bound Host button");
            Object.FindAnyObjectByType<MenuSessionViewBase>().OnHostClicked.Invoke();

            yield return WaitForCoroutine(IsLobbyReady, LOBBY_TIMEOUT_SECONDS, "the lobby with an authenticated host client and a bound Start button");
            Object.FindAnyObjectByType<StartGameViewBase>().OnStartClicked.Invoke();

            yield return WaitForCoroutine(IsGameReady, GAME_TIMEOUT_SECONDS, "GameScene with the ship, the local player and the Build phase");
            Assert.AreSame(_fastConfig, ProjectContext.Instance.Container.Resolve<ShipRunConfig>(), "The test ShipRunConfig override is not bound.");
        }

        private void InstallRequiredEngines() {
            int installed = 0;
            foreach (ShipSocket socket in _ship.Sockets) {
                if (socket.RequiredForLaunch == false)
                    continue;

                Assert.AreEqual(ShipModuleType.Engine, socket.AcceptedType, $"Required socket {socket.name} does not take an engine.");
                Assert.IsTrue(socket.ServerTryInstall(ShipModuleType.Engine, ItemViewId.Engine), $"Engine install failed on {socket.name}.");
                installed++;
            }

            Assert.AreEqual(REQUIRED_ENGINES, installed, "Unexpected number of required engine sockets.");
            Assert.IsTrue(_ship.CanLaunch, "The ship cannot launch with both engines installed.");
        }

        // Stands in for walking aboard: the player is put above the deck and lands on it under physics.
        private IEnumerator BoardPlayerCoroutine() {
            Bounds deck = _ship.DeckBounds;
            Vector3 target = new Vector3(deck.center.x, deck.max.y + BOARD_DROP_HEIGHT, deck.center.z);
            Rigidbody body = LocalPlayer().GetComponent<CharacterMovableBase>().Body;
            body.position = target;
            body.transform.position = target;
            body.linearVelocity = Vector3.zero;

            yield return new WaitForSeconds(BOARD_SETTLE_SECONDS);
            Assert.IsTrue(ContainsXZ(_ship.DeckBounds, LocalPlayer().transform.position, PLAYER_ON_SURFACE_TOLERANCE), "The player did not stay on the deck before launch.");
        }

        private IEnumerator FlyLoopCoroutine(int expectedLoopIndex) {
            Assert.AreEqual(expectedLoopIndex - 1, _model.LoopIndex, "LoopIndex before launch.");
            _phases.Clear();
            RecordPhase();
            PullLaunchLever();

            yield return WaitForCoroutine(IsCruising, FLIGHT_TIMEOUT_SECONDS, $"Cruise of loop {expectedLoopIndex}");
            Assert.IsTrue(LocalPlayer().GetComponent<ShipRider>().IsRiding, "The player is not riding the ship in cruise.");

            yield return WaitForCoroutine(IsBackInBuild, FLIGHT_TIMEOUT_SECONDS, $"Build after landing of loop {expectedLoopIndex}");
            CollectionAssert.AreEqual(_expectedLoopPhases, _phases, $"Phase sequence of loop {expectedLoopIndex}: {string.Join(" -> ", _phases)}");
            Assert.AreEqual(expectedLoopIndex, _model.LoopIndex, "LoopIndex after landing.");
        }

        private void PullLaunchLever() {
            ShipLaunchInteractable lever = Object.FindAnyObjectByType<ShipLaunchInteractable>();
            NetworkIdentity user = LocalPlayer();
            GrabController grab = user.GetComponentInChildren<GrabController>();
            Assert.IsTrue(lever.CanUse(user, grab), "The launch lever cannot be used.");
            lever.ServerUse(user, grab);
        }

        private IEnumerator AssertLandedSafelyCoroutine(ShipLandingPad launchPad, ShipLandingPad landingPad) {
            Assert.AreEqual(REQUIRED_ENGINES, CountInstalledEngines(), "Engines were lost on landing.");
            Assert.AreNotSame(launchPad, landingPad, "The ship is not on a new pad after landing.");
            Assert.AreEqual(ShipRunAbortReason.None, _model.LastAbortReason, "The flight ended as a wreck.");

            Bounds padBounds = PadBounds(landingPad);
            Bounds deck = _ship.DeckBounds;
            Assert.IsTrue(ContainsXZ(padBounds, deck.min, DECK_ON_PAD_TOLERANCE) && ContainsXZ(padBounds, deck.max, DECK_ON_PAD_TOLERANCE),
                $"The deck {deck} is not inside the pad {padBounds}.");

            float startY = LocalPlayer().transform.position.y;
            yield return new WaitForSeconds(FALL_SAMPLE_SECONDS);
            Vector3 player = LocalPlayer().transform.position;
            bool isOnDeck = ContainsXZ(deck, player, PLAYER_ON_SURFACE_TOLERANCE);
            bool isOnPad = ContainsXZ(padBounds, player, PLAYER_ON_SURFACE_TOLERANCE);
            Assert.IsTrue(isOnDeck || isOnPad, $"The player at {player} is neither on the deck {deck} nor on the pad {padBounds}.");
            Assert.Greater(player.y, padBounds.max.y - KILL_DEPTH_BELOW_PAD, "The player is below the kill depth.");
            Assert.Less(startY - player.y, MAX_FALL_DROP, $"The player is falling: {startY} -> {player.y}.");
        }

        private IEnumerator LeaveToMenuCoroutine() {
            Object.FindAnyObjectByType<LeaveSessionViewBase>().OnLeaveClicked.Invoke();
            yield return WaitForCoroutine(IsBackInMenu, LEAVE_TIMEOUT_SECONDS, "the menu after Leave");
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

        private bool IsMenuReady() {
            MenuSessionViewBase menu = Object.FindAnyObjectByType<MenuSessionViewBase>();
            return menu != null && menu.OnHostClicked != null;
        }

        // Start sends a scene change to every client; the host's own client drops it until it is authenticated.
        private bool IsLobbyReady() {
            StartGameViewBase start = Object.FindAnyObjectByType<StartGameViewBase>();
            bool isHostClientReady = NetworkServer.active && NetworkClient.ready && NetworkClient.connection.isAuthenticated;
            return isHostClientReady && start != null && start.OnStartClicked != null;
        }

        private bool IsGameReady() {
            if (NetworkClient.localPlayer == null || Object.FindAnyObjectByType<ShipRunDirector>() == null)
                return false;

            if (NetworkManager.singleton is ConnectionNetworkManager manager && manager.IsMapLoaded == false)
                return false;

            _ship = Object.FindAnyObjectByType<ShipBase>();
            _model = ProjectContext.Instance.Container.Resolve<ShipRunModel>();
            return _ship != null && _model.Phase == ShipRunPhase.Build;
        }

        private bool IsCruising() =>
            _model.Phase == ShipRunPhase.Cruise;

        private bool IsBackInBuild() =>
            _phases.Count > 1 && _model.Phase == ShipRunPhase.Build;

        private bool IsBackInMenu() {
            if (NetworkServer.active || NetworkClient.active || IsMenuReady() == false)
                return false;

            IGameFlowStateMachineService flow = FindGameFlow();
            return flow.IsTransitioning == false && flow.CurrentStateType == typeof(MenuGameFlowState);
        }

        private int CountInstalledEngines() {
            int count = 0;
            foreach (ShipSocket socket in _ship.Sockets) {
                if (socket.RequiredForLaunch && socket.InstalledView == ItemViewId.Engine)
                    count++;
            }

            return count;
        }

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

        private static bool ContainsXZ(Bounds bounds, Vector3 point, float tolerance) =>
            point.x >= bounds.min.x - tolerance && point.x <= bounds.max.x + tolerance
            && point.z >= bounds.min.z - tolerance && point.z <= bounds.max.z + tolerance;

        private static NetworkIdentity LocalPlayer() =>
            NetworkClient.localPlayer;

        private static IGameFlowStateMachineService FindGameFlow() {
            foreach (SceneContext context in Object.FindObjectsByType<SceneContext>(FindObjectsSortMode.None)) {
                if (context.Container.HasBinding<IGameFlowStateMachineService>())
                    return context.Container.Resolve<IGameFlowStateMachineService>();
            }

            throw new InvalidOperationException("No scene context binds IGameFlowStateMachineService.");
        }

        private void OnProjectContextPostInstall() {
            DiContainer container = ProjectContext.Instance.Container;
            _fastConfig = Object.Instantiate(container.Resolve<ShipRunConfig>());
            JsonUtility.FromJsonOverwrite(FAST_TIMINGS_JSON, _fastConfig);
            container.Unbind<ShipRunConfig>();
            container.Bind<ShipRunConfig>().FromInstance(_fastConfig);
        }
    }
}
#endif
