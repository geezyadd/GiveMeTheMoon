using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Debris;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Network;
using Features.FlightFieldModule.Scripts.Requests;
using Features.FlightFieldModule.Scripts.Server;
using Features.FlightFieldModule.Scripts.Structures;

namespace Features.FlightFieldModule.Scripts.Rocks {
    // Server: the generated field, from the moment it stands, before it is published.
    public sealed class ServerFieldRockFieldSource : IFieldRockFieldSource {
        private readonly IReadOnlyFlightFieldServerModel _flightFieldServerModel;
        private readonly FieldStructureCatalog _fieldStructureCatalog;
        private readonly IFieldProfileLookup _fieldProfileLookup;

        public int Number => _flightFieldServerModel.HasField ? _flightFieldServerModel.Number : LiveFieldNumbers.NO_FIELD;
        public FieldFrame Frame => _flightFieldServerModel.Frame;

        public ServerFieldRockFieldSource(IReadOnlyFlightFieldServerModel flightFieldServerModel, FieldStructureCatalog fieldStructureCatalog, IFieldProfileLookup fieldProfileLookup) {
            _flightFieldServerModel = flightFieldServerModel;
            _fieldStructureCatalog = fieldStructureCatalog;
            _fieldProfileLookup = fieldProfileLookup;
        }

        public void Read(FieldRockFieldData fieldData) {
            FlightField field = _flightFieldServerModel.Field;
            FieldDifficultyProfile profile = _fieldProfileLookup.GetRequired(field.ProfileId);
            fieldData.Read(field, _flightFieldServerModel.Number, new MatrixFieldDebrisCells(field.Cells, _fieldStructureCatalog, profile));
        }
    }
}
