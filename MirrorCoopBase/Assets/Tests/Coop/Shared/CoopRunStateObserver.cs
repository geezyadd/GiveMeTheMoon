#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using Features.CharacterMovableModule.Scripts;
using Features.FloatingControllerModule;
using Features.GrabModule.Scripts;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Generated;
using Features.StagePresetModule.Data.Generated;
using Features.TurretModule.Scripts;
using Mirror;
using Tests.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Tests.Coop.Shared {
    // Reads CoopRunState in this process: the hull and its damage from the synced models while a ship is loaded, and
    // the local player's hand, seat and body wherever it is (menu, lobby, game).
    public static class CoopRunStateObserver {
        private const float GROUND_PROBE_UP = 0.5f;
        private const float GROUND_PROBE_DISTANCE = 4f;
        private const int GROUND_HIT_CAPACITY = 8;

        private static readonly RaycastHit[] _groundHits = new RaycastHit[GROUND_HIT_CAPACITY];

        public static CoopRunState Capture() {
            CoopRunState state = new CoopRunState {
                Scene = SceneManager.GetActiveScene().name,
                IsHosting = NetworkServer.active,
                PlayerCount = Object.FindObjectsByType<ShipRider>(FindObjectsSortMode.None).Length
            };
            ShipBase ship = Object.FindAnyObjectByType<ShipBase>();
            if (ship != null)
                CaptureShip(state);

            if (SessionQueries.LocalPlayer != null)
                CaptureLocalPlayer(SessionQueries.LocalPlayer, ship, state);

            return state;
        }

        private static void CaptureShip(CoopRunState state) {
            state.PresetId = SessionQueries.Resolve<IReadOnlyRunStagePresetModel>().PresetId;
            IReadOnlyShipStatsModel stats = SessionQueries.Resolve<IReadOnlyShipStatsModel>();
            state.Hull = stats.Hull;
            state.HullMax = stats.HullMax;
            IReadOnlyShipConditionModel condition = SessionQueries.Resolve<IReadOnlyShipConditionModel>();
            state.BrokenSockets = Sorted(condition.BrokenSockets);
            state.OpenBreaches = Sorted(condition.OpenBreaches);
        }

        private static void CaptureLocalPlayer(NetworkIdentity player, ShipBase ship, CoopRunState state) {
            state.HasLocalPlayer = true;
            state.PlayerPosition = player.transform.position;
            GrabController hand = player.GetComponent<GrabController>();
            state.HeldItemNetId = hand.Held != null ? hand.Held.netId : 0u;
            ShipRider rider = player.GetComponent<ShipRider>();
            state.IsRiding = rider.IsRiding;
            state.IsSeated = rider.IsSeated;
            state.IsTurretGunner = ship != null && CoopTurret.TryFindStation(ship, out ShipTurretStation turret) && turret.IsOccupiedByLocalPlayer;
            Rigidbody body = player.GetComponent<Rigidbody>();
            state.IsKinematic = body.isKinematic;
            state.DetectsCollisions = body.detectCollisions;
            state.IsHovering = player.GetComponent<FloatingController>().HoverEnabled;
            state.IsControlBlocked = SessionQueries.Resolve<CharacterInputBuffer>().IsBlocked;
            state.StandsOnGround = StandsOnGround(player.transform);
        }

        private static bool StandsOnGround(Transform player) {
            int count = Physics.RaycastNonAlloc(player.position + Vector3.up * GROUND_PROBE_UP, Vector3.down, _groundHits, GROUND_PROBE_DISTANCE, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++) {
                if (_groundHits[i].collider.transform.IsChildOf(player) == false)
                    return true;
            }

            return false;
        }

        private static string[] Sorted(IReadOnlyCollection<string> values) {
            List<string> sorted = new List<string>(values);
            sorted.Sort(StringComparer.Ordinal);
            return sorted.ToArray();
        }
    }
}
#endif
