#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using Features.CharacterMovableModule.Scripts;
using Features.GrabModule.Scripts;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Generated;
using Features.ShopModule.Scripts.Network;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.PlayMode.LoopSmoke {
    // One station -> flight -> station loop of the host's ship: engines, boarding, the launch lever, the phases and
    // the checks after landing. Failure messages start with the loop number.
    public sealed class LoopSmokeFlight {
        private const float FLIGHT_TIMEOUT_SECONDS = 30f;
        private const float BOARD_DROP_HEIGHT = 1f;
        private const float BOARD_SETTLE_SECONDS = 1.5f;
        private const float FALL_SAMPLE_SECONDS = 1f;
        private const float MAX_FALL_DROP = 0.25f;
        private const float DECK_ON_PAD_TOLERANCE = 0.05f;
        private const float PLAYER_ON_SURFACE_TOLERANCE = 0.5f;
        // GameCore has no kill volume yet: a player this far below the pad top counts as lost.
        private const float KILL_DEPTH_BELOW_PAD = 2f;
        private const int REQUIRED_ENGINES = 2;
        private const string ROCKET_ENGINE_MODULE_ID = "rocket-engine";
        private const float MILLISECONDS_PER_SECOND = 1000f;

        private static readonly ShipRunPhase[] _expectedLoopPhases = {
            ShipRunPhase.Build,
            ShipRunPhase.Takeoff,
            ShipRunPhase.Cruise,
            ShipRunPhase.Landing,
            ShipRunPhase.Build
        };

        private readonly LoopSmokeSession _session;
        private readonly LoopSmokeFieldCheck _fieldCheck;
        private float _longestTakeoffFrame;

        private ShipBase Ship =>
            _session.Ship;

        private IReadOnlyShipRunModel Model =>
            _session.Model;

        private static NetworkIdentity LocalPlayer =>
            NetworkClient.localPlayer;

        public LoopSmokeFlight(LoopSmokeSession session, LoopSmokeFieldCheck fieldCheck) {
            _session = session;
            _fieldCheck = fieldCheck;
        }

        public void InstallRequiredEngines() {
            int installed = 0;
            foreach (ShipSocket socket in Ship.Sockets) {
                if (socket.RequiredForLaunch == false)
                    continue;

                Assert.AreEqual(ShipModuleType.Engine, socket.AcceptedType, $"Required socket {socket.name} does not take an engine.");
                Assert.IsTrue(socket.ServerTryInstall(ROCKET_ENGINE_MODULE_ID), $"Engine install failed on {socket.name}.");
                installed++;
            }

            Assert.AreEqual(REQUIRED_ENGINES, installed, "Unexpected number of required engine sockets.");
            Assert.IsTrue(Ship.CanLaunch, "The ship cannot launch with both engines installed.");
        }

        // Stands in for walking aboard: the player is put above the deck and lands on it under physics.
        public IEnumerator BoardPlayerCoroutine() {
            Bounds deck = Ship.DeckGeometry.Bounds;
            Vector3 target = new Vector3(deck.center.x, deck.max.y + BOARD_DROP_HEIGHT, deck.center.z);
            Rigidbody body = LocalPlayer.GetComponent<CharacterMovableBase>().Body;
            body.position = target;
            body.transform.position = target;
            body.linearVelocity = Vector3.zero;

            yield return new WaitForSeconds(BOARD_SETTLE_SECONDS);
            Assert.IsTrue(ContainsXZ(Ship.DeckGeometry.Bounds, LocalPlayer.transform.position, PLAYER_ON_SURFACE_TOLERANCE), "The player did not stay on the deck before launch.");
        }

        public IEnumerator FlyLoopCoroutine(int expectedLoopIndex, Func<IEnumerator> inCruise = null) {
            Assert.AreEqual(expectedLoopIndex - 1, Model.LoopIndex, $"Loop {expectedLoopIndex}: LoopIndex before launch.");
            _session.StartPhaseRecord();
            _longestTakeoffFrame = 0f;
            PullLaunchLever(expectedLoopIndex);

            yield return _session.WaitForCoroutine(IsCruisingAfterTakeoffFrame, FLIGHT_TIMEOUT_SECONDS, $"Cruise of loop {expectedLoopIndex}");
            Debug.Log($"[LoopSmoke] loop {expectedLoopIndex} takeoff: longest frame {_longestTakeoffFrame * MILLISECONDS_PER_SECOND:0.0} ms");
            Assert.IsTrue(LocalPlayer.GetComponent<ShipRider>().IsRiding, $"Loop {expectedLoopIndex}: the player is not riding the ship in cruise.");
            yield return _fieldCheck.AssertNewFieldStandsCoroutine(expectedLoopIndex);
            if (inCruise != null)
                yield return inCruise();

            yield return _session.WaitForCoroutine(IsBackInBuild, FLIGHT_TIMEOUT_SECONDS, $"Build after landing of loop {expectedLoopIndex}");
            CollectionAssert.AreEqual(_expectedLoopPhases, _session.Phases, $"Loop {expectedLoopIndex}: phase sequence {string.Join(" -> ", _session.Phases)}");
            Assert.AreEqual(expectedLoopIndex, Model.LoopIndex, $"Loop {expectedLoopIndex}: LoopIndex after landing.");
            yield return _fieldCheck.AssertFieldClearedCoroutine(expectedLoopIndex);
        }

        public IEnumerator AssertLandedSafelyCoroutine(int loop, ShipLandingPad launchPad, ShipLandingPad landingPad) {
            Assert.AreEqual(REQUIRED_ENGINES, CountInstalledEngines(), $"Loop {loop}: engines were lost on landing.");
            Assert.AreNotSame(launchPad, landingPad, $"Loop {loop}: the ship is not on a new pad after landing.");
            Assert.AreEqual(ShipRunAbortReason.None, Model.LastAbortReason, $"Loop {loop}: the flight ended as a wreck.");
            Assert.IsNotNull(landingPad.GetComponentInChildren<ShopKioskInteractable>(), $"Loop {loop}: the new station has no shop kiosk.");
            Assert.AreEqual(LoopSmokeSpawnCounts.CountSceneObjects<ShipLandingPad>(), LoopSmokeSpawnCounts.CountSceneObjects<ShopKioskInteractable>(), $"Loop {loop}: shop kiosks do not match the pads one to one.");
            LoopSmokeCrewInviteCheck.AssertOneTerminalPerPad();

            Bounds padBounds = PadBounds(landingPad);
            Bounds deck = Ship.DeckGeometry.Bounds;
            Assert.IsTrue(ContainsXZ(padBounds, deck.min, DECK_ON_PAD_TOLERANCE) && ContainsXZ(padBounds, deck.max, DECK_ON_PAD_TOLERANCE),
                $"Loop {loop}: the deck {deck} is not inside the pad {padBounds}.");

            float startY = LocalPlayer.transform.position.y;
            yield return new WaitForSeconds(FALL_SAMPLE_SECONDS);
            Vector3 player = LocalPlayer.transform.position;
            bool isOnDeck = ContainsXZ(deck, player, PLAYER_ON_SURFACE_TOLERANCE);
            bool isOnPad = ContainsXZ(padBounds, player, PLAYER_ON_SURFACE_TOLERANCE);
            Assert.IsTrue(isOnDeck || isOnPad, $"Loop {loop}: the player at {player} is neither on the deck {deck} nor on the pad {padBounds}.");
            Assert.Greater(player.y, padBounds.max.y - KILL_DEPTH_BELOW_PAD, $"Loop {loop}: the player is below the kill depth.");
            Assert.Less(startY - player.y, MAX_FALL_DROP, $"Loop {loop}: the player is falling: {startY} -> {player.y}.");
        }

        public void AssertPlayerOnDeck(int loop) {
            Bounds deck = Ship.DeckGeometry.Bounds;
            Vector3 player = LocalPlayer.transform.position;
            Assert.IsTrue(ContainsXZ(deck, player, PLAYER_ON_SURFACE_TOLERANCE), $"Loop {loop}: the player at {player} is not on the deck {deck}.");
        }

        public ShipLandingPad FindPadUnderShip() {
            ShipLandingPad closest = null;
            float best = float.MaxValue;
            foreach (ShipLandingPad pad in Object.FindObjectsByType<ShipLandingPad>(FindObjectsSortMode.None)) {
                float distance = (pad.LandingPoint.position - Ship.transform.position).sqrMagnitude;
                if (distance >= best)
                    continue;

                best = distance;
                closest = pad;
            }

            Assert.IsNotNull(closest, "No landing pad in the scene.");
            return closest;
        }

        private void PullLaunchLever(int loop) {
            ShipLaunchInteractable lever = Object.FindAnyObjectByType<ShipLaunchInteractable>();
            GrabController grab = LocalPlayer.GetComponentInChildren<GrabController>();
            Assert.IsTrue(lever.CanUse(LocalPlayer, grab), $"Loop {loop}: the launch lever cannot be used.");
            lever.ServerUse(LocalPlayer, grab);
        }

        private bool IsCruisingAfterTakeoffFrame() {
            _longestTakeoffFrame = Mathf.Max(_longestTakeoffFrame, Time.unscaledDeltaTime);
            return Model.Phase == ShipRunPhase.Cruise;
        }

        private bool IsBackInBuild() =>
            _session.Phases.Count > 1 && Model.Phase == ShipRunPhase.Build;

        private int CountInstalledEngines() {
            int count = 0;
            foreach (ShipSocket socket in Ship.Sockets) {
                if (socket.RequiredForLaunch && socket.InstalledModuleId == ROCKET_ENGINE_MODULE_ID)
                    count++;
            }

            return count;
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
    }
}
#endif
