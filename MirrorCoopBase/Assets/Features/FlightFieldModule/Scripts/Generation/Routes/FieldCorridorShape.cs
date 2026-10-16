namespace Features.FlightFieldModule.Scripts.Generation.Routes {
    // The corridor parameters of one main route, read from the field's profile at the route difficulty by
    // IFieldRouteShaper.
    public readonly struct FieldCorridorShape {
        public int HalfWidthCells { get; }
        public float WidthVariationCells { get; }
        public float MeanderAmplitudeCells { get; }
        public float MeanderPeriodCells { get; }
        public float DebrisDensity { get; }
        public int WidthCells => HalfWidthCells * 2 + 1;

        public FieldCorridorShape(int halfWidthCells, float widthVariationCells, float meanderAmplitudeCells, float meanderPeriodCells, float debrisDensity) {
            HalfWidthCells = halfWidthCells;
            WidthVariationCells = widthVariationCells;
            MeanderAmplitudeCells = meanderAmplitudeCells;
            MeanderPeriodCells = meanderPeriodCells;
            DebrisDensity = debrisDensity;
        }
    }
}
