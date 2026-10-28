namespace Features.WeaponModule.Contracts {
    // How a weapon that hooks its target pulls it: the target comes at a constant speed and the rope breaks when the
    // holder gets farther from it than the rope is long.
    public readonly struct WeaponPull {
        public float PullSpeedMetersPerSecond { get; }
        public float MaxRopeMeters { get; }

        public WeaponPull(float pullSpeedMetersPerSecond, float maxRopeMeters) {
            PullSpeedMetersPerSecond = pullSpeedMetersPerSecond;
            MaxRopeMeters = maxRopeMeters;
        }
    }
}
