namespace Features.FlightFieldModule.Scripts.Model {
    // One cell of the field matrix: why it is kept empty (Reserve) and which placement stands on it. Only a route
    // corridor may be reserved and occupied at once (obstacles inside a corridor).
    public readonly struct FieldCell {
        public const int NO_PLACEMENT = 0;

        public FieldReserveKind Reserve { get; }
        public int PlacementId { get; }
        public bool IsOccupied => PlacementId != NO_PLACEMENT;
        public bool IsReserved => Reserve != FieldReserveKind.None;
        public bool IsFree => !IsOccupied && !IsReserved;

        public FieldCell(FieldReserveKind reserve, int placementId) {
            Reserve = reserve;
            PlacementId = placementId;
        }

        public FieldCell WithReserve(FieldReserveKind reserve) =>
            new(reserve, PlacementId);

        public FieldCell WithoutPlacement() =>
            new(Reserve, NO_PLACEMENT);
    }
}
