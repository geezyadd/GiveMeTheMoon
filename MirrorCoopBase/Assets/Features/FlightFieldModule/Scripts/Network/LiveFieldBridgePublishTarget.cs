using Features.FlightFieldModule.Scripts.Network.Generated;

namespace Features.FlightFieldModule.Scripts.Network {
    // Writes a field publish into the live field bridge on the server.
    public sealed class LiveFieldBridgePublishTarget : IFieldPublishTarget {
        private readonly LiveFieldBridge _bridge;

        public LiveFieldBridgePublishTarget(LiveFieldBridge bridge) =>
            _bridge = bridge;

        public void AddPlacement(FieldPlacementEntry placement) =>
            _bridge.ServerAddPlacements(placement);

        public void AddRoute(FieldRouteEntry route) =>
            _bridge.ServerAddRoutes(route);

        public void AddRouteCell(FieldRouteCellEntry routeCell) =>
            _bridge.ServerAddRouteCells(routeCell);

        public void AddDebrisRun(FieldDebrisRunEntry debrisRun) =>
            _bridge.ServerAddDebrisRuns(debrisRun);

        public void AddTarget(FieldTargetEntry target) =>
            _bridge.ServerAddTargets(target);

        public void SetState(LiveFieldState state) =>
            _bridge.ServerSetState(state);
    }
}
