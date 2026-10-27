#if UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using Tests.Coop.Shared;
using UnityEngine;

namespace Tests.Coop.Host {
    // What the connection tests compare beyond CoopObservationComparison: the hull, its broken modules and open
    // breaches, the stage preset, the crew's sonar marks and which loose items lie on the deck.
    public static class CoopRunStateComparison {
        // The hull travels as a float over the network.
        private const float HULL_TOLERANCE = 0.01f;

        public static List<string> FindMismatches(CoopObservation host, CoopObservation client) {
            List<string> mismatches = new List<string>();
            CoopRunState hostState = host.RunState;
            CoopRunState clientState = client.RunState;
            if (Mathf.Abs(hostState.Hull - clientState.Hull) > HULL_TOLERANCE || Mathf.Abs(hostState.HullMax - clientState.HullMax) > HULL_TOLERANCE)
                mismatches.Add($"hull: host {hostState.Hull}/{hostState.HullMax}, client {clientState.Hull}/{clientState.HullMax}");

            AddIfDifferent(mismatches, "stage preset", hostState.PresetId ?? string.Empty, clientState.PresetId ?? string.Empty);
            AddIfDifferent(mismatches, "broken modules", hostState.BrokenSockets, clientState.BrokenSockets);
            AddIfDifferent(mismatches, "open breaches", hostState.OpenBreaches, clientState.OpenBreaches);
            AddIfDifferent(mismatches, "sonar marks", host.SonarMarks, client.SonarMarks);
            AddIfDifferent(mismatches, "items on the deck", Describe(host.DeckItemIds), Describe(client.DeckItemIds));
            return mismatches;
        }

        private static void AddIfDifferent(List<string> mismatches, string what, string[] host, string[] client) =>
            AddIfDifferent(mismatches, what, string.Join(CoopObservation.LIST_SEPARATOR, host ?? Array.Empty<string>()), string.Join(CoopObservation.LIST_SEPARATOR, client ?? Array.Empty<string>()));

        private static void AddIfDifferent(List<string> mismatches, string what, string host, string client) {
            if (host != client)
                mismatches.Add($"{what}: host '{host}', client '{client}'");
        }

        private static string Describe(uint[] ids) =>
            ids == null ? string.Empty : string.Join(CoopObservation.LIST_SEPARATOR, ids);
    }
}
#endif
