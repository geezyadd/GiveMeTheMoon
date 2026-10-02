using System;

namespace Features.PlayerLifeModule.Scripts.Editor.Tests {
    // A player for the life tests: health and life state in memory, the death side effects counted.
    public sealed class FakePlayer : IPlayerLifeActor, IDamageable {
        private readonly PlayerDamageRule _rule = new PlayerDamageRule();

        public FakePlayer(PlayerLifeState lifeState = PlayerLifeState.Alive, float health = 100f) {
            LifeState = lifeState;
            Health = health;
        }

        public PlayerLifeState LifeState { get; private set; }
        public float Health { get; private set; }
        public float MaxHealth => 100f;
        public bool IsDead => _rule.IsDead(Health);
        public int DeathsApplied { get; private set; }
        public int Restores { get; private set; }

        public event Action<DamageInfo> OnDamaged;
        public event Action<DamageInfo> OnDied;

        public void ServerSetLifeState(PlayerLifeState state) =>
            LifeState = state;

        public void ServerApplyDeath() =>
            DeathsApplied++;

        public void ServerApplyDamage(DamageInfo info) {
            if (_rule.TryApply(Health, info.Amount, out float nextHealth, out float appliedAmount) == false)
                return;

            Health = nextHealth;
            DamageInfo applied = new DamageInfo(appliedAmount, info.Type, info.Source);
            OnDamaged?.Invoke(applied);
            if (_rule.IsDead(nextHealth))
                OnDied?.Invoke(applied);
        }

        public void ServerKill(DamageType type) =>
            ServerApplyDamage(new DamageInfo(Health, type));

        public void ServerRestore() {
            Restores++;
            Health = MaxHealth;
        }
    }
}
