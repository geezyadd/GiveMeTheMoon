namespace Features.BehaviourTreeModule {
    // Inactive is the "none" value: a node that has not run yet or was interrupted.
    public enum BtStatus {
        Inactive = 0,
        Running = 1,
        Success = 2,
        Failure = 3
    }
}
