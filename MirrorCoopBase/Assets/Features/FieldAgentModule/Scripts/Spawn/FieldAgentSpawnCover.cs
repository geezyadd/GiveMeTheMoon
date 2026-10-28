namespace Features.FieldAgentModule.Scripts.Spawn {
    // What hides a spawn point from the players. Never renumber, only append.
    public enum FieldAgentSpawnCover {
        None = 0,
        // A rock sphere stands between every player's view and the point.
        Rock = 1,
        // The point lies past the haze's hidden distance from every player (or behind a rock for some of them).
        Haze = 2,
    }
}
