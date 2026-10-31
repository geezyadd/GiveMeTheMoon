namespace Features.PointOfInterestModule.Scripts.Content {
    public sealed class BugNestFlockTargetIds : IBugNestFlockTargetIds {
        private const string PREFIX = "nest-flock:";

        public string GetTargetId(int pointId) =>
            PrefixedPointId.Create(PREFIX, pointId);

        public bool TryGetPointId(string targetId, out int pointId) =>
            PrefixedPointId.TryParse(targetId, PREFIX, out pointId);
    }
}
