using System;
using System.Collections.Generic;
using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Debris;
using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Network;
using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Rocks {
    // Finds the rocks of the current field in a world volume from the field data, not from the scene (a peer builds
    // only the rocks around its own camera): the server reads its generated field, every peer the published one
    // (IFieldRockFieldSource), through the same reader and layouts. It looks only at the cells under the volume, widened
    // by the farthest a rock can reach past its own cell, reads their placements and debris through IFieldRockReader
    // and leaves out broken rocks. Allocates nothing per call once its lists have grown.
    public sealed class FieldRockQuery : IFieldRockQuery {
        private const float HALF = 0.5f;

        private readonly IFieldRockFieldSource _fieldRockFieldSource;
        private readonly IFieldRockReader _fieldRockReader;
        private readonly IFieldBrokenRocks _fieldBrokenRocks;
        private readonly FieldRockFieldData _fieldData;
        // The catalog does not change while the game runs, so the widest reach is read once.
        private readonly float _reachPastCell;
        private readonly List<FieldRockDescription> _cellRocks = new();
        private readonly HashSet<int> _visitedPlacements = new();

        public FieldRockQuery(IFieldRockFieldSource fieldRockFieldSource, FieldStructureCatalog fieldStructureCatalog, FlightFieldConfiguration flightFieldConfiguration,
                              IFieldRockReader fieldRockReader, IFieldBrokenRocks fieldBrokenRocks, IFieldRandomFactory fieldRandomFactory) {
            _fieldRockFieldSource = fieldRockFieldSource;
            _fieldRockReader = fieldRockReader;
            _fieldBrokenRocks = fieldBrokenRocks;
            _fieldData = new FieldRockFieldData(new FieldDebrisWorkspace(fieldRandomFactory.Create(0)));
            _reachPastCell = ReadReachPastCell(fieldStructureCatalog, flightFieldConfiguration);
        }

        public void FindInSphere(Vector3 center, float radius, List<FieldRockDescription> results) =>
            Find(FieldRockVolume.CreateSphere(center, radius), results);

        public void FindInBox(Vector3 center, Vector3 halfExtents, Quaternion rotation, List<FieldRockDescription> results) =>
            Find(FieldRockVolume.CreateBox(center, halfExtents, rotation), results);

        public bool TryGetRock(FieldRockId id, out FieldRockDescription rock) {
            rock = default;
            if (!TryReadField() || _fieldBrokenRocks.IsBroken(id) || !TryReadRocksOf(id))
                return false;

            if (id.RockIndex < 0 || id.RockIndex >= _cellRocks.Count)
                return false;

            rock = _cellRocks[id.RockIndex];
            return true;
        }

        private void Find(FieldRockVolume volume, List<FieldRockDescription> results) {
            results.Clear();
            if (!TryReadField())
                return;

            FieldFrame frame = _fieldRockFieldSource.Frame;
            Vector3 local = Quaternion.Inverse(Quaternion.LookRotation(frame.Forward, Vector3.up)) * (volume.Center - frame.Origin);
            // One more cell: a giant's main rock may stick out of its footprint by about a cell.
            float reach = volume.BoundingRadius + _reachPastCell + frame.CellSize;
            Vector2Int first = ClampToField(frame, ToCell(local.z - reach, local.x - reach, frame.CellSize));
            Vector2Int last = ClampToField(frame, ToCell(local.z + reach, local.x + reach, frame.CellSize));
            _visitedPlacements.Clear();
            for (int along = first.x; along <= last.x; along++) {
                for (int across = first.y; across <= last.y; across++)
                    AddCellRocks(frame, new Vector2Int(along, across), volume, results);
            }
        }

        private void AddCellRocks(FieldFrame frame, Vector2Int cell, FieldRockVolume volume, List<FieldRockDescription> results) {
            IFieldRockCells cells = _fieldData.Cells;
            if (cells.TryGetPlacementId(cell, out int placementId) && _fieldData.TryGetPlacementIndex(placementId, out int placementIndex) && _visitedPlacements.Add(placementIndex)) {
                _fieldRockReader.ReadPlacement(_fieldData, frame, placementIndex, _cellRocks);
                AddTouching(volume, results);
            }

            if (!cells.TryGetDebrisId(cell, out _))
                return;

            _fieldRockReader.ReadDebrisCell(_fieldData, frame, cell, _cellRocks);
            AddTouching(volume, results);
        }

        private void AddTouching(FieldRockVolume volume, List<FieldRockDescription> results) {
            foreach (FieldRockDescription rock in _cellRocks) {
                if (volume.Intersects(rock.Center, rock.Radius) && !_fieldBrokenRocks.IsBroken(rock.Id))
                    results.Add(rock);
            }
        }

        private bool TryReadRocksOf(FieldRockId id) {
            FieldFrame frame = _fieldRockFieldSource.Frame;
            switch (id.Source) {
                case FieldRockSource.Cluster:
                    if (id.PlacementIndex < 0 || id.PlacementIndex >= _fieldData.Placements.Count)
                        return false;

                    _fieldRockReader.ReadPlacement(_fieldData, frame, id.PlacementIndex, _cellRocks);
                    return true;
                case FieldRockSource.Debris:
                    if (!_fieldData.Cells.IsInField(id.DebrisCell))
                        return false;

                    _fieldRockReader.ReadDebrisCell(_fieldData, frame, id.DebrisCell, _cellRocks);
                    return true;
                default:
                    throw new ArgumentOutOfRangeException(nameof(id), id.Source, $"Rock {id} has no source.");
            }
        }

        // The field data follows the source's field: read again when a new one stands, dropped when none does.
        private bool TryReadField() {
            int number = _fieldRockFieldSource.Number;
            if (number == LiveFieldNumbers.NO_FIELD) {
                if (_fieldData.HasField)
                    _fieldData.Clear();

                return false;
            }

            if (_fieldData.Number != number)
                _fieldRockFieldSource.Read(_fieldData);

            return true;
        }

        // Matrix cell: x along the field, y across it.
        private static Vector2Int ToCell(float along, float across, float cellSize) =>
            new(Mathf.FloorToInt(along / cellSize), Mathf.FloorToInt(across / cellSize));

        private static Vector2Int ClampToField(FieldFrame frame, Vector2Int cell) =>
            new(Mathf.Clamp(cell.x, 0, frame.LengthCells - 1), Mathf.Clamp(cell.y, 0, frame.WidthCells - 1));

        // A debris rock's middle stands in its cell, so it reaches past it by its radius; a dense-field rock reaches
        // past its footprint by the spill (its middle moved by the jitter), an extra rock by its radius.
        private static float ReadReachPastCell(FieldStructureCatalog catalog, FlightFieldConfiguration configuration) {
            float reach = 0f;
            foreach (FieldStructureDefinition structure in catalog.Structures) {
                if (!structure.HasClusterRecipe || !structure.ClusterRecipe.HasPlaneCover)
                    continue;

                FieldClusterRecipe recipe = structure.ClusterRecipe;
                reach = Mathf.Max(reach, Mathf.Max(recipe.PlaneMaxSpillMeters + recipe.PlaneJitterMeters, recipe.ExtraDiameterMeters.y * HALF));
            }

            foreach (FieldDifficultyProfile profile in configuration.DifficultyProfiles) {
                foreach (FieldRockSizeClass sizeClass in profile.DebrisSizeClasses)
                    reach = Mathf.Max(reach, sizeClass.DiameterMeters.y * HALF);
            }

            return reach;
        }
    }
}
