using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Debris;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Network;

namespace Features.FlightFieldModule.Scripts.Rocks {
    // What the server's rock query reads of one field, set once per field: the field, the placement index of every
    // placement id the cells store, the debris cells and the debris layout's workspace (its drawn points belong to
    // this field only).
    public sealed class FieldRockFieldData {
        private readonly Dictionary<int, int> _placementIndexById = new();

        public int Number { get; private set; } = LiveFieldNumbers.NO_FIELD;
        public FlightField Field { get; private set; }
        public IFieldDebrisCells DebrisCells { get; private set; }
        public FieldDebrisWorkspace Workspace { get; }

        public FieldRockFieldData(FieldDebrisWorkspace workspace) =>
            Workspace = workspace;

        public void Read(FlightField field, int number, IFieldDebrisCells debrisCells) {
            Clear();
            Field = field;
            Number = number;
            DebrisCells = debrisCells;
            for (int i = 0; i < field.Placements.Count; i++)
                _placementIndexById.Add(field.Placements[i].Id, i);
        }

        public bool TryGetPlacementIndex(int placementId, out int placementIndex) =>
            _placementIndexById.TryGetValue(placementId, out placementIndex);

        public void Clear() {
            _placementIndexById.Clear();
            Workspace.Clear();
            Field = null;
            DebrisCells = null;
            Number = LiveFieldNumbers.NO_FIELD;
        }
    }
}
