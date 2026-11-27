namespace Features.EmoteModule.Contracts {
    // Where a player's emote plays come from on this peer: the owner's emote wheel for the player's own body, the
    // owner's packets for a copy of someone else's. A play is the count growing; a count that only becomes readable (a
    // copy starting, a body waking) is no play.
    public interface IEmotePlays {
        // Whether this peer reads the plays right now (the owner, or a copy simulated from the owner's packets).
        bool IsAvailable { get; }

        // Grows by one with every emote played (wraps at 255).
        byte EmoteCount { get; }

        // The catalog index of the emote played last.
        byte EmoteIndex { get; }

        // Whether the emote plays right now: the owner's own state for its body, the owner's packet for a copy. A copy
        // stops its emote as soon as this is off, so it never keeps an emote the owner already stopped.
        bool IsEmoting { get; }
    }
}
