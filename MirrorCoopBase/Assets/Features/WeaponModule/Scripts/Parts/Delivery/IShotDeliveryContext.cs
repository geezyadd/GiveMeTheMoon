using UnityEngine;

namespace Features.WeaponModule.Scripts.Parts.Delivery {
    // The world services a delivery definition may build its runtime delivery from. A new kind of delivery that needs
    // more (a projectile pool, a tether) gets it here, not through a wider CreateDelivery signature.
    public interface IShotDeliveryContext {
        public IWeaponRandom Random { get; }

        public IWeaponRaycast CreateRaycast(LayerMask hitLayers);

        // A ray as thick as a sphere of the radius: it strikes what passes within the radius of its line.
        public IWeaponRaycast CreateSphereCast(LayerMask hitLayers, float radiusMeters);
    }
}
