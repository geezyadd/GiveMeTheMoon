using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Debris;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Network;
using Features.FlightFieldModule.Scripts.Network.Generated;
using Features.FlightFieldModule.Scripts.Requests;
using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Rocks {
    // Turns a published field (the live field's state, placements and debris runs) into rock field data: the same
    // placements by index, their footprint cells and the debris cells a peer lays its small rocks out from.
    public sealed class PublishedFieldRockReader {
        private readonly FieldStructureCatalog _fieldStructureCatalog;
        private readonly IFieldFootprintReader _fieldFootprintReader;
        private readonly IFieldProfileLookup _fieldProfileLookup;
        private readonly PublishedFieldRockCells _cells = new();
        private readonly LiveFieldDebrisCells _debrisCells = new();
        private readonly List<FieldPlacement> _placements = new();

        public PublishedFieldRockReader(FieldStructureCatalog fieldStructureCatalog, IFieldFootprintReader fieldFootprintReader, IFieldProfileLookup fieldProfileLookup) {
            _fieldStructureCatalog = fieldStructureCatalog;
            _fieldFootprintReader = fieldFootprintReader;
            _fieldProfileLookup = fieldProfileLookup;
        }

        public void Read(FieldRockFieldData fieldData, LiveFieldState state, IReadOnlyList<FieldPlacementEntry> placements, IReadOnlyList<FieldDebrisRunEntry> debrisRuns) {
            _placements.Clear();
            for (int i = 0; i < placements.Count; i++) {
                FieldPlacementEntry entry = placements[i];
                _placements.Add(new FieldPlacement(i + PublishedFieldRockCells.PLACEMENT_ID_OFFSET, entry.StructureId, entry.Anchor, entry.Rotation));
            }

            _cells.Read(new Vector2Int(state.LengthCells, state.WidthCells), placements, debrisRuns, _fieldStructureCatalog, _fieldFootprintReader);
            _debrisCells.Read(debrisRuns, placements, _fieldStructureCatalog, _fieldFootprintReader, _fieldProfileLookup.GetRequired(state.ProfileId));
            fieldData.Read(state.Number, state.Seed, _placements, _cells, _debrisCells);
        }
    }
}
