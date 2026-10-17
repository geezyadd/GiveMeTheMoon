namespace Features.FlightFieldModule.Scripts.Generation.Routes {
    // The corridor parameters of one main route (one channel), read from the field's profile at the route difficulty
    // by IFieldRouteShaper.
    public readonly struct FieldCorridorShape {
        public int HalfWidthCells { get; }
        public float WidthVariationCells { get; }
        public float MeanderAmplitudeCells { get; }
        public float MeanderPeriodCells { get; }
        public float DebrisDensity { get; }
        // Full swings over the whole route; 0 = MeanderPeriodCells.
        public float MeanderWaves { get; }
        // How far this channel runs beside the straight line between the part and the meeting, cells.
        public float LateralOffsetCells { get; }
        public int WidthCells => HalfWidthCells * 2 + 1;

        public FieldCorridorShape(int halfWidthCells, float widthVariationCells, float meanderAmplitudeCells, float meanderPeriodCells, float debrisDensity, float meanderWaves,
                                  float lateralOffsetCells) {
            HalfWidthCells = halfWidthCells;
            WidthVariationCells = widthVariationCells;
            MeanderAmplitudeCells = meanderAmplitudeCells;
            MeanderPeriodCells = meanderPeriodCells;
            DebrisDensity = debrisDensity;
            MeanderWaves = meanderWaves;
            LateralOffsetCells = lateralOffsetCells;
        }

        public FieldCorridorShape WithLateralOffset(float lateralOffsetCells) =>
            new(HalfWidthCells, WidthVariationCells, MeanderAmplitudeCells, MeanderPeriodCells, DebrisDensity, MeanderWaves, lateralOffsetCells);
    }
}
