using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Debris;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Network;

namespace Features.FlightFieldModule.Scripts.Rocks {
    // What a rock query reads of one field, set once per field: its seed, its placements with the placement index of
    // every placement id the cells store, its cells and debris cells and the debris layout's workspace (its drawn points
    // belong to this field only). The server and the generator read a generated field; every peer reads the published
    // one; the rock layouts are the same for both.
    public sealed class FieldRockFieldData {
        private readonly Dictionary<int, int> _placementIndexById = new();
        private readonly List<FieldPlacement> _placements = new();

        public int Number { get; private set; } = LiveFieldNumbers.NO_FIELD;
        public bool HasField => Cells != null;
        public int Seed { get; private set; }
        public IReadOnlyList<FieldPlacement> Placements => _placements;
        public IFieldRockCells Cells { get; private set; }
        public IFieldDebrisCells DebrisCells { get; private set; }
        // The generated field when the data came from one (its working grid); null for a published field.
        public FlightField Field { get; private set; }
        public FieldDebrisWorkspace Workspace { get; }

        public FieldRockFieldData(FieldDebrisWorkspace workspace) =>
            Workspace = workspace;

        public void Read(FlightField field, int number, IFieldDebrisCells debrisCells) {
            Read(number, field.Seed, field.Placements, new MatrixFieldRockCells(field.Cells), debrisCells);
            Field = field;
        }

        public void Read(int number, int seed, IReadOnlyList<FieldPlacement> placements, IFieldRockCells cells, IFieldDebrisCells debrisCells) {
            Clear();
            Number = number;
            Seed = seed;
            Cells = cells;
            DebrisCells = debrisCells;
            for (int i = 0; i < placements.Count; i++) {
                _placements.Add(placements[i]);
                _placementIndexById.Add(placements[i].Id, i);
            }
        }

        public bool TryGetPlacementIndex(int placementId, out int placementIndex) =>
            _placementIndexById.TryGetValue(placementId, out placementIndex);

        public void Clear() {
            _placementIndexById.Clear();
            _placements.Clear();
            Workspace.Clear();
            Field = null;
            Cells = null;
            DebrisCells = null;
            Seed = 0;
            Number = LiveFieldNumbers.NO_FIELD;
        }
    }
}
