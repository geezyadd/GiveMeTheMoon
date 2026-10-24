namespace Features.FlightFieldModule.Scripts.Targets {
    public enum FieldTargetDamageResult {
        None = 0,
        Damaged = 1,
        Popped = 2,
        // No such balloon in the current field, or it is popped already.
        NotFound = 3,
        // The reported point lies too far from the balloon.
        TooFar = 4,
    }
}
