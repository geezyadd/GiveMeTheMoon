namespace Features.FlightFieldModule.Scripts.Debris {
    // What one debris layout call works on: the field seed, the cell size, the cells around, the caller's workspace and
    // the largest rock radius of the catalog (read once per call).
    public readonly struct DebrisLayoutFrame {
        public int FieldSeed { get; }
        public float CellSize { get; }
        public IFieldDebrisCells Cells { get; }
        public FieldDebrisWorkspace Workspace { get; }
        public float LargestRadius { get; }

        public DebrisLayoutFrame(int fieldSeed, float cellSize, IFieldDebrisCells cells, FieldDebrisWorkspace workspace, float largestRadius) {
            FieldSeed = fieldSeed;
            CellSize = cellSize;
            Cells = cells;
            Workspace = workspace;
            LargestRadius = largestRadius;
        }
    }
}
