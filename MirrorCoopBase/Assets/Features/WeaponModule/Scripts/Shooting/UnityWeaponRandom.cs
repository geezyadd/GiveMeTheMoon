using Features.WeaponModule.Scripts.Parts.Delivery;

namespace Features.WeaponModule.Scripts.Shooting {
    public sealed class UnityWeaponRandom : IWeaponRandom {
        // Random.value includes 1; the contract is [0, 1).
        private const float BELOW_ONE = 0.99999f;

        public float NextUnitValue() =>
            UnityEngine.Random.value * BELOW_ONE;
    }
}
