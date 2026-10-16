using Features.FlightFieldModule.Scripts.Generation.Placement;
using Features.FlightFieldModule.Scripts.Model;

namespace Features.FlightFieldModule.Scripts.Generation.Steps {
    // Giants: the closed cells become placements. The generator decided only the class of a cell; which obstacle
    // closes it comes from the profile's options for the zone (wall or ridge).
    public sealed class FieldGiantPlacementStep : IFieldGenerationStep {
        private readonly IFieldGiantPacker _fieldGiantPacker;

        public FieldGiantPlacementStep(IFieldGiantPacker fieldGiantPacker) =>
            _fieldGiantPacker = fieldGiantPacker;

        public void Execute(FieldGenerationContext context) {
            _fieldGiantPacker.Pack(context, FieldCellClass.Wall, context.Profile.WallObstacles);
            _fieldGiantPacker.Pack(context, FieldCellClass.Ridge, context.Profile.RidgeObstacles);
        }
    }
}
