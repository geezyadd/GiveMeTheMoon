#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Collections.Generic;
using Features.CharacterMovableModule.Scripts;
using Game.Connection;
using Mirror;
using Tests.Coop.Shared;
using Tests.Shared;
using UnityEngine;

namespace Tests.Coop.Client {
    // The client walks on the flying deck with a bad link (scenario deck-walk-lag): on the host's command it sends with
    // the latency profile "150 ms, jitter 20, loss 5%", stands in the deck's middle, reports that it starts and walks a
    // few short legs there and back (stops between them) through the scripted move input, then reports how far it walked
    // on the deck and sends without lag again. The host measures its copy of the client meanwhile.
    public sealed class CoopDeckWalkLagClient : CoopScenarioClientBot {
        private const int LOOP = 1;
        // Long enough to land on the deck and for the lagged packet stream to settle on the host.
        private const float LAG_SETTLE_SECONDS = 2f;
        private const int ROUNDS = 2;
        private const float LEG_SECONDS = 0.8f;
        private const float PAUSE_SECONDS = 0.5f;

        // Short legs out and back, so the client stays near the middle of the deck whatever way its camera faces.
        private static readonly Vector2[] _legs = {
            Vector2.up, Vector2.down, Vector2.right, Vector2.left
        };

        private readonly WaitForSeconds _lagSettle = new WaitForSeconds(LAG_SETTLE_SECONDS);

        private Vector3 _lastShipLocal;
        private float _walkedMeters;

        private static NetworkIdentity Player => SessionQueries.LocalPlayer;

        protected override IEnumerator RunScenarioCoroutine() {
            yield return BoardCoroutine();
            yield return WaitForCruiseCoroutine(LOOP);
            yield return WaitForCommandCoroutine(CoopFlightSteps.COMMAND_WALK_WITH_LAG);
            if (TryFindLagProfile(out ConnectionLatencyProfile profile) == false)
                yield return FailCoroutine($"No latency profile '{CoopFlightSteps.DECK_WALK_LAG_PROFILE}' in the connection config.");

            ConnectionLatencySimulation simulation = SessionQueries.NetworkManager.LatencySimulation;
            simulation.Apply(profile);
            ShipDeckBoarding.PutAboveDeck(Player.GetComponent<CharacterMovableBase>(), Ship);
            yield return _lagSettle;
            Report(CoopFlightObserver.CaptureFlight(CoopFlightSteps.WALKING_WITH_LAG));

            yield return WalkLegsCoroutine();
            CoopObservation observation = CoopFlightObserver.CaptureFlight(CoopFlightSteps.WALKED_WITH_LAG);
            observation.WalkedMeters = _walkedMeters;
            observation.IsAboard = SessionQueries.IsAboard(Ship, Player);
            Report(observation);
            simulation.Apply(new ConnectionLatencyProfile());
        }

        private IEnumerator WalkLegsCoroutine() {
            CharacterInputBuffer input = SessionQueries.Resolve<CharacterInputBuffer>();
            _lastShipLocal = ShipLocalPlayer();
            _walkedMeters = 0f;
            input.BeginScripted();
            try {
                for (int round = 0; round < ROUNDS; round++) {
                    for (int leg = 0; leg < _legs.Length; leg++) {
                        yield return HoldCoroutine(input, _legs[leg], LEG_SECONDS);
                        yield return HoldCoroutine(input, Vector2.zero, PAUSE_SECONDS);
                    }
                }
            } finally {
                input.SetScripted(Vector2.zero, false, false);
                input.EndScripted();
            }
        }

        private IEnumerator HoldCoroutine(CharacterInputBuffer input, Vector2 move, float seconds) {
            input.SetScripted(move, false, false);
            for (float time = 0f; time < seconds; time += Time.deltaTime) {
                yield return null;
                MeasureStep();
            }
        }

        // Summed in the ship's axes on the deck plane: in the world the ship carries the player hundreds of metres.
        private void MeasureStep() {
            Vector3 now = ShipLocalPlayer();
            Vector3 step = now - _lastShipLocal;
            step.y = 0f;
            _walkedMeters += step.magnitude;
            _lastShipLocal = now;
        }

        private static Vector3 ShipLocalPlayer() =>
            Ship.transform.InverseTransformPoint(Player.transform.position);

        private static bool TryFindLagProfile(out ConnectionLatencyProfile profile) {
            IReadOnlyList<ConnectionLatencyProfile> profiles = SessionQueries.Resolve<ConnectionConfig>().LatencyProfiles;
            for (int i = 0; i < profiles.Count; i++) {
                if (profiles[i].Label != CoopFlightSteps.DECK_WALK_LAG_PROFILE)
                    continue;

                profile = profiles[i];
                return true;
            }

            profile = null;
            return false;
        }
    }
}
#endif
