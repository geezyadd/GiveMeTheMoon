namespace Features.BugModule.Scripts.Spawn {
    public interface IBugRandomSpawnRule {
        // One roll in [0, 1) of the random spawn in flight: whether a group comes.
        public bool IsGroupComing(bool isEnabled, float roll, bool hasCargoOnBoard);
        // A share in [0, 1) picks the group size between the configured smallest and biggest.
        public int CalculateGroupSize(float share);
    }
}
