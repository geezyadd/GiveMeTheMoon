namespace Features.FlightFieldModule.Scripts.Model {
    public readonly struct FieldRouteRequest {
        // 0 is the easiest route, 1 the hardest.
        public float Difficulty { get; }

        public FieldRouteRequest(float difficulty) =>
            Difficulty = difficulty;
    }
}
