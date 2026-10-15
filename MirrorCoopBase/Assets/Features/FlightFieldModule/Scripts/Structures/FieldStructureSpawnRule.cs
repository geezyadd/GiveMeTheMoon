namespace Features.FlightFieldModule.Scripts.Structures {
    // The one place that decides how a structure gets into the world, by its definition's flag: a local static object
    // every peer builds from the placement list, or a Mirror object the server spawns for everyone.
    public sealed class FieldStructureSpawnRule : IFieldStructureSpawnRule {
        public bool IsBuiltLocally(FieldStructureDefinition structure) =>
            !structure.IsNetworked;
    }
}
