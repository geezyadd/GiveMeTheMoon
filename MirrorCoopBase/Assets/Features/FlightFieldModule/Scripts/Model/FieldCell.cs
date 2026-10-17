namespace Features.FlightFieldModule.Scripts.Model {
    // One cell of the field matrix: why it is kept empty (Reserve), what the generator decided it is (Class), which
    // placement stands on it and, for a debris cell, how dense its small rocks are and which debris type they are.
    // Only corridor cells may be reserved and occupied at once (a closed alternative route).
    public readonly struct FieldCell {
        public const int NO_PLACEMENT = 0;

        public FieldReserveKind Reserve { get; }
        public int PlacementId { get; }
        public FieldCellClass Class { get; }
        public float DebrisDensity { get; }
        public string DebrisId { get; }
        public bool IsOccupied => PlacementId != NO_PLACEMENT;
        public bool IsReserved => Reserve != FieldReserveKind.None;
        public bool IsFree => !IsOccupied && !IsReserved;
        public bool IsCorridor => Reserve is FieldReserveKind.RouteCorridor or FieldReserveKind.AlternativeCorridor or FieldReserveKind.DeadEndCorridor;
        public bool IsBlockedClass => Class is FieldCellClass.Wall or FieldCellClass.Ridge;
        public bool HasDebris => Class == FieldCellClass.Debris && DebrisDensity > 0f;

        public FieldCell(FieldReserveKind reserve, int placementId, FieldCellClass cellClass) {
            Reserve = reserve;
            PlacementId = placementId;
            Class = cellClass;
            DebrisDensity = 0f;
            DebrisId = null;
        }

        private FieldCell(FieldReserveKind reserve, int placementId, FieldCellClass cellClass, float debrisDensity, string debrisId) {
            Reserve = reserve;
            PlacementId = placementId;
            Class = cellClass;
            DebrisDensity = debrisDensity;
            DebrisId = debrisId;
        }

        public FieldCell WithReserve(FieldReserveKind reserve) =>
            new(reserve, PlacementId, Class, DebrisDensity, DebrisId);

        public FieldCell WithClass(FieldCellClass cellClass) =>
            new(Reserve, PlacementId, cellClass, 0f, null);

        public FieldCell WithDebris(float density, string debrisId) =>
            new(Reserve, PlacementId, FieldCellClass.Debris, density, debrisId);

        // A cell whose structure is taken out is open again.
        public FieldCell WithoutPlacement() =>
            new(Reserve, NO_PLACEMENT, FieldCellClass.Empty, 0f, null);
    }
}
