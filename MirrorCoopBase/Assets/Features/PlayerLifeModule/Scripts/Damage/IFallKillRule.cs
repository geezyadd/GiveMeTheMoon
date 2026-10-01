namespace Features.PlayerLifeModule.Scripts {
    public interface IFallKillRule {
        bool IsBelowKillHeight(float playerY, float floorY, bool isFlying);
    }
}
