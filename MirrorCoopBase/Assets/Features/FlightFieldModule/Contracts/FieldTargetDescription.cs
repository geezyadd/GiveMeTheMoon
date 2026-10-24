using UnityEngine;

namespace Features.FlightFieldModule.Contracts {
    // A target balloon as the field data describes it, wherever it is built: its id, its kind (the definition id), its
    // middle in the world now, the radius of its sphere and the money it gives when it pops.
    public readonly struct FieldTargetDescription {
        public FieldTargetId Id { get; }
        public string KindId { get; }
        public Vector3 Center { get; }
        public float Radius { get; }
        public int Reward { get; }

        public FieldTargetDescription(FieldTargetId id, string kindId, Vector3 center, float radius, int reward) {
            Id = id;
            KindId = kindId;
            Center = center;
            Radius = radius;
            Reward = reward;
        }
    }
}
