#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using Features.GrabModule.Scripts;
using Features.ShipDamageModule.Scripts.Damage;
using Features.ShipDamageModule.Scripts.Repair;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Contracts;
using Features.ShipModule.Scripts.Generated;
using Tests.Coop.Shared;
using Tests.Shared;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.Coop.Client {
    // The client bot's work on the ship with its hands: picks a bought module up from the pad and installs it in a
    // socket by aim and interact, repairs a broken module with a wrench and patches a breach with the hammer by holding
    // interact on them, and reports the hold while it runs.
    public sealed class CoopClientShipWork {
        // The trigger over a broken module the aim finds it by (ShipPlatform prefab).
        private const string REPAIR_VOLUME = "RepairVolume";

        private readonly CoopTestClient _client;
        private readonly CoopClientHands _hands;

        private static ShipBase Ship => Object.FindAnyObjectByType<ShipBase>();
        private static UseController Use => SessionQueries.LocalPlayer.GetComponent<UseController>();
        private static IReadOnlyShipConditionModel Condition => SessionQueries.Resolve<IReadOnlyShipConditionModel>();

        public CoopClientShipWork(CoopTestClient client, CoopClientHands hands) {
            _client = client;
            _hands = hands;
        }

        // From behind the gun the seat's volume covers the turret socket plate (TurretModule's known limit): the turret
        // socket is used through its use command, what E sends; every other socket by the real aim.
        public IEnumerator InstallCoroutine(string moduleId, string socketId) {
            ShipItem item = FindLooseModuleItem(moduleId);
            if (item == null)
                yield return _client.FailCoroutine($"No bought {moduleId} lies loose to install.");

            yield return _hands.PickUpCoroutine(item.GetComponent<Grabbable>(), $"the bought {moduleId}");
            ShipSocket socket = FindSocket(socketId);
            ShipInstallInteractable install = socket.GetComponent<ShipInstallInteractable>();
            Vector3 plate = CoopClientHands.FindAimPoint(install);
            yield return _hands.StandOnDeckNearCoroutine(Ship, plate);
            if (socketId == CoopWeaponsEconomySteps.TURRET_SOCKET)
                Use.RequestUse(install);
            else
                yield return _hands.UseAimedCoroutine(install, plate, $"the empty socket {socketId}");

            yield return _client.WaitForCoroutine(() => socket.InstalledModuleId == moduleId && CoopClientHands.Hand.IsHolding == false, $"{moduleId} installed in {socketId} from the hand");
            Report(CoopWeaponsEconomySteps.INSTALLED, socketId);
        }

        public IEnumerator SeeDamageCoroutine(string socketId, string breachId) {
            yield return _client.WaitForCoroutine(() => IsBroken(socketId) && IsOpen(breachId), $"{socketId} broken and {breachId} open in the client's condition model");
            Report(CoopWeaponsEconomySteps.DAMAGE_SEEN, string.Empty);
        }

        // With the bought wrench from the pad: a tool lying off the deck.
        public IEnumerator RepairModuleCoroutine(string socketId) {
            RepairTool wrench = FindLooseTool(ShipRepairKind.Module, false);
            if (wrench == null)
                yield return _client.FailCoroutine("No bought wrench lies on the pad.");

            yield return _hands.PickUpCoroutine(wrench.GetComponent<Grabbable>(), "the bought wrench");
            Vector3 volume = FindSocket(socketId).transform.Find(REPAIR_VOLUME).position;
            yield return _hands.StandOnDeckNearCoroutine(Ship, volume);
            yield return HoldOnCoroutine<ShipModuleRepairInteractable>(volume, () => IsBroken(socketId) == false, $"the broken {socketId}");
            Report(CoopWeaponsEconomySteps.REPAIRED, socketId);
        }

        // With the hammer from the deck's tool rack.
        public IEnumerator PatchBreachCoroutine(string breachId) {
            RepairTool hammer = FindLooseTool(ShipRepairKind.Breach, true);
            if (hammer == null)
                yield return _client.FailCoroutine("No hammer lies on the deck's rack.");

            yield return _hands.PickUpCoroutine(hammer.GetComponent<Grabbable>(), "the rack's hammer");
            Vector3 point = FindDamagePoint(breachId).GetComponent<Collider>().bounds.center;
            yield return _hands.StandOnDeckNearCoroutine(Ship, point);
            yield return HoldOnCoroutine<ShipBreachRepairInteractable>(point, () => IsOpen(breachId) == false, $"the open {breachId}");
            Report(CoopWeaponsEconomySteps.REPAIRED, breachId);
        }

        // Holds interact on the aimed repair place: reports the running hold, lets go once repaired, drops the tool.
        private IEnumerator HoldOnCoroutine<TRepair>(Vector3 point, Func<bool> isRepaired, string what) where TRepair : InteractableBase {
            yield return _hands.AimAtCoroutine(point, what);
            if (CoopClientHands.Interaction.Target.Usable is not TRepair)
                yield return _client.FailCoroutine($"{what} is not offered for repair under the crosshair (usable {CoopClientHands.Interaction.Target.Usable}, looked at {CoopClientHands.Interaction.Target.LookedAt}, aimed at {point}, {CoopClientHands.DescribeView()}).");

            CoopClientHands.Interaction.Interact();
            yield return _client.WaitForCoroutine(() => Use.HoldProgress > 0f, $"the hold on {what} to start");
            Report(CoopWeaponsEconomySteps.REPAIR_HOLDING, what);
            yield return _client.WaitForCoroutine(isRepaired, $"{what} repaired by holding interact");
            CoopClientHands.Interaction.EndInteract();
            yield return _client.WaitForCoroutine(() => Use.HoldProgress <= 0f, "the hold bar cleared after the repair");
            yield return _hands.DropCoroutine();
        }

        private void Report(string step, string message) {
            CoopObservation observation = CoopWeaponsEconomyObserver.Capture(step);
            observation.Message = message;
            _client.Report(observation);
        }

        private static bool IsBroken(string socketId) {
            foreach (string broken in Condition.BrokenSockets) {
                if (broken == socketId)
                    return true;
            }

            return false;
        }

        private static bool IsOpen(string breachId) {
            foreach (string open in Condition.OpenBreaches) {
                if (open == breachId)
                    return true;
            }

            return false;
        }

        // The bought one: GameScene also places loose modules at the start station, away from the kiosk.
        private static ShipItem FindLooseModuleItem(string moduleId) {
            Vector3 delivery = CoopClientShop.FindKiosk().DeliveryPoint.position;
            ShipItem nearest = null;
            foreach (ShipItem item in Object.FindObjectsByType<ShipItem>(FindObjectsSortMode.None)) {
                bool isCandidate = item.Module.Id == moduleId && CoopWeaponsEconomyObserver.IsLooseModuleItem(item);
                if (isCandidate && (nearest == null || IsCloser(item.transform, nearest.transform, delivery)))
                    nearest = item;
            }

            return nearest;
        }

        // The bought tool lies at the kiosk's delivery point, the rack's one on the deck.
        private static RepairTool FindLooseTool(ShipRepairKind kind, bool isOnDeck) {
            Vector3 delivery = CoopClientShop.FindKiosk().DeliveryPoint.position;
            RepairTool nearest = null;
            foreach (RepairTool tool in Object.FindObjectsByType<RepairTool>(FindObjectsSortMode.None)) {
                bool isCandidate = tool.Definition.Repairs == kind && tool.GetComponent<Grabbable>().CanBeGrabbed
                                   && SessionQueries.IsOnDeck(Ship, tool.transform.position) == isOnDeck;
                if (isCandidate && (nearest == null || IsCloser(tool.transform, nearest.transform, delivery)))
                    nearest = tool;
            }

            return nearest;
        }

        private static bool IsCloser(Transform candidate, Transform current, Vector3 point) =>
            (candidate.position - point).sqrMagnitude < (current.position - point).sqrMagnitude;

        private static ShipSocket FindSocket(string socketId) {
            foreach (ShipSocket socket in Ship.Sockets) {
                if (socket.SocketId == socketId)
                    return socket;
            }

            throw new InvalidOperationException($"The ship has no socket {socketId}.");
        }

        private static ShipHullDamagePoint FindDamagePoint(string pointId) {
            foreach (ShipHullDamagePoint point in SessionQueries.Resolve<IReadOnlyComponentRegistry<ShipHullDamagePoint>>().Items) {
                if (point.PointId == pointId)
                    return point;
            }

            throw new InvalidOperationException($"The ship has no hull damage point {pointId}.");
        }
    }
}
#endif
