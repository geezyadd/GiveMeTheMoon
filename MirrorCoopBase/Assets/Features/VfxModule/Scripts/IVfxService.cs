using UnityEngine;

namespace Features.VfxModule {
    // Plays visual effects on this peer, from pools: nothing here goes over the network.
    public interface IVfxService {
        // A free-standing effect at a point; scale 1 is the prefab's own size.
        public void Play(VfxDefinition effect, Vector3 position, Quaternion rotation, float scale = 1f);

        // An effect that follows a transform until its handle stops it or the transform is gone.
        public VfxHandle Attach(VfxDefinition effect, Transform target);
    }
}
