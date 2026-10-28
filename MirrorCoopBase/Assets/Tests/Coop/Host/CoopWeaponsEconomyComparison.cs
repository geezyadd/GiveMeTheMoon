#if UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using Tests.Coop.Shared;
using UnityEngine;

namespace Tests.Coop.Host {
    // What the economy and weapons scenarios compare on top of CoopObservationComparison: the crew balance, the loose
    // module items, the ship's broken sockets, open breaches and hull, and the field's popped balloons and broken rocks.
    // Every one of them is synced state, so host and client must agree exactly once the client reports.
    public static class CoopWeaponsEconomyComparison {
        // The hull travels as a float; a tenth of a point is far below any hit or repair.
        private const float HULL_TOLERANCE = 0.1f;

        public static List<string> FindMismatches(CoopObservation host, CoopObservation client) {
            List<string> mismatches = new List<string>();
            AddIfDifferent(mismatches, "crew balance", host.Balance.ToString(), client.Balance.ToString());
            AddIfDifferent(mismatches, "loose module items", host.LooseModuleItems.ToString(), client.LooseModuleItems.ToString());
            AddIfDifferent(mismatches, "broken sockets", Join(host.BrokenSockets), Join(client.BrokenSockets));
            AddIfDifferent(mismatches, "open breaches", Join(host.OpenBreaches), Join(client.OpenBreaches));
            AddIfDifferent(mismatches, "popped balloons", Join(host.PoppedTargets), Join(client.PoppedTargets));
            AddIfDifferent(mismatches, "broken rocks", Join(host.BrokenRocks), Join(client.BrokenRocks));
            if (Mathf.Abs(host.Hull - client.Hull) > HULL_TOLERANCE)
                mismatches.Add($"hull: host {host.Hull:0.0}, client {client.Hull:0.0}");

            return mismatches;
        }

        private static void AddIfDifferent(List<string> mismatches, string what, string host, string client) {
            if (host != client)
                mismatches.Add($"{what}: host '{host}', client '{client}'");
        }

        private static string Join<T>(T[] values) =>
            string.Join(CoopObservation.LIST_SEPARATOR, values ?? Array.Empty<T>());
    }
}
#endif
