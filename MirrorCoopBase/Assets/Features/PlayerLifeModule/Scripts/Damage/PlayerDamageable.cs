using System;
using Features.NetworkModelModule.Scripts;
using Features.PlayerLifeModule.Scripts.Generated;
using Mirror;
using Zenject;

namespace Features.PlayerLifeModule.Scripts {
    // Health lives in the per-player PlayerLife model, so a rejoin with the same key gets it back.
    // OnDamaged / OnDied are raised on the server, where damage is applied; other peers read the model.
    public sealed class PlayerDamageable : PlayerLifeBridge, IDamageable {
        private PlayerDamageConfiguration _configuration;
        private IPlayerDamageRule _rule;

        public event Action<DamageInfo> OnDamaged;
        public event Action<DamageInfo> OnDied;

        public IReadOnlyPlayerLifeModel Life =>
            Bound;

        public PlayerKey Key =>
            new PlayerKey(PlayerKeyId);

        public float Health =>
            Bound.Health;

        public float MaxHealth =>
            Bound.MaxHealth;

        public bool IsDead =>
            _rule.IsDead(Bound.Health);

        [Inject]
        private void InjectDependencies(PlayerDamageConfiguration configuration, IPlayerDamageRule rule) {
            _configuration = configuration;
            _rule = rule;
        }

        public override void OnStartServer() {
            base.OnStartServer();
            // A new key has an empty record; a rejoin keeps the stored health.
            if (Bound.MaxHealth <= 0f)
                ServerRestore();
        }

        [Server]
        public void ServerApplyDamage(DamageInfo info) {
            if (_rule.TryApply(Bound.Health, info.Amount, out float nextHealth, out float appliedAmount) == false)
                return;

            ServerSetLastDamageType(info.Type);
            ServerSetHealth(nextHealth);
            DamageInfo applied = new DamageInfo(appliedAmount, info.Type, info.Source);
            OnDamaged?.Invoke(applied);
            if (_rule.IsDead(nextHealth))
                OnDied?.Invoke(applied);
        }

        [Server]
        public void ServerKill(DamageType type) =>
            ServerApplyDamage(new DamageInfo(Bound.Health, type));

        [Server]
        public void ServerRestore() {
            ServerSetMaxHealth(_configuration.MaxHealth);
            ServerSetHealth(_configuration.MaxHealth);
        }
    }
}
