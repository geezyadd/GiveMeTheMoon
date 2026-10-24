using Features.NetworkModelModule.Scripts;
using UnityEngine;

namespace Features.FlightFieldModule.Contracts {
    // One rock broken on the server: what it was, where, why and by whom (an empty key when nobody did it), and in
    // which field of which loop.
    public readonly struct FieldRockDestroyed {
        public FieldRockDescription Rock { get; }
        public FieldRockDestroyCause Cause { get; }
        public PlayerKey Instigator { get; }
        public int FieldNumber { get; }
        public int LoopIndex { get; }
        public FieldRockId Id => Rock.Id;
        public string SourceId => Rock.SourceId;
        public FieldRockSize Size => Rock.Size;
        public float Diameter => Rock.Diameter;
        public Vector3 Position => Rock.Center;

        public FieldRockDestroyed(FieldRockDescription rock, FieldRockDestroyCause cause, PlayerKey instigator, int fieldNumber, int loopIndex) {
            Rock = rock;
            Cause = cause;
            Instigator = instigator;
            FieldNumber = fieldNumber;
            LoopIndex = loopIndex;
        }
    }
}
