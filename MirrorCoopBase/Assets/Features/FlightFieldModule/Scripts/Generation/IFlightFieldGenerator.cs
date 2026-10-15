using Features.FlightFieldModule.Scripts.Model;

namespace Features.FlightFieldModule.Scripts.Generation {
    public interface IFlightFieldGenerator {
        public FlightField Generate(FieldRequest request);
    }
}
