using Features.NetworkModelModule.Scripts;
using UnityEngine;

namespace Features.FlightFieldModule.Contracts {
    // A piece of ore as the live field describes it now: its id, its kind (the definition id), its middle in the world,
    // the radius it is caught at, the money it pays when it reaches the ship and who pulls it on a rope (an empty key
    // while it drifts).
    public readonly struct FieldOreDescription {
        public FieldOreId Id { get; }
        public string KindId { get; }
        public Vector3 Center { get; }
        public float Radius { get; }
        public int Price { get; }
        public PlayerKey Hooker { get; }
        public bool IsHooked => !Hooker.IsEmpty;

        public FieldOreDescription(FieldOreId id, string kindId, Vector3 center, float radius, int price, PlayerKey hooker) {
            Id = id;
            KindId = kindId;
            Center = center;
            Radius = radius;
            Price = price;
            Hooker = hooker;
        }
    }
}
