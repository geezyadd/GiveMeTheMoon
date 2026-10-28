using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Network.Generated;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Network {
    // One field on its way into the live field model, in batches: placements, then routes, then route cells, then
    // debris runs (one per row stretch of debris cells of one kind and density), at most the batch size of entries per batch, and the scalar state with the field number alone in the last batch, so a
    // peer reacting to the new number finds complete lists. A long field is tens of kilobytes; one batch per Mirror
    // delta keeps every delta below the transport's message limit.
    public sealed class FieldPublishJob {
        private readonly List<FieldPlacementEntry> _placements = new();
        private readonly List<FieldRouteEntry> _routes = new();
        private readonly List<FieldRouteCellEntry> _routeCells = new();
        private readonly List<FieldDebrisRunEntry> _debrisRuns = new();
        private readonly int _entriesPerBatch;

        private LiveFieldState _state;
        private int _nextPlacement;
        private int _nextRoute;
        private int _nextRouteCell;
        private int _nextDebrisRun;

        public bool IsDone { get; private set; }

        public FieldPublishJob(FlightField field, int number, int entriesPerBatch) {
            _entriesPerBatch = Mathf.Max(1, entriesPerBatch);
            foreach (FieldPlacement placement in field.Placements)
                _placements.Add(new FieldPlacementEntry { StructureId = placement.StructureId, Anchor = placement.Anchor, Rotation = placement.Rotation });

            foreach (FieldRoute route in field.Routes)
                AddRoute(route);

            AddDebrisRuns(field);

            _state = new LiveFieldState {
                Number = number,
                Seed = field.Seed,
                Origin = field.Frame.Origin,
                Forward = field.Frame.Forward,
                CellSize = field.Frame.CellSize,
                LengthCells = field.Frame.LengthCells,
                WidthCells = field.Frame.WidthCells,
                ProfileId = field.ProfileId,
            };
        }

        // A world shift while the field is still on its way: the state not sent yet follows it.
        public void ShiftOrigin(Vector3 delta) =>
            _state.Origin += delta;

        public void WriteNextBatch(IFieldPublishTarget target) {
            if (IsDone)
                return;

            int budget = _entriesPerBatch;
            while (budget > 0 && _nextPlacement < _placements.Count) {
                target.AddPlacement(_placements[_nextPlacement++]);
                budget--;
            }

            while (budget > 0 && _nextRoute < _routes.Count) {
                target.AddRoute(_routes[_nextRoute++]);
                budget--;
            }

            while (budget > 0 && _nextRouteCell < _routeCells.Count) {
                target.AddRouteCell(_routeCells[_nextRouteCell++]);
                budget--;
            }

            while (budget > 0 && _nextDebrisRun < _debrisRuns.Count) {
                target.AddDebrisRun(_debrisRuns[_nextDebrisRun++]);
                budget--;
            }

            if (budget == _entriesPerBatch) {
                target.SetState(_state);
                IsDone = true;
            }
        }

        private void AddDebrisRuns(FlightField field) {
            Vector2Int size = field.Frame.Size;
            for (int y = 0; y < size.y; y++) {
                for (int x = 0; x < size.x;)
                    x = AddDebrisRunFrom(field, new Vector2Int(x, y), size.x);
            }
        }

        // Returns the column after the run (or after the cell, when it has no debris).
        private int AddDebrisRunFrom(FlightField field, Vector2Int start, int lengthCells) {
            FieldCell first = field.Cells[start];
            if (!first.HasDebris)
                return start.x + 1;

            int end = start.x + 1;
            while (end < lengthCells && IsSameDebris(first, field.Cells[end, start.y]))
                end++;

            _debrisRuns.Add(new FieldDebrisRunEntry { Start = start, Length = end - start.x, Density = first.DebrisDensity, DebrisId = first.DebrisId });
            return end;
        }

        private static bool IsSameDebris(FieldCell first, FieldCell next) =>
            next.HasDebris && Mathf.Approximately(next.DebrisDensity, first.DebrisDensity) && next.DebrisId == first.DebrisId;

        private void AddRoute(FieldRoute route) {
            _routes.Add(new FieldRouteEntry { Id = route.Id, Kind = route.Kind, Difficulty = route.Difficulty, CorridorWidthCells = route.CorridorWidthCells });
            foreach (Vector2Int cell in route.Centerline)
                _routeCells.Add(new FieldRouteCellEntry { RouteId = route.Id, Cell = cell });
        }
    }
}
