using System;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Structures;
using Zenject;

namespace Features.FlightFieldModule.Scripts.Configuration {
    // The one startup check of the flight field configuration: routes for loop 0 on, every wall structure in the
    // catalog and a last wall structure that closes a single cell, so the wall has no holes; the release radius not
    // below the build radius.
    public sealed class FlightFieldConfigurationValidator : IInitializable {
        private const int SINGLE_CELL = 1;

        private readonly FlightFieldConfiguration _flightFieldConfiguration;
        private readonly FieldStructureCatalog _fieldStructureCatalog;
        private readonly IFieldFootprintReader _fieldFootprintReader;

        public FlightFieldConfigurationValidator(FlightFieldConfiguration flightFieldConfiguration, FieldStructureCatalog fieldStructureCatalog,
                                                 IFieldFootprintReader fieldFootprintReader) {
            _flightFieldConfiguration = flightFieldConfiguration;
            _fieldStructureCatalog = fieldStructureCatalog;
            _fieldFootprintReader = fieldFootprintReader;
        }

        public void Initialize() {
            ValidateRoutes();
            ValidateWall();
            ValidateStreaming();
        }

        private void ValidateStreaming() {
            if (_flightFieldConfiguration.ReleaseRadius < _flightFieldConfiguration.BuildRadius)
                throw new InvalidOperationException(
                    $"{_flightFieldConfiguration.name}: the release radius ({_flightFieldConfiguration.ReleaseRadius} m) is below the build radius ({_flightFieldConfiguration.BuildRadius} m).");
        }

        private void ValidateRoutes() {
            bool hasFirstLoop = false;
            foreach (FlightFieldConfiguration.LoopRoutes loopRoutes in _flightFieldConfiguration.RoutesByLoop) {
                if (loopRoutes.RouteDifficulties.Count == 0)
                    throw new InvalidOperationException($"{_flightFieldConfiguration.name}: routes from loop {loopRoutes.FromLoop} have no route.");

                hasFirstLoop |= loopRoutes.FromLoop == 0;
            }

            if (!hasFirstLoop)
                throw new InvalidOperationException($"{_flightFieldConfiguration.name}: no routes from loop 0.");
        }

        private void ValidateWall() {
            int count = _flightFieldConfiguration.WallStructures.Count;
            if (count == 0)
                throw new InvalidOperationException($"{_flightFieldConfiguration.name}: no wall structures.");

            foreach (FieldStructureDefinition structure in _flightFieldConfiguration.WallStructures) {
                if (structure == null || !_fieldStructureCatalog.TryGet(structure.Id, out _))
                    throw new InvalidOperationException($"{_flightFieldConfiguration.name}: a wall structure is empty or missing from {_fieldStructureCatalog.name}.");
            }

            FieldFootprint lastFootprint = _fieldFootprintReader.Read(_flightFieldConfiguration.WallStructures[count - 1], FieldRotation.None);
            if (lastFootprint.Cells.Count != SINGLE_CELL)
                throw new InvalidOperationException($"{_flightFieldConfiguration.name}: the last wall structure must take a single cell, or the wall gets holes.");
        }
    }
}
