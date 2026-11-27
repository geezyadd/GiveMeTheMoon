using UnityEngine;

namespace Features.ExplosionModule {
    // What counts as ground for an explosion's look (ExplosionGroundProbe), the same for every preset.
    public readonly struct ExplosionGroundSettings {
        public ExplosionGroundSettings(LayerMask layers, float probeDistance, float maxSlopeDegrees, float flatTolerance) {
            Layers = layers;
            ProbeDistance = probeDistance;
            MaxSlopeDegrees = maxSlopeDegrees;
            FlatTolerance = flatTolerance;
        }

        public LayerMask Layers { get; }
        // The surface must be at most this far under the centre, m: farther, the explosion went off in the air.
        public float ProbeDistance { get; }
        // The surface right under the centre may lean at most this much, degrees.
        public float MaxSlopeDegrees { get; }
        // The surface around the centre may be at most this much higher or lower than under it, m.
        public float FlatTolerance { get; }
    }
}
