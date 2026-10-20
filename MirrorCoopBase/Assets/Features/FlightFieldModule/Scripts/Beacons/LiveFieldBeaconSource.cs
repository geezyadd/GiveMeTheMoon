using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Debris;
using Features.FlightFieldModule.Scripts.Network;
using Features.FlightFieldModule.Scripts.Network.Generated;
using Features.FlightFieldModule.Scripts.Requests;
using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Beacons {
    // The beacons as a peer lays them out: the routes of the live field with their centerline cells (sent in
    // centerline order, each with the corridor's half width) and the cells read from its placements and debris runs,
    // once per field.
    public sealed class LiveFieldBeaconSource : IFieldBeaconSource {
        private readonly IFieldBeaconLayout _fieldBeaconLayout;
        private readonly FieldStructureCatalog _fieldStructureCatalog;
        private readonly IFieldFootprintReader _fieldFootprintReader;
        private readonly IFieldProfileLookup _fieldProfileLookup;
        private readonly LiveFieldDebrisCells _cells = new();
        private readonly List<FieldBeaconRoute> _routes = new();
        private readonly Dictionary<int, (List<Vector2Int> centerline, List<int> halfWidths)> _cellsByRoute = new();

        public LiveFieldBeaconSource(IFieldBeaconLayout fieldBeaconLayout, FieldStructureCatalog fieldStructureCatalog, IFieldFootprintReader fieldFootprintReader,
                                     IFieldProfileLookup fieldProfileLookup) {
            _fieldBeaconLayout = fieldBeaconLayout;
            _fieldStructureCatalog = fieldStructureCatalog;
            _fieldFootprintReader = fieldFootprintReader;
            _fieldProfileLookup = fieldProfileLookup;
        }

        public void Read(IReadOnlyLiveFieldModel liveFieldModel, List<FieldBeacon> beacons) {
            _cells.Read(liveFieldModel, _fieldStructureCatalog, _fieldFootprintReader, _fieldProfileLookup.GetRequired(liveFieldModel.ProfileId));
            ReadRoutes(liveFieldModel);
            _fieldBeaconLayout.Fill(_routes, liveFieldModel.CellSize, _cells, beacons);
        }

        private void ReadRoutes(IReadOnlyLiveFieldModel liveFieldModel) {
            _cellsByRoute.Clear();
            foreach (FieldRouteCellEntry routeCell in liveFieldModel.RouteCells) {
                if (!_cellsByRoute.TryGetValue(routeCell.RouteId, out (List<Vector2Int> centerline, List<int> halfWidths) cells)) {
                    cells = (new List<Vector2Int>(), new List<int>());
                    _cellsByRoute.Add(routeCell.RouteId, cells);
                }

                cells.centerline.Add(routeCell.Cell);
                cells.halfWidths.Add(routeCell.HalfWidth);
            }

            _routes.Clear();
            foreach (FieldRouteEntry route in liveFieldModel.Routes) {
                if (_cellsByRoute.TryGetValue(route.Id, out (List<Vector2Int> centerline, List<int> halfWidths) cells))
                    _routes.Add(new FieldBeaconRoute(route.Id, route.Kind, cells.centerline, cells.halfWidths));
            }
        }
    }
}
