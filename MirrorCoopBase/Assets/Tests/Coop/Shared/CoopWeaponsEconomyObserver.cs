#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Network.Generated;
using Features.GrabModule.Scripts;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Generated;
using Tests.Shared;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.Coop.Shared {
    // CoopObserver's view plus what the economy and weapons scenarios compare: the module items lying loose, the ship's
    // condition and hull, the field's popped balloons and broken rocks, the local hand and its hold progress. The host
    // and the client capture the same fields.
    public static class CoopWeaponsEconomyObserver {
        private const string EMPTY_HAND = "";

        public static CoopObservation Capture(string step) {
            CoopObservation observation = CoopObserver.CaptureGame(step);
            observation.LooseModuleItems = CountLooseModuleItems();
            IReadOnlyShipConditionModel condition = SessionQueries.Resolve<IReadOnlyShipConditionModel>();
            observation.BrokenSockets = Sorted(condition.BrokenSockets);
            observation.OpenBreaches = Sorted(condition.OpenBreaches);
            observation.Hull = SessionQueries.Resolve<IReadOnlyShipStatsModel>().Hull;
            IReadOnlyLiveFieldModel field = SessionQueries.Resolve<IReadOnlyLiveFieldModel>();
            observation.PoppedTargets = CapturePoppedTargets(field.PoppedTargets);
            observation.BrokenRocks = CaptureBrokenRocks(field.BrokenRocks);
            CaptureHand(observation);
            return observation;
        }

        public static bool IsLooseModuleItem(ShipItem item) =>
            item.TryGetComponent(out Grabbable grabbable) && grabbable.CanBeGrabbed;

        private static int CountLooseModuleItems() {
            int count = 0;
            foreach (ShipItem item in Object.FindObjectsByType<ShipItem>(FindObjectsSortMode.None)) {
                if (IsLooseModuleItem(item))
                    count++;
            }

            return count;
        }

        private static void CaptureHand(CoopObservation observation) {
            Grabbable held = SessionQueries.LocalPlayer.GetComponent<GrabController>().Held;
            observation.HeldItem = held != null ? held.name : EMPTY_HAND;
            observation.HoldProgress = SessionQueries.LocalPlayer.GetComponent<UseController>().HoldProgress;
        }

        private static int[] CapturePoppedTargets(IReadOnlyList<FieldTargetId> popped) {
            int[] indices = new int[popped.Count];
            for (int i = 0; i < indices.Length; i++)
                indices[i] = popped[i].Index;

            Array.Sort(indices);
            return indices;
        }

        private static string[] CaptureBrokenRocks(IReadOnlyList<FieldRockId> broken) {
            string[] ids = new string[broken.Count];
            for (int i = 0; i < ids.Length; i++)
                ids[i] = FieldRockIdText.Format(broken[i]);

            Array.Sort(ids, StringComparer.Ordinal);
            return ids;
        }

        private static string[] Sorted(IReadOnlyCollection<string> values) {
            List<string> sorted = new List<string>(values);
            sorted.Sort(StringComparer.Ordinal);
            return sorted.ToArray();
        }
    }
}
#endif
