using System.Collections.Generic;
using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Network;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Server {
    // The generated field's points of interest through the frame as it stands now, so a world shift moves them too.
    public sealed class ServerFieldPointOfInterestSource : IFieldPointOfInterestSource {
        private readonly IReadOnlyFlightFieldServerModel _flightFieldServerModel;

        public bool HasField => _flightFieldServerModel.HasField;
        public int FieldNumber => _flightFieldServerModel.HasField ? _flightFieldServerModel.Number : LiveFieldNumbers.NO_FIELD;

        public ServerFieldPointOfInterestSource(IReadOnlyFlightFieldServerModel flightFieldServerModel) =>
            _flightFieldServerModel = flightFieldServerModel;

        public void CollectPoints(List<Vector3> worldPositions) {
            worldPositions.Clear();
            if (!_flightFieldServerModel.HasField)
                return;

            foreach (Vector3 fieldPosition in _flightFieldServerModel.Field.PointsOfInterest)
                worldPositions.Add(_flightFieldServerModel.Frame.FieldToWorld(fieldPosition));
        }
    }
}
