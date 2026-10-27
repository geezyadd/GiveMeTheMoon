#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using UnityEngine;

namespace Tests.Coop.Shared {
    // The ship, flight and navigation fields of an observation (CoopFlightObserver), for the coop flight tests.
    public sealed partial class CoopObservation {
        // The ship's pose as this peer shows it, from the flight's destination: the same on both peers whatever their
        // world origin.
        public Vector3 ShipFromDestination;
        public float ShipYaw;
        public string ThrottleHolderKey;
        public float Hull;
        public string[] BrokenSockets;
        public string[] OpenBreaches;
        // The marks' positions in the order of SonarMarks, from the flight's destination.
        public Vector3[] SonarMarkFromDestination;
        public int SonarMarkerCount;
        public bool IsStationMarkerShown;
        public bool IsHazeEnabled;
        public float HazeClearMeters;
        public float HazeHiddenMeters;
        // The rock impacts this peer got since the host's last watch-impacts command, and how many of them shook its view.
        public string[] ImpactKinds;
        public int ShakenImpacts;
        public uint HeldItemId;
        public uint DroppedItemId;
        // The local player released from the ship (after a hull loss): riding, seated, body, hover, input, how far it walked.
        public bool IsRiding;
        public bool IsSeated;
        public string ReleaseProblems;
        public float WalkedMeters;
        public string Screenshot;

        public string DescribeFlight() =>
            $"{Step}: ship from destination {ShipFromDestination} yaw {ShipYaw:0.0}, throttle holder '{ThrottleHolderKey}', "
            + $"hull {Hull:0.0}, broken [{Join(BrokenSockets)}], breaches [{Join(OpenBreaches)}], "
            + $"sonar marks [{Join(SonarMarks)}] markers {SonarMarkerCount}, station marker {IsStationMarkerShown}, "
            + $"haze {IsHazeEnabled} {HazeClearMeters:0}-{HazeHiddenMeters:0} m, impacts [{Join(ImpactKinds)}] shaken {ShakenImpacts}, "
            + $"held #{HeldItemId} dropped #{DroppedItemId}, riding {IsRiding} seated {IsSeated}, release problems '{ReleaseProblems}', walked {WalkedMeters:0.00} m";

        private static string Join(string[] values) =>
            string.Join(LIST_SEPARATOR, values ?? Array.Empty<string>());
    }
}
#endif
