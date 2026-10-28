#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Ore;
using Features.GrabModule.Scripts;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Generated;
using Features.SonarModule.Data;
using Features.SonarModule.Data.Generated;
using Features.SpaceHazeModule.Data;
using Features.SpaceHazeModule.Scripts.Core;
using Tests.Shared;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.Coop.Shared {
    // What this process sees of the ship and the navigation on top of CoopObserver's fields: the ship's pose from the
    // destination, the throttle's holder, the hull and its damage, the sonar marks' positions and the HUD markers, the
    // own station marker and the haze. The host and the client capture the same fields.
    public static class CoopFlightObserver {
        public static CoopObservation CaptureFlight(string step) {
            CoopObservation observation = CoopObserver.CaptureGame(step);
            ShipBase ship = Object.FindAnyObjectByType<ShipBase>();
            Vector3 destination = SessionQueries.Resolve<IReadOnlyShipRunModel>().TransitDestination;
            observation.ShipFromDestination = ship.transform.position - destination;
            observation.ShipYaw = ship.transform.eulerAngles.y;
            observation.ThrottleHolderKey = SessionQueries.Resolve<IReadOnlyShipThrottleModel>().HolderKey ?? string.Empty;
            CaptureDamage(observation);
            CaptureSonar(observation, destination);
            CaptureHaze(observation);
            CaptureHeldItem(observation);
            CaptureOre(observation, destination);
            return observation;
        }

        private static void CaptureOre(CoopObservation observation, Vector3 destination) {
            List<FieldOreDescription> ores = new List<FieldOreDescription>();
            SessionQueries.Resolve<IFieldOreQuery>().FindAll(ores);
            ores.Sort(CompareById);
            FieldOreSceneModel scene = SessionQueries.Resolve<FieldOreSceneModel>();
            GrabController hand = SessionQueries.LocalPlayer.GetComponent<GrabController>();
            observation.OreIds = new int[ores.Count];
            observation.OreFromDestination = new Vector3[ores.Count];
            observation.OreShown = new bool[ores.Count];
            for (int i = 0; i < ores.Count; i++) {
                observation.OreIds[i] = ores[i].Id.Value;
                observation.OreFromDestination[i] = ores[i].Center - destination;
                bool isShown = scene.TryGetView(ores[i].Id.Value, out FieldOreView view) && view.gameObject.activeInHierarchy;
                observation.OreShown[i] = isShown;
                if (!ores[i].Hooker.Equals(hand.Key))
                    continue;

                observation.OwnHookedOreId = ores[i].Id.Value;
                observation.IsOwnRopeShown = isShown && view.GetComponentInChildren<LineRenderer>(true).enabled;
            }
        }

        private static int CompareById(FieldOreDescription first, FieldOreDescription second) =>
            first.Id.Value.CompareTo(second.Id.Value);

        private static void CaptureDamage(CoopObservation observation) {
            observation.Hull = SessionQueries.Resolve<IReadOnlyShipStatsModel>().Hull;
            IReadOnlyShipConditionModel condition = SessionQueries.Resolve<IReadOnlyShipConditionModel>();
            observation.BrokenSockets = Sorted(condition.BrokenSockets);
            observation.OpenBreaches = Sorted(condition.OpenBreaches);
        }

        // In the order of CoopObservation.SonarMarks: sorted by contact id.
        private static void CaptureSonar(CoopObservation observation, Vector3 destination) {
            List<SonarMark> marks = new List<SonarMark>(SessionQueries.Resolve<IReadOnlySonarConsoleModel>().Marks);
            marks.Sort(CompareByContact);
            observation.SonarMarkFromDestination = new Vector3[marks.Count];
            for (int i = 0; i < marks.Count; i++)
                observation.SonarMarkFromDestination[i] = marks[i].Position - destination;

            observation.SonarMarkerCount = SessionQueries.Resolve<SonarMarkerScreenModel>().Markers.Count;
            observation.IsStationMarkerShown = SessionQueries.Resolve<SonarStationMarkerModel>().IsShown;
        }

        private static void CaptureHaze(CoopObservation observation) {
            SpaceHazeModel haze = SessionQueries.Resolve<SpaceHazeModel>();
            SpaceHazeShape shape = SessionQueries.Resolve<ISpaceHazeRule>().CreateShape(haze);
            observation.IsHazeEnabled = haze.IsEnabled;
            observation.HazeClearMeters = shape.ClearMeters;
            observation.HazeHiddenMeters = shape.HiddenMeters;
        }

        private static void CaptureHeldItem(CoopObservation observation) {
            GrabController hand = SessionQueries.LocalPlayer.GetComponent<GrabController>();
            observation.HeldItemId = hand.Held != null ? hand.Held.netId : 0u;
        }

        private static int CompareByContact(SonarMark first, SonarMark second) =>
            string.CompareOrdinal(first.ContactId, second.ContactId);

        private static string[] Sorted(IReadOnlyCollection<string> values) {
            string[] sorted = new string[values.Count];
            int index = 0;
            foreach (string value in values)
                sorted[index++] = value;

            Array.Sort(sorted, StringComparer.Ordinal);
            return sorted;
        }
    }
}
#endif
