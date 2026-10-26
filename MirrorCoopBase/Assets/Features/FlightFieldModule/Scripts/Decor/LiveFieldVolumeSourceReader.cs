using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Network;
using Features.FlightFieldModule.Scripts.Network.Generated;
using Features.FlightFieldModule.Scripts.Requests;
using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Decor {
    // Reads the volume source from the live field model, the same on every peer: the profile by its synced id and its
    // volume (its own or the configuration's), the field's difficulty (the hardest route), the ends of the first main
    // route's centerline and the spheres of the field's giants. The giants are read again only when the
    // field number or the placement count changes (a client may get the lists after the state): the source is read
    // for every decor chunk.
    public sealed class LiveFieldVolumeSourceReader : IFieldVolumeSourceReader {
        private const float CELL_CENTER = 0.5f;
        private const int NO_ROUTE = -1;

        private readonly IReadOnlyLiveFieldModel _liveFieldModel;
        private readonly FlightFieldConfiguration _flightFieldConfiguration;
        private readonly IFieldProfileLookup _fieldProfileLookup;
        private readonly FieldStructureCatalog _fieldStructureCatalog;
        private readonly IFieldFootprintReader _fieldFootprintReader;
        private readonly IFieldStructureLayout _fieldStructureLayout;
        private readonly List<FieldVolumeGiant> _giants = new();

        private int _giantsFieldNumber = LiveFieldNumbers.NO_FIELD;
        private int _giantsPlacementCount;

        public LiveFieldVolumeSourceReader(IReadOnlyLiveFieldModel liveFieldModel, FlightFieldConfiguration flightFieldConfiguration, IFieldProfileLookup fieldProfileLookup,
                                           FieldStructureCatalog fieldStructureCatalog, IFieldFootprintReader fieldFootprintReader, IFieldStructureLayout fieldStructureLayout) {
            _liveFieldModel = liveFieldModel;
            _flightFieldConfiguration = flightFieldConfiguration;
            _fieldProfileLookup = fieldProfileLookup;
            _fieldStructureCatalog = fieldStructureCatalog;
            _fieldFootprintReader = fieldFootprintReader;
            _fieldStructureLayout = fieldStructureLayout;
        }

        public bool TryRead(out FieldVolumeSource source) {
            source = default;
            int mainRouteId = FindFirstMainRouteId(out float difficulty);
            if (mainRouteId == NO_ROUTE || !_fieldProfileLookup.TryFind(_liveFieldModel.ProfileId, out FieldDifficultyProfile profile))
                return false;

            if (!TryFindRouteEnds(mainRouteId, out Vector2Int start, out Vector2Int end))
                return false;

            float cellSize = _liveFieldModel.CellSize;
            FieldVolumeConfiguration volume = profile.HasOwnVolume ? profile.Volume : _flightFieldConfiguration.Volume;
            ReadGiants();
            source = new FieldVolumeSource(profile, volume, difficulty, cellSize, OnPlane(start, cellSize), OnPlane(end, cellSize), _giants);
            return true;
        }

        private void ReadGiants() {
            bool isRead = _giantsFieldNumber == _liveFieldModel.Number && _giantsPlacementCount == _liveFieldModel.Placements.Count;
            if (isRead)
                return;

            _giantsFieldNumber = _liveFieldModel.Number;
            _giantsPlacementCount = _liveFieldModel.Placements.Count;
            _giants.Clear();
            foreach (FieldPlacementEntry placement in _liveFieldModel.Placements)
                _giants.Add(ToGiant(placement));
        }

        private FieldVolumeGiant ToGiant(FieldPlacementEntry placement) {
            FieldStructureDefinition structure = _fieldStructureCatalog.GetRequired(placement.StructureId);
            FieldFootprint footprint = _fieldFootprintReader.Read(structure, placement.Rotation);
            Pose pose = _fieldStructureLayout.GetFieldPose(footprint, placement.Anchor, placement.Rotation, _liveFieldModel.CellSize);
            return FieldVolumeGiant.CreateFromPlacement(structure, footprint, pose, _liveFieldModel.CellSize);
        }

        private int FindFirstMainRouteId(out float hardestDifficulty) {
            hardestDifficulty = 0f;
            int firstId = NO_ROUTE;
            foreach (FieldRouteEntry route in _liveFieldModel.Routes) {
                hardestDifficulty = Mathf.Max(hardestDifficulty, route.Difficulty);
                bool isEarlierMain = route.Kind == FieldRouteKind.Main && (firstId == NO_ROUTE || route.Id < firstId);
                if (isEarlierMain)
                    firstId = route.Id;
            }

            return firstId;
        }

        private bool TryFindRouteEnds(int routeId, out Vector2Int start, out Vector2Int end) {
            start = default;
            end = default;
            bool isFound = false;
            foreach (FieldRouteCellEntry routeCell in _liveFieldModel.RouteCells) {
                if (routeCell.RouteId != routeId)
                    continue;

                if (!isFound)
                    start = routeCell.Cell;

                end = routeCell.Cell;
                isFound = true;
            }

            return isFound;
        }

        // Field space: x across (matrix y), y along (matrix x).
        private static Vector2 OnPlane(Vector2Int cell, float cellSize) =>
            new((cell.y + CELL_CENTER) * cellSize, (cell.x + CELL_CENTER) * cellSize);
    }
}
