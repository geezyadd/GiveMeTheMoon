using Features.FlightFieldModule.Scripts.Network.Generated;

namespace Features.FlightFieldModule.Scripts.Network {
    // Where a field publish writes to: the live field bridge in the game, a recorder in tests.
    public interface IFieldPublishTarget {
        public void AddPlacement(FieldPlacementEntry placement);
        public void AddRoute(FieldRouteEntry route);
        public void AddRouteCell(FieldRouteCellEntry routeCell);
        public void AddDebrisRun(FieldDebrisRunEntry debrisRun);
        public void AddTarget(FieldTargetEntry target);
        public void SetState(LiveFieldState state);
    }
}
