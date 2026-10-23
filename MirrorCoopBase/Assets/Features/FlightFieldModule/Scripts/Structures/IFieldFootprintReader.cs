using Features.FlightFieldModule.Scripts.Model;

namespace Features.FlightFieldModule.Scripts.Structures {
    public interface IFieldFootprintReader {
        public FieldFootprint Read(FieldStructureDefinition structure, FieldRotation rotation);
    }
}
