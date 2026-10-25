using UnityEngine;

namespace Features.ShipImpactModule.Scripts {
    public interface IShipImpactEffectPool {
        // Plays a free (or new) impact effect at a world position.
        public void Play(Vector3 position, float size);
    }
}
