using Features.FlightFieldModule.Scripts.Configuration;

namespace Features.FlightFieldModule.Scripts.Generation.Steps {
    // An obstacle field always has a passage: giants keep their gap cells from each other, every debris rock keeps the
    // ship gap from the giants and from the other rocks, and nothing closes the border, so the free plane between them
    // is one connected space with room for the ship everywhere. FieldSlalomTests check it on the laid-out rocks.
    public sealed class FieldObstaclePassageStep : IFieldGenerationStep {
        public bool IsUsedBy(FieldLayoutKind layout) =>
            layout == FieldLayoutKind.Obstacles;

        public void Execute(FieldGenerationContext context) =>
            context.HasPassage = true;
    }
}
