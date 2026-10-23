#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using UnityEngine;

namespace Tests.Coop.Shared {
    // What one side sees at a step of the scenario: a line of the client's report, or the host's own view to compare it
    // with. Public fields for JsonUtility.
    [Serializable]
    public sealed class CoopObservation {
        public const string LIST_SEPARATOR = ", ";

        public string Step;
        public float RealTime;
        public string Phase;
        public int LoopIndex;
        public string[] Sockets;
        public float Thrust;
        public float DodgeRange;
        public float Handling;
        public float Armor;
        public float FlightSpeed;
        public long Balance;
        public int FieldNumber;
        public int FieldSeed;
        public int FieldPlacements;
        public string OtherNameplate;
        public bool IsOnDeck;
        public string LifeState;
        public bool IsSpectating;
        public Vector3 Position;
        public string RejectReason;
        public bool IsRejectTextShown;
        public string Message;
        public string[] Errors;

        public string Describe() =>
            $"{Step} at {RealTime:0.0} s: phase {Phase}, loop {LoopIndex}, {LifeState}, spectating {IsSpectating}, on deck {IsOnDeck} at {Position}, "
            + $"field #{FieldNumber} seed {FieldSeed} placements {FieldPlacements}, nameplate '{OtherNameplate}', "
            + $"sockets [{string.Join(LIST_SEPARATOR, Sockets ?? Array.Empty<string>())}], "
            + $"stats thrust {Thrust} dodge {DodgeRange} handling {Handling} armor {Armor} speed {FlightSpeed}, balance {Balance}"
            + (string.IsNullOrEmpty(Message) ? string.Empty : $", message: {Message}");
    }
}
#endif
