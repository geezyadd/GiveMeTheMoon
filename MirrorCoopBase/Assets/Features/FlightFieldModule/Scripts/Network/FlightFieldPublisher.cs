using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Network.Generated;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Network {
    // Writes a generated field into the live field model: the placements are the content every peer builds from, the
    // routes are kept for the debug map and later features. The scalar state goes last, so whoever reacts to a new
    // field number already finds its lists. All of it goes out in one broadcast: a field of a few hundred placements
    // is a few kilobytes, far below the transports' reliable message limit, so it is not sent in parts.
    public sealed class FlightFieldPublisher : IFlightFieldPublisher {
        private readonly IFlightFieldSyncBindingModel _flightFieldSyncBindingModel;

        public FlightFieldPublisher(IFlightFieldSyncBindingModel flightFieldSyncBindingModel) =>
            _flightFieldSyncBindingModel = flightFieldSyncBindingModel;

        public void Publish(FlightField field, int number) {
            LiveFieldBridge bridge = _flightFieldSyncBindingModel.Bridge;
            ClearLists(bridge);
            foreach (FieldPlacement placement in field.Placements)
                bridge.ServerAddPlacements(new FieldPlacementEntry { StructureId = placement.StructureId, Anchor = placement.Anchor, Rotation = placement.Rotation });

            foreach (FieldRoute route in field.Routes)
                PublishRoute(bridge, route);

            bridge.ServerSetState(new LiveFieldState {
                Number = number,
                Seed = field.Seed,
                Origin = field.Frame.Origin,
                Forward = field.Frame.Forward,
                CellSize = field.Frame.CellSize,
                SideCells = field.Frame.SideCells,
            });
        }

        // The bridge is gone when the game scene stops before the run is reset; then there is nothing to clear.
        public void Clear() {
            if (!_flightFieldSyncBindingModel.IsBound)
                return;

            LiveFieldBridge bridge = _flightFieldSyncBindingModel.Bridge;
            bridge.ServerSetState(default);
            ClearLists(bridge);
        }

        public void ShiftOrigin(Vector3 delta) {
            if (!_flightFieldSyncBindingModel.IsBound)
                return;

            LiveFieldBridge bridge = _flightFieldSyncBindingModel.Bridge;
            LiveFieldState state = bridge.State;
            if (state.Number == LiveFieldNumbers.NO_FIELD)
                return;

            state.Origin += delta;
            bridge.ServerSetState(state);
        }

        private static void PublishRoute(LiveFieldBridge bridge, FieldRoute route) {
            bridge.ServerAddRoutes(new FieldRouteEntry { Id = route.Id, Difficulty = route.Difficulty, CorridorWidthCells = route.CorridorWidthCells });
            foreach (Vector2Int cell in route.Centerline)
                bridge.ServerAddRouteCells(new FieldRouteCellEntry { RouteId = route.Id, Cell = cell });
        }

        private static void ClearLists(LiveFieldBridge bridge) {
            bridge.ServerClearPlacements();
            bridge.ServerClearRoutes();
            bridge.ServerClearRouteCells();
        }
    }
}
