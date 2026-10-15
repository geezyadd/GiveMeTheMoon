namespace Features.FlightFieldModule.Scripts.Generation.Routes {
    // The replaceable part of route generation: difficulty -> corridor width, waviness and obstacles inside the
    // corridor. Real difficulty logic and several routes change only this implementation and the data.
    public interface IFieldRouteShaper {
        public FieldCorridorShape Shape(float difficulty);
    }
}
