namespace Features.PointOfInterestModule.Scripts.Content {
    // The cutscene target id of a nest's flock, the same on every peer.
    public interface IBugNestFlockTargetIds {
        public string GetTargetId(int pointId);
        public bool TryGetPointId(string targetId, out int pointId);
    }
}
