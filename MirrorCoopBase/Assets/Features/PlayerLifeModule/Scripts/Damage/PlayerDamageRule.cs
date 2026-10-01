using UnityEngine;

namespace Features.PlayerLifeModule.Scripts {
    public sealed class PlayerDamageRule : IPlayerDamageRule {
        public bool IsDead(float health) =>
            health <= 0f;

        public bool TryApply(float health, float amount, out float nextHealth, out float appliedAmount) {
            nextHealth = health;
            appliedAmount = 0f;
            if (IsDead(health))
                return false;

            appliedAmount = Mathf.Max(0f, amount);
            nextHealth = Mathf.Max(0f, health - appliedAmount);
            return true;
        }
    }
}
