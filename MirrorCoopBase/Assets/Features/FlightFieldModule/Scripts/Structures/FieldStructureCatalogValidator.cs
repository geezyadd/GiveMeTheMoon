using System;
using System.Collections.Generic;
using Mirror;
using Zenject;

namespace Features.FlightFieldModule.Scripts.Structures {
    // The one startup check of the structure catalog: every entry set, ids present and unique, a footprint of 'X' and
    // '.' rows of one length with at least one occupied cell, a kind and a prefab; at least one obstacle the random
    // fill can pick.
    public sealed class FieldStructureCatalogValidator : IInitializable {
        private readonly FieldStructureCatalog _fieldStructureCatalog;

        public FieldStructureCatalogValidator(FieldStructureCatalog fieldStructureCatalog) =>
            _fieldStructureCatalog = fieldStructureCatalog;

        public void Initialize() {
            HashSet<string> ids = new(StringComparer.Ordinal);
            for (int i = 0; i < _fieldStructureCatalog.Structures.Count; i++)
                ValidateStructure(_fieldStructureCatalog.Structures[i], i, ids);

            if (!HasRandomObstacle())
                throw new InvalidOperationException($"{_fieldStructureCatalog.name}: no obstacle with a positive weight, the field would have no fill.");
        }

        private bool HasRandomObstacle() {
            foreach (FieldStructureDefinition structure in _fieldStructureCatalog.Structures) {
                if (structure.IsRandomObstacle)
                    return true;
            }

            return false;
        }

        private void ValidateStructure(FieldStructureDefinition structure, int index, HashSet<string> ids) {
            if (structure == null)
                throw new InvalidOperationException($"{_fieldStructureCatalog.name}: structure entry {index} is empty.");

            if (string.IsNullOrEmpty(structure.Id))
                throw new InvalidOperationException($"{_fieldStructureCatalog.name}: structure {structure.name} has no id.");

            if (!ids.Add(structure.Id))
                throw new InvalidOperationException($"{_fieldStructureCatalog.name}: structure id '{structure.Id}' is used twice (second: {structure.name}).");

            if (structure.Kind == FieldStructureKind.None)
                throw new InvalidOperationException($"{_fieldStructureCatalog.name}: structure {structure.Id} has no kind.");

            if (structure.Prefab == null)
                throw new InvalidOperationException($"{_fieldStructureCatalog.name}: structure {structure.Id} has no prefab.");

            if (structure.IsNetworked && !structure.Prefab.TryGetComponent(out NetworkIdentity _))
                throw new InvalidOperationException($"{_fieldStructureCatalog.name}: networked structure {structure.Id} has no NetworkIdentity on its prefab.");

            ValidateFootprint(structure);
        }

        private void ValidateFootprint(FieldStructureDefinition structure) {
            IReadOnlyList<string> rows = structure.FootprintRows;
            if (rows.Count == 0 || string.IsNullOrEmpty(rows[0]))
                throw new InvalidOperationException($"{_fieldStructureCatalog.name}: structure {structure.Id} has an empty footprint.");

            bool hasOccupiedCell = false;
            for (int y = 0; y < rows.Count; y++) {
                if (rows[y].Length != rows[0].Length)
                    throw new InvalidOperationException($"{_fieldStructureCatalog.name}: footprint rows of structure {structure.Id} differ in length.");

                hasOccupiedCell |= ValidateFootprintRow(structure, rows[y]);
            }

            if (!hasOccupiedCell)
                throw new InvalidOperationException($"{_fieldStructureCatalog.name}: footprint of structure {structure.Id} has no '{FieldFootprintSymbols.OCCUPIED_CELL}' cell.");
        }

        private bool ValidateFootprintRow(FieldStructureDefinition structure, string row) {
            bool hasOccupiedCell = false;
            foreach (char cell in row) {
                if (cell != FieldFootprintSymbols.OCCUPIED_CELL && cell != FieldFootprintSymbols.FREE_CELL)
                    throw new InvalidOperationException($"{_fieldStructureCatalog.name}: footprint of structure {structure.Id} has '{cell}', only '{FieldFootprintSymbols.OCCUPIED_CELL}' and '{FieldFootprintSymbols.FREE_CELL}' are allowed.");

                hasOccupiedCell |= cell == FieldFootprintSymbols.OCCUPIED_CELL;
            }

            return hasOccupiedCell;
        }
    }
}
