namespace Features.FlightFieldModule.Scripts.Rocks {
    public enum FieldRockDamageResult {
        None = 0,
        Damaged = 1,
        Broken = 2,
        // No such rock in the current field, or it is broken already.
        NotFound = 3,
        // The reported point lies too far from the rock.
        TooFar = 4,
        Unbreakable = 5,
    }
}
