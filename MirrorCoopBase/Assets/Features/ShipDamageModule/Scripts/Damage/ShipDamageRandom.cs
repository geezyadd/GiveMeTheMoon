using UnityEngine;

namespace Features.ShipDamageModule.Scripts.Damage {
    public sealed class ShipDamageRandom : IShipDamageRandom {
        // Random.value includes 1; the rule's rolls are below 1.
        private const float BELOW_ONE = 0.9999f;

        public float Roll() =>
            Random.value * BELOW_ONE;
    }
}
