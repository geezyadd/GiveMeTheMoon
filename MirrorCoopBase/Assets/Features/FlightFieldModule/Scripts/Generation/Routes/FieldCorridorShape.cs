namespace Features.FlightFieldModule.Scripts.Generation.Routes {
    // The corridor parameters of one route, made from its difficulty by IFieldRouteShaper.
    public readonly struct FieldCorridorShape {
        public int HalfWidthCells { get; }
        public int MaxSideOffsetCells { get; }
        public float ObstacleShare { get; }
        public int WidthCells => HalfWidthCells * 2 + 1;

        public FieldCorridorShape(int halfWidthCells, int maxSideOffsetCells, float obstacleShare) {
            HalfWidthCells = halfWidthCells;
            MaxSideOffsetCells = maxSideOffsetCells;
            ObstacleShare = obstacleShare;
        }
    }
}
