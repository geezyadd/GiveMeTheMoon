using Features.NetworkModelModule.Scripts;
using UnityEngine;

namespace Features.FlightFieldModule.Contracts {
    // One piece of ore that reached the ship on the server: what it was, where, who pulled it in (an empty key when
    // the ship flew into it) and in which field of which loop.
    public readonly struct FieldOreCollected {
        public FieldOreDescription Ore { get; }
        public int FieldNumber { get; }
        public int LoopIndex { get; }
        public FieldOreId Id => Ore.Id;
        public string KindId => Ore.KindId;
        public int Price => Ore.Price;
        public PlayerKey Hooker => Ore.Hooker;
        public Vector3 Position => Ore.Center;

        public FieldOreCollected(FieldOreDescription ore, int fieldNumber, int loopIndex) {
            Ore = ore;
            FieldNumber = fieldNumber;
            LoopIndex = loopIndex;
        }
    }
}
