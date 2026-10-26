using System.Collections.Generic;
using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Rocks;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Targets {
    // What one balloon placement pass reads the rocks with: the field's rock data and the debris rocks of every cell
    // it has laid out so far, so a cell is laid out once per field however many spots are tried near it. Made per
    // generation, so generations on several threads share nothing.
    public sealed class FieldTargetRockWorkspace {
        private readonly Dictionary<Vector2Int, List<FieldRockDescription>> _debrisRocks = new();

        public FieldRockFieldData FieldData { get; }

        public FieldTargetRockWorkspace(FieldRockFieldData fieldData) =>
            FieldData = fieldData;

        public bool TryGetDebrisRocks(Vector2Int cell, out List<FieldRockDescription> rocks) =>
            _debrisRocks.TryGetValue(cell, out rocks);

        public void AddDebrisRocks(Vector2Int cell, List<FieldRockDescription> rocks) =>
            _debrisRocks.Add(cell, rocks);
    }
}
