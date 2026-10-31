using System.Collections.Generic;
using Features.PointOfInterestModule.Scripts.Network.Generated;
using Features.ShipModule.Scripts.Contracts;

namespace Features.PointOfInterestModule.Scripts.Network {
    // The points live in the synced PointsOfInterest model, so every peer and a joining player get them. A field has
    // one to three, so a lookup walks the list (no allocation).
    public sealed class LivePointsOfInterest : IPointsOfInterest, IPointOfInterestWriter {
        private readonly IReadOnlyPointsOfInterestModel _pointsOfInterestModel;
        private readonly IReadOnlyComponentRegistry<PointOfInterestSync> _pointOfInterestSyncRegistry;

        public int Count => _pointsOfInterestModel.IsAvailable ? _pointsOfInterestModel.Points.Count : 0;
        public bool IsAvailable => _pointOfInterestSyncRegistry.Items.Count > 0;

        private PointOfInterestSync Sync => _pointOfInterestSyncRegistry.Items[0];

        public LivePointsOfInterest(IReadOnlyPointsOfInterestModel pointsOfInterestModel, IReadOnlyComponentRegistry<PointOfInterestSync> pointOfInterestSyncRegistry) {
            _pointsOfInterestModel = pointsOfInterestModel;
            _pointOfInterestSyncRegistry = pointOfInterestSyncRegistry;
        }

        public PointOfInterestEntry Get(int index) =>
            _pointsOfInterestModel.Points[index];

        public bool TryFindIndex(int id, out int index) {
            index = -1;
            if (!_pointsOfInterestModel.IsAvailable)
                return false;

            IReadOnlyList<PointOfInterestEntry> points = _pointsOfInterestModel.Points;
            for (int i = 0; i < points.Count; i++) {
                if (points[i].Id != id)
                    continue;

                index = i;
                return true;
            }

            return false;
        }

        public void ServerAdd(PointOfInterestEntry entry) =>
            Sync.ServerAddPoints(entry);

        public void ServerSet(int index, PointOfInterestEntry entry) =>
            Sync.ServerSetPoints(index, entry);

        public void ServerClear() =>
            Sync.ServerClearPoints();
    }
}
