namespace Features.AttackModule.Contracts {
    // Where an attacker's attack presses come from on this peer: the owner's button for the player's own body, the
    // owner's packets for a copy of someone else's, an AI for a mob. A press is the count growing; a count that only
    // becomes readable (a copy starting, a body waking) is no press.
    public interface IAttackPresses {
        // Whether this peer reads the presses right now (the owner, or a copy simulated from the owner's packets).
        bool IsAvailable { get; }

        // Grows by one with every attack press (wraps at 255).
        byte AttackCount { get; }
    }
}
