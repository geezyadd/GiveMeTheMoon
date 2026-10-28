namespace Features.BugModule.Scripts.Brain {
    // What a bug is doing now on the server. Server only, never synced: renumbering is safe, but keep it explicit.
    public enum BugStage {
        None = 0,
        // Flying in to its own point on the ring round the ship.
        Approach = 1,
        // Flying at a cargo on board to grab it.
        SeekCargo = 2,
        // Dragging the cargo away from the ship.
        Carry = 3,
        // Flying at its gnaw point and landing on it.
        Land = 4,
        // Sitting on the ship and biting it.
        Gnaw = 5,
        // The ship no longer flies: flying off to be gone.
        Leave = 6,
    }
}
