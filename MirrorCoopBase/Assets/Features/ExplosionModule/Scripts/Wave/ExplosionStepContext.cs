using UnityEngine;

namespace Features.ExplosionModule.Wave {
    // What every wave's step shares on this peer this physics step.
    internal readonly struct ExplosionStepContext {
        public readonly bool ThrowsLooseBodies;
        public readonly LayerMask AffectedLayers;
        public readonly Collider[] OverlapBuffer;

        public ExplosionStepContext(bool throwsLooseBodies, LayerMask affectedLayers, Collider[] overlapBuffer) {
            ThrowsLooseBodies = throwsLooseBodies;
            AffectedLayers = affectedLayers;
            OverlapBuffer = overlapBuffer;
        }
    }
}
