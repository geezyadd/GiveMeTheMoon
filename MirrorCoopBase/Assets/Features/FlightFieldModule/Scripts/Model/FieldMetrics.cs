namespace Features.FlightFieldModule.Scripts.Model {
    // How the generated field plays: the zone shares and how the rocks along the main course feel. Filled by the
    // balance metric step, read by the debug map and tests.
    public sealed class FieldMetrics {
        public float BlockedShare { get; set; }
        public float DebrisShare { get; set; }
        public float EmptyShare { get; set; }
        // Debris rocks on the main centerline the ship has to turn away from per minute at cruise speed.
        public float DodgesPerMinute { get; set; }
    }
}
