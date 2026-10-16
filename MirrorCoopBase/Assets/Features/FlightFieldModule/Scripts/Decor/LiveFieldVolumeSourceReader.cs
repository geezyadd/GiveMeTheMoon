using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Network;
using Features.FlightFieldModule.Scripts.Network.Generated;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Decor {
    // Reads the volume source from the live field model, the same on every peer: the profile by its synced id, the
    // field's difficulty (the hardest route) and the ends of the first main route's centerline.
    public sealed class LiveFieldVolumeSourceReader : IFieldVolumeSourceReader {
        private const float CELL_CENTER = 0.5f;
        private const int NO_ROUTE = -1;

        private readonly IReadOnlyLiveFieldModel _liveFieldModel;
        private readonly FlightFieldConfiguration _flightFieldConfiguration;

        public LiveFieldVolumeSourceReader(IReadOnlyLiveFieldModel liveFieldModel, FlightFieldConfiguration flightFieldConfiguration) {
            _liveFieldModel = liveFieldModel;
            _flightFieldConfiguration = flightFieldConfiguration;
        }

        public bool TryRead(out FieldVolumeSource source) {
            source = default;
            int mainRouteId = FindFirstMainRouteId(out float difficulty);
            if (mainRouteId == NO_ROUTE || !TryFindProfile(_liveFieldModel.ProfileId, out FieldDifficultyProfile profile))
                return false;

            if (!TryFindRouteEnds(mainRouteId, out Vector2Int start, out Vector2Int end))
                return false;

            float cellSize = _liveFieldModel.CellSize;
            source = new FieldVolumeSource(profile, difficulty, cellSize, OnPlane(start, cellSize), OnPlane(end, cellSize));
            return true;
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

        private bool TryFindProfile(string id, out FieldDifficultyProfile found) {
            foreach (FieldDifficultyProfile profile in _flightFieldConfiguration.DifficultyProfiles) {
                if (profile.Id != id)
                    continue;

                found = profile;
                return true;
            }

            found = null;
            return false;
        }

        // Field space: x across (matrix y), y along (matrix x).
        private static Vector2 OnPlane(Vector2Int cell, float cellSize) =>
            new((cell.y + CELL_CENTER) * cellSize, (cell.x + CELL_CENTER) * cellSize);
    }
}
