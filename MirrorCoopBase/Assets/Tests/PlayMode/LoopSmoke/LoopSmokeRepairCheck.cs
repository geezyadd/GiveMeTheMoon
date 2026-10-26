#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using Features.CameraModule.Scripts;
using Features.GrabModule.Scripts;
using Features.GrabModule.Scripts.Hold;
using Features.ShipDamageModule.Scripts.Damage;
using Features.ShipDamageModule.Scripts.Debug;
using Features.ShipDamageModule.Scripts.Repair;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Contracts;
using Features.ShipModule.Scripts.Debug;
using Features.ShipModule.Scripts.Generated;
using Mirror;
using NUnit.Framework;
using Tests.Shared;
using UnityEngine;

namespace Tests.PlayMode.LoopSmoke {
    // The mechanic's job through the real hand: the debug window's break entry breaks an engine at the start station, the
    // synced speed and condition show it; the wrench from the deck rack repairs it by holding interact on the socket
    // (an early release resets the hold); the hammer patches a breach and gives hull HP back. In the first cruise a
    // module breaks and a breach opens again, and the landing repairs both.
    public sealed class LoopSmokeRepairCheck {
        private const string ENGINE_SOCKET = "engine-left";
        private const string STATION_BREACH = "breach-04";
        // The trigger over a broken module the aim finds it by (ShipPlatform prefab).
        private const string REPAIR_VOLUME = "RepairVolume";
        private const float HULL_HIT = 5f;
        private const float EARLY_RELEASE_SECONDS = 0.5f;
        private const float COMMAND_TIMEOUT_SECONDS = 3f;
        private const float REPAIR_TIMEOUT_SECONDS = 8f;
        private const float DROP_SETTLE_SECONDS = 1f;
        private const float TOLERANCE = 0.01f;

        private readonly ShipBase _ship;

        public LoopSmokeRepairCheck(LoopSmokeContext context) =>
            _ship = context.Session.Ship;

        private static NetworkIdentity Player => NetworkClient.localPlayer;
        private static LocalPlayerInteraction Interaction => Player.GetComponent<LocalPlayerInteraction>();
        private static UseController Use => Player.GetComponent<UseController>();
        private static GrabController Hand => Player.GetComponent<GrabController>();

        [LoopSmokeStep(LoopSmokePhase.BeforeTakeoff, 1, 1, 20)]
        public IEnumerator RepairEngineAndBreachAtStationCoroutine() {
            try {
                CameraLookDriver.DebugPitchOverride = true;
                yield return RepairEngineWithWrenchCoroutine();
                yield return PatchBreachWithHammerCoroutine();
            } finally {
                CameraLookDriver.DebugPitchOverride = false;
                CameraLookDriver.DebugPitch = 0f;
            }
        }

        // A rock hit of the cruise may already have broken something, so the random entry of the debug window is taken.
        [LoopSmokeStep(LoopSmokePhase.InCruise, 1, 1, 50)]
        public IEnumerator BreakInCruiseCoroutine() {
            SessionQueries.Resolve<IShipDamageDebug>().DebugBreakRandom();
            yield return null;
            IReadOnlyShipConditionModel condition = SessionQueries.Resolve<IReadOnlyShipConditionModel>();
            Assert.IsNotEmpty(condition.BrokenSockets, "Repair: no module is broken in cruise.");
            Assert.IsNotEmpty(condition.OpenBreaches, "Repair: no breach is open in cruise.");
        }

        [LoopSmokeStep(LoopSmokePhase.AfterLanding, 1, 1, 30)]
        public void AssertRepairedAfterLanding() {
            IReadOnlyShipConditionModel condition = SessionQueries.Resolve<IReadOnlyShipConditionModel>();
            Assert.IsEmpty(condition.BrokenSockets, "Repair: broken modules were not repaired on landing.");
            Assert.IsEmpty(condition.OpenBreaches, "Repair: breaches were not repaired on landing.");
        }

        private IEnumerator RepairEngineWithWrenchCoroutine() {
            IReadOnlyShipStatsModel stats = SessionQueries.Resolve<IReadOnlyShipStatsModel>();
            IShipModuleCondition condition = SessionQueries.Resolve<IShipModuleCondition>();
            float workingSpeed = stats.FlightSpeed;
            Assert.IsTrue(SessionQueries.Resolve<IShipDamageDebug>().DebugBreakModule(ENGINE_SOCKET), "Repair: the debug break did not break the engine.");
            yield return null;
            Assert.IsFalse(condition.IsWorking(ENGINE_SOCKET), "Repair: the broken engine still works in the synced condition.");
            Assert.Less(stats.FlightSpeed, workingSpeed - TOLERANCE, "Repair: a broken engine did not lower the flight speed.");

            yield return GrabToolCoroutine(ShipRepairKind.Module);
            ShipSocket socket = FindSocket(ENGINE_SOCKET);
            yield return LoopSmokeHandCheck.AimAtCoroutine(socket.transform.Find(REPAIR_VOLUME).position);
            Assert.IsInstanceOf<ShipModuleRepairInteractable>(Interaction.Target.Usable, $"Repair: the broken engine is not offered for repair with the wrench (looked at {Interaction.Target.LookedAt}, socket at {socket.transform.position}, player at {Player.transform.position}, held {Hand.Held}).");
            yield return ReleaseEarlyCoroutine(() => condition.IsWorking(ENGINE_SOCKET));
            yield return HoldUntilCoroutine(() => condition.IsWorking(ENGINE_SOCKET), "the engine repaired by holding interact");
            Assert.AreEqual(workingSpeed, stats.FlightSpeed, TOLERANCE, "Repair: the repaired engine did not bring the flight speed back.");
            yield return DropToolCoroutine();
        }

        private IEnumerator PatchBreachWithHammerCoroutine() {
            IReadOnlyShipStatsModel stats = SessionQueries.Resolve<IReadOnlyShipStatsModel>();
            IShipBreachCondition breaches = SessionQueries.Resolve<IShipBreachCondition>();
            SessionQueries.Resolve<IShipHullDebug>().DebugDamage(HULL_HIT);
            Assert.IsTrue(SessionQueries.Resolve<IShipDamageDebug>().DebugOpenBreach(STATION_BREACH), "Repair: the debug breach did not open.");
            yield return null;
            float damagedHull = stats.Hull;
            Assert.Less(damagedHull, stats.HullMax, "Repair: the debug hit did not lower the hull.");

            yield return GrabToolCoroutine(ShipRepairKind.Breach);
            ShipHullDamagePoint point = FindDamagePoint(STATION_BREACH);
            yield return LoopSmokeHandCheck.AimAtCoroutine(point.GetComponent<Collider>().bounds.center);
            Assert.IsInstanceOf<ShipBreachRepairInteractable>(Interaction.Target.Usable, "Repair: the open breach is not offered for repair with the hammer.");
            yield return HoldUntilCoroutine(() => breaches.IsOpen(STATION_BREACH) == false, "the breach patched by holding interact");
            Assert.Greater(stats.Hull, damagedHull, "Repair: a patched breach gave no hull HP back.");
            Assert.AreEqual(stats.HullMax, stats.Hull, TOLERANCE, "Repair: the patched breach did not give back the small hit.");
            yield return DropToolCoroutine();
        }

        private static IEnumerator GrabToolCoroutine(ShipRepairKind kind) {
            RepairTool tool = FindTool(kind);
            yield return LoopSmokeHandCheck.AimAtCoroutine(tool.GetComponent<Collider>().bounds.center);
            Grabbable item = tool.GetComponent<Grabbable>();
            Assert.AreSame(item, Interaction.Target.Grabbable, $"Repair: the {kind} tool from the rack is not under the crosshair (tool at {tool.transform.position}, player at {Player.transform.position}, looked at {Interaction.Target.LookedAt}).");
            Interaction.Interact();
            yield return WaitForCoroutine(() => Hand.Held == item, $"the {kind} tool in the hand", COMMAND_TIMEOUT_SECONDS);
        }

        // A hold released before its time repairs nothing and starts from zero next time.
        private static IEnumerator ReleaseEarlyCoroutine(Func<bool> isRepaired) {
            Interaction.Interact();
            yield return WaitForCoroutine(() => Use.HoldProgress > 0f, "the hold to start", COMMAND_TIMEOUT_SECONDS);
            Assert.IsTrue(SessionQueries.Resolve<HoldUseModel>().IsHolding, "Repair: the HUD model does not show the running hold.");
            yield return new WaitForSeconds(EARLY_RELEASE_SECONDS);
            Interaction.EndInteract();
            yield return WaitForCoroutine(() => Use.HoldProgress <= 0f, "the released hold to reset", COMMAND_TIMEOUT_SECONDS);
            Assert.IsFalse(isRepaired(), "Repair: a hold released early repaired anyway.");
        }

        private static IEnumerator HoldUntilCoroutine(Func<bool> isRepaired, string target) {
            Interaction.Interact();
            yield return WaitForCoroutine(isRepaired, target, REPAIR_TIMEOUT_SECONDS);
            Interaction.EndInteract();
            yield return WaitForCoroutine(() => Use.HoldProgress <= 0f, "the hold bar to clear after the repair", COMMAND_TIMEOUT_SECONDS);
        }

        private static IEnumerator DropToolCoroutine() {
            Interaction.ReleaseHeld();
            yield return WaitForCoroutine(() => Hand.IsHolding == false, "the hand to be empty after dropping the tool", COMMAND_TIMEOUT_SECONDS);
            yield return new WaitForSeconds(DROP_SETTLE_SECONDS);
        }

        private ShipSocket FindSocket(string socketId) {
            foreach (ShipSocket socket in _ship.Sockets) {
                if (socket.SocketId == socketId)
                    return socket;
            }

            throw new InvalidOperationException($"Repair: the ship has no socket {socketId}.");
        }

        private ShipHullDamagePoint FindDamagePoint(string pointId) {
            foreach (ShipHullDamagePoint point in SessionQueries.Resolve<IReadOnlyComponentRegistry<ShipHullDamagePoint>>().Items) {
                if (point.PointId == pointId)
                    return point;
            }

            throw new InvalidOperationException($"Repair: the ship has no hull damage point {pointId}.");
        }

        private static RepairTool FindTool(ShipRepairKind kind) {
            RepairTool[] tools = UnityEngine.Object.FindObjectsByType<RepairTool>(FindObjectsSortMode.None);
            foreach (RepairTool tool in tools) {
                if (tool.Definition.Repairs == kind)
                    return tool;
            }

            throw new InvalidOperationException($"Repair: no {kind} tool on the ship's rack ({tools.Length} tools in the scene).");
        }

        private static IEnumerator WaitForCoroutine(Func<bool> condition, string target, float timeoutSeconds) {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (condition() == false) {
                if (Time.realtimeSinceStartup > deadline)
                    Assert.Fail($"Repair: timed out after {timeoutSeconds} s waiting for {target}.");

                yield return null;
            }
        }
    }
}
#endif
