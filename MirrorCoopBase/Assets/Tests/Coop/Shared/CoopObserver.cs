#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Network.Generated;
using Features.GrabModule.Scripts;
using Features.PlayerLifeModule.Scripts;
using Features.PlayerLifeModule.Scripts.Spectator;
using Features.PlayerProfileModule.Scripts;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Generated;
using Features.ShopModule.Scripts.Generated;
using Features.SonarModule.Data;
using Features.SonarModule.Data.Generated;
using Mirror;
using Tests.Shared;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.Coop.Shared {
    // Reads what this process sees of the shared game: the run, the ship's sockets and stats, the crew balance, the
    // flight field, the other player's nameplate, where the local player stands, whether it is alive and at the helm,
    // the throttle and how the items on the deck lie.
    // The host and the client capture the same fields.
    public static class CoopObserver {
        private const string SOCKET_FORMAT = "{0}={1}";

        public static CoopObservation CaptureStep(string step) =>
            new CoopObservation { Step = step, RealTime = Time.realtimeSinceStartup };

        public static CoopObservation CaptureGame(string step) {
            CoopObservation observation = CaptureStep(step);
            IReadOnlyShipRunModel run = SessionQueries.Resolve<IReadOnlyShipRunModel>();
            observation.Phase = run.Phase.ToString();
            observation.LoopIndex = run.LoopIndex;
            observation.Sockets = CaptureSockets(SessionQueries.Resolve<IReadOnlyShipSocketsModel>());
            CaptureStats(SessionQueries.Resolve<IReadOnlyShipStatsModel>(), observation);
            CaptureField(SessionQueries.Resolve<IReadOnlyLiveFieldModel>(), observation);
            observation.Balance = SessionQueries.Resolve<IReadOnlyWalletModel>().Balance;
            observation.OtherNameplate = CaptureOtherNameplate();
            CaptureLocalPlayer(observation);
            observation.LifeState = SessionQueries.Resolve<IPlayerLifeQuery>().LocalState.ToString();
            observation.IsSpectating = SessionQueries.Resolve<SpectatorModel>().IsSpectating;
            observation.SonarMarks = CaptureSonarMarks(SessionQueries.Resolve<IReadOnlySonarConsoleModel>());
            observation.ThrottlePosition = SessionQueries.Resolve<IReadOnlyShipThrottleModel>().Position;
            CaptureDeckItems(Object.FindAnyObjectByType<ShipBase>(), observation);
            return observation;
        }

        // Sorted by net id, so the host and the client list the same items in the same order.
        private static void CaptureDeckItems(ShipBase ship, CoopObservation observation) {
            List<uint> ids = new List<uint>();
            List<float> gaps = new List<float>();
            foreach (Grabbable item in Object.FindObjectsByType<Grabbable>(FindObjectsSortMode.None)) {
                if (TryMeasureDeckItem(ship, item, out uint netId, out float gap) == false)
                    continue;

                ids.Add(netId);
                gaps.Add(gap);
            }

            observation.DeckItemIds = ids.ToArray();
            observation.DeckItemGaps = gaps.ToArray();
            Array.Sort(observation.DeckItemIds, observation.DeckItemGaps);
        }

        private static bool TryMeasureDeckItem(ShipBase ship, Grabbable item, out uint netId, out float gap) {
            netId = 0u;
            gap = 0f;
            if (item.CanBeGrabbed == false)
                return false;

            BoxCollider box = item.GetComponentInChildren<BoxCollider>();
            if (box == null || SessionQueries.IsOnDeck(ship, item.transform.position) == false)
                return false;

            netId = item.netId;
            gap = ShipDeckItemRest.MeasureGapAboveDeck(ship, box);
            return true;
        }

        // The contacts the crew has marked, as this process's synced sonar model holds them.
        private static string[] CaptureSonarMarks(IReadOnlySonarConsoleModel sonar) {
            List<string> marks = new List<string>();
            foreach (SonarMark mark in sonar.Marks)
                marks.Add(mark.ContactId);

            marks.Sort(StringComparer.Ordinal);
            return marks.ToArray();
        }

        private static string[] CaptureSockets(IReadOnlyShipSocketsModel sockets) {
            List<string> installed = new List<string>();
            foreach (KeyValuePair<string, ShipSocketState> socket in sockets.States) {
                if (socket.Value.Occupied)
                    installed.Add(string.Format(SOCKET_FORMAT, socket.Key, socket.Value.ModuleId));
            }

            installed.Sort(StringComparer.Ordinal);
            return installed.ToArray();
        }

        private static void CaptureStats(IReadOnlyShipStatsModel stats, CoopObservation observation) {
            observation.DodgeRange = stats.DodgeRange;
            observation.Armor = stats.Armor;
            observation.FlightSpeed = stats.FlightSpeed;
        }

        private static void CaptureField(IReadOnlyLiveFieldModel field, CoopObservation observation) {
            observation.FieldNumber = field.Number;
            observation.FieldSeed = field.Seed;
            observation.FieldPlacements = field.Placements.Count;
        }

        private static string CaptureOtherNameplate() {
            List<string> names = new List<string>();
            foreach (PlayerNameplate nameplate in Object.FindObjectsByType<PlayerNameplate>(FindObjectsSortMode.None)) {
                NetworkIdentity owner = nameplate.GetComponentInParent<NetworkIdentity>();
                if (owner != null && owner != SessionQueries.LocalPlayer)
                    names.Add(nameplate.GetComponentInChildren<TextMeshPro>(true).text);
            }

            return string.Join(CoopObservation.LIST_SEPARATOR, names);
        }

        private static void CaptureLocalPlayer(CoopObservation observation) {
            ShipBase ship = Object.FindAnyObjectByType<ShipBase>();
            Vector3 position = SessionQueries.LocalPlayer.transform.position;
            observation.Position = position;
            observation.IsOnDeck = SessionQueries.IsOnDeck(ship, position);
            observation.IsHelmPilot = CoopHelm.FindStation(ship).IsSteeredByLocalPlayer;
        }
    }
}
#endif
