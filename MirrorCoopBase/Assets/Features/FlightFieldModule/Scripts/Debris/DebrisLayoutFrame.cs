namespace Features.FlightFieldModule.Scripts.Debris {
    // What one debris layout call works on: the field seed, the cell size, the cells around and the caller's workspace.
    public readonly struct DebrisLayoutFrame {
        public int FieldSeed { get; }
        public float CellSize { get; }
        public IFieldDebrisCells Cells { get; }
        public FieldDebrisWorkspace Workspace { get; }

        public DebrisLayoutFrame(int fieldSeed, float cellSize, IFieldDebrisCells cells, FieldDebrisWorkspace workspace) {
            FieldSeed = fieldSeed;
            CellSize = cellSize;
            Cells = cells;
            Workspace = workspace;
        }
    }
}
