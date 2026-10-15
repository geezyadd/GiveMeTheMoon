#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Network.Generated;
using Features.PlayerLifeModule.Scripts;
using Features.PlayerLifeModule.Scripts.Spectator;
using Features.PlayerProfileModule.Scripts;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Generated;
using Features.ShopModule.Scripts.Generated;
using Mirror;
using Tests.Shared;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.Coop.Shared {
    // Reads what this process sees of the shared game: the run, the ship's sockets and stats, the crew balance, the
    // flight field, the other player's nameplate, where the local player stands and whether it is alive.
    // The host and the client capture the same fields.
    public static class CoopObserver {
        private const float DECK_EDGE_TOLERANCE = 0.5f;
        private const float BELOW_DECK_TOLERANCE = 1f;
        private const string SOCKET_FORMAT = "{0}={1}";
        private const string NAME_SEPARATOR = ", ";

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
            return observation;
        }

        public static bool IsOnDeck(ShipBase ship, Vector3 position) {
            Bounds deck = ship.DeckGeometry.Bounds;
            return SessionQueries.ContainsXZ(deck, position, DECK_EDGE_TOLERANCE) && position.y > deck.min.y - BELOW_DECK_TOLERANCE;
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
            observation.Thrust = stats.Thrust;
            observation.DodgeRange = stats.DodgeRange;
            observation.Handling = stats.Handling;
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

            return string.Join(NAME_SEPARATOR, names);
        }

        private static void CaptureLocalPlayer(CoopObservation observation) {
            ShipBase ship = Object.FindAnyObjectByType<ShipBase>();
            Vector3 position = SessionQueries.LocalPlayer.transform.position;
            observation.Position = position;
            observation.IsOnDeck = IsOnDeck(ship, position);
        }
    }
}
#endif
