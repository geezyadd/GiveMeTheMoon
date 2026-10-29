using System.Collections.Generic;
using Features.FlightFieldModule.Contracts;
using Features.PointOfInterestModule.Scripts.Configuration;
using Features.PointOfInterestModule.Scripts.Network;
using UnityEngine;

namespace Features.PointOfInterestModule.Scripts.Server {
    // The contents are rolled from the field number, so one field always gets the same contents in its points.
    public sealed class PointOfInterestPlacementService : IPointOfInterestPlacementService {
        private const int FIRST_POINT_ID = 1;

        private readonly IFieldPointOfInterestSource _fieldPointOfInterestSource;
        private readonly IPointOfInterestWriter _pointOfInterestWriter;
        private readonly IPointOfInterestContentCatalog _pointOfInterestContentCatalog;
        private readonly PointOfInterestServerModel _pointOfInterestServerModel;

        public PointOfInterestPlacementService(IFieldPointOfInterestSource fieldPointOfInterestSource, IPointOfInterestWriter pointOfInterestWriter,
                                               IPointOfInterestContentCatalog pointOfInterestContentCatalog, PointOfInterestServerModel pointOfInterestServerModel) {
            _fieldPointOfInterestSource = fieldPointOfInterestSource;
            _pointOfInterestWriter = pointOfInterestWriter;
            _pointOfInterestContentCatalog = pointOfInterestContentCatalog;
            _pointOfInterestServerModel = pointOfInterestServerModel;
        }

        public void ServerPlaceForCurrentField() {
            int fieldNumber = _fieldPointOfInterestSource.FieldNumber;
            _pointOfInterestWriter.ServerClear();
            _pointOfInterestServerModel.SetPlacedField(fieldNumber);
            if (!_fieldPointOfInterestSource.HasField)
                return;

            List<Vector3> positions = new();
            _fieldPointOfInterestSource.CollectPoints(positions);
            System.Random random = new(fieldNumber);
            for (int i = 0; i < positions.Count; i++)
                _pointOfInterestWriter.ServerAdd(CreateEntry(FIRST_POINT_ID + i, positions[i], (float)random.NextDouble()));
        }

        private PointOfInterestEntry CreateEntry(int id, Vector3 position, float contentRoll) =>
            new() {
                Id = id,
                ContentId = _pointOfInterestContentCatalog.Pick(contentRoll).Id,
                Position = position,
                State = PointOfInterestState.Dormant,
            };
    }
}
