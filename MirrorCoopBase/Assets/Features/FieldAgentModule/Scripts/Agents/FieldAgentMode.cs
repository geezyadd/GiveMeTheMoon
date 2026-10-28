namespace Features.FieldAgentModule.Scripts.Agents {
    // How an agent moves now. Flight positions are in the world; every other mode keeps the agent in ship space, so
    // it stays on the deck while the ship flies and turns. Synced as a byte: never renumber, only append.
    public enum FieldAgentMode : byte {
        None = 0,
        Flight = 1,
        Landing = 2,
        Deck = 3,
        TakingOff = 4,
    }
}
