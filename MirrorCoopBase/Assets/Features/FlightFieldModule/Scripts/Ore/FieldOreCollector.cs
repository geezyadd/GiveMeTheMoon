using System;
using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Server;

namespace Features.FlightFieldModule.Scripts.Ore {
    // Server: the one entry that takes a piece of ore in at the ship. A piece of the current field that is still there
    // leaves the synced list (every peer stops showing it) and OreCollected fires, so it fires once per piece whoever
    // asks and however often.
    public sealed class FieldOreCollector : IFieldOreCollector, IFieldOreEvents {
        private readonly IReadOnlyFlightFieldServerModel _flightFieldServerModel;
        private readonly IFieldOres _fieldOres;
        private readonly IFieldOreWriter _fieldOreWriter;
        private readonly IFieldOreQuery _fieldOreQuery;

        public event Action<FieldOreCollected> OreCollected;

        public FieldOreCollector(IReadOnlyFlightFieldServerModel flightFieldServerModel, IFieldOres fieldOres, IFieldOreWriter fieldOreWriter, IFieldOreQuery fieldOreQuery) {
            _flightFieldServerModel = flightFieldServerModel;
            _fieldOres = fieldOres;
            _fieldOreWriter = fieldOreWriter;
            _fieldOreQuery = fieldOreQuery;
        }

        public FieldOreCollectResult ServerCollect(FieldOreId id) {
            if (!_fieldOreWriter.IsAvailable || !_flightFieldServerModel.HasField)
                return FieldOreCollectResult.NoField;

            if (!_fieldOreQuery.TryGetOre(id, out FieldOreDescription ore) || !_fieldOres.TryFindIndex(id.Value, out int index))
                return FieldOreCollectResult.NotFound;

            _fieldOreWriter.ServerRemoveAt(index);
            OreCollected?.Invoke(new FieldOreCollected(ore, _flightFieldServerModel.Number, _flightFieldServerModel.Field.LoopIndex));
            return FieldOreCollectResult.Collected;
        }
    }
}
