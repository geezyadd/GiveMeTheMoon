namespace Features.PlayerProfileModule.Scripts {
    // Whether this player sees the names over other players (a player setting). Read by every PlayerNameplate.
    public sealed class PlayerNameplateVisibilityModel {
        public bool AreShown { get; set; } = true;
    }
}
