#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using Tests.Coop.Shared;
using UnityEngine;

namespace Tests.Coop.Host {
    // What must match between the host's view and the client's report at the same step: the run, the ship, the crew
    // balance, the field in flight, each player's name over the other one and the client standing on the deck.
    public static class CoopObservationComparison {
        // Stats travel as floats over the network; a thousandth is far below any modifier step.
        private const float STAT_PRECISION = 1000f;

        // A client that leaves right after its report is gone from the host's view, so its nameplate is not compared then.
        public static List<string> FindMismatches(CoopObservation host, CoopObservation client, bool isInFlight, bool isClientStaying) {
            List<string> mismatches = new List<string>();
            AddIfDifferent(mismatches, "phase", host.Phase, client.Phase);
            AddIfDifferent(mismatches, "loop index", host.LoopIndex.ToString(), client.LoopIndex.ToString());
            AddIfDifferent(mismatches, "sockets", string.Join(CoopObservation.LIST_SEPARATOR, host.Sockets), string.Join(CoopObservation.LIST_SEPARATOR, client.Sockets));
            AddIfDifferent(mismatches, "stats", DescribeStats(host), DescribeStats(client));
            AddIfDifferent(mismatches, "crew balance", host.Balance.ToString(), client.Balance.ToString());
            AddIfDifferent(mismatches, "client's nameplate over the host", CoopTestPlayers.HOST_NAME, client.OtherNameplate);
            if (isClientStaying)
                AddIfDifferent(mismatches, "host's nameplate over the client", CoopTestPlayers.CLIENT_NAME, host.OtherNameplate);

            if (client.IsOnDeck == false)
                mismatches.Add($"the client sees itself off the deck at {client.Position}");

            if (isInFlight)
                AddIfDifferent(mismatches, "flight field", DescribeField(host), DescribeField(client));

            return mismatches;
        }

        private static void AddIfDifferent(List<string> mismatches, string what, string host, string client) {
            if (host != client)
                mismatches.Add($"{what}: host '{host}', client '{client}'");
        }

        private static string DescribeStats(CoopObservation observation) =>
            $"thrust {Round(observation.Thrust)} dodge {Round(observation.DodgeRange)} handling {Round(observation.Handling)} "
            + $"armor {Round(observation.Armor)} speed {Round(observation.FlightSpeed)}";

        private static string DescribeField(CoopObservation observation) =>
            $"#{observation.FieldNumber} seed {observation.FieldSeed} placements {observation.FieldPlacements}";

        private static string Round(float value) =>
            Mathf.Round(value * STAT_PRECISION).ToString();
    }
}
#endif
