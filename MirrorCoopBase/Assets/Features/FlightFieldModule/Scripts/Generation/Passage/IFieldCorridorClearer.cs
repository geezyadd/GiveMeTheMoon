using Features.FlightFieldModule.Scripts.Model;

namespace Features.FlightFieldModule.Scripts.Generation.Passage {
    public interface IFieldCorridorClearer {
        // Removes every placement with a cell within the radius (diagonals included) of the route centerline.
        public void Clear(FieldGenerationContext context, FieldRoute route, int radiusCells);
    }
}
