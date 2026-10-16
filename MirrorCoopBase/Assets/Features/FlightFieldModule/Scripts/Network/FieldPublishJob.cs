using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Network.Generated;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Network {
    // One field on its way into the live field model, in batches: placements, then routes, then route cells, at most
    // the batch size of entries per batch, and the scalar state with the field number alone in the last batch, so a
    // peer reacting to the new number finds complete lists. A long field is tens of kilobytes; one batch per Mirror
    // delta keeps every delta below the transport's message limit.
    public sealed class FieldPublishJob {
        private readonly List<FieldPlacementEntry> _placements = new();
        private readonly List<FieldRouteEntry> _routes = new();
        private readonly List<FieldRouteCellEntry> _routeCells = new();
        private readonly int _entriesPerBatch;

        private LiveFieldState _state;
        private int _nextPlacement;
        private int _nextRoute;
        private int _nextRouteCell;

        public bool IsDone { get; private set; }

        public FieldPublishJob(FlightField field, int number, int entriesPerBatch) {
            _entriesPerBatch = Mathf.Max(1, entriesPerBatch);
            foreach (FieldPlacement placement in field.Placements)
                _placements.Add(new FieldPlacementEntry { StructureId = placement.StructureId, Anchor = placement.Anchor, Rotation = placement.Rotation });

            foreach (FieldRoute route in field.Routes)
                AddRoute(route);

            _state = new LiveFieldState {
                Number = number,
                Seed = field.Seed,
                Origin = field.Frame.Origin,
                Forward = field.Frame.Forward,
                CellSize = field.Frame.CellSize,
                LengthCells = field.Frame.LengthCells,
                WidthCells = field.Frame.WidthCells,
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

            if (budget == _entriesPerBatch) {
                target.SetState(_state);
                IsDone = true;
            }
        }

        private void AddRoute(FieldRoute route) {
            _routes.Add(new FieldRouteEntry { Id = route.Id, Difficulty = route.Difficulty, CorridorWidthCells = route.CorridorWidthCells });
            foreach (Vector2Int cell in route.Centerline)
                _routeCells.Add(new FieldRouteCellEntry { RouteId = route.Id, Cell = cell });
        }
    }
}
