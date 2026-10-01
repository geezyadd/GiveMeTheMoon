namespace Features.PlayerLifeModule.Scripts {
    public sealed class FallKillRule : IFallKillRule {
        private readonly PlayerDamageConfiguration _playerDamageConfiguration;

        public FallKillRule(PlayerDamageConfiguration playerDamageConfiguration) =>
            _playerDamageConfiguration = playerDamageConfiguration;

        public bool IsBelowKillHeight(float playerY, float floorY, bool isFlying) {
            float depth = isFlying
                ? _playerDamageConfiguration.KillDepthBelowDeckInFlight
                : _playerDamageConfiguration.KillDepthBelowStation;
            return playerY < floorY - depth;
        }
    }
}
