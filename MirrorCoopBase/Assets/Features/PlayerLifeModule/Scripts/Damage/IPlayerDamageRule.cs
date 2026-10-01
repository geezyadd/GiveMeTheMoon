namespace Features.PlayerLifeModule.Scripts {
    public interface IPlayerDamageRule {
        bool IsDead(float health);
        bool TryApply(float health, float amount, out float nextHealth, out float appliedAmount);
    }
}
