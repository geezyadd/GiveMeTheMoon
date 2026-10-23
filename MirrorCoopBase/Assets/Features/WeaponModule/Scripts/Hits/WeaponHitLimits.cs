using Features.WeaponModule.Scripts.Definitions;

namespace Features.WeaponModule.Scripts.Hits {
    // The numbers of one weapon the server checks a reported hit against.
    public readonly struct WeaponHitLimits {
        public WeaponHitLimits(float shotsPerSecond, float rangeMeters, int maxHitsPerShot) {
            ShotsPerSecond = shotsPerSecond;
            RangeMeters = rangeMeters;
            MaxHitsPerShot = maxHitsPerShot;
        }

        public float ShotsPerSecond { get; }
        public float RangeMeters { get; }
        public int MaxHitsPerShot { get; }

        public static WeaponHitLimits CreateFor(WeaponDefinition weapon) =>
            new WeaponHitLimits(weapon.ShotsPerSecond, weapon.RangeMeters, weapon.Delivery.MaxHitsPerShot);
    }
}
