using System;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.PlayerLifeModule.Scripts {
    public sealed class PlayerDamageable : NetworkBehaviour, IDamageable {
        private const uint NO_SOURCE_NET_ID = 0;
        private const int UNPUBLISHED_REVISION = -1;

        private PlayerDamageConfiguration _configuration;
        private PlayerDamageableRegistry _registry;
        private PlayerHealth _health;
        private int _publishedRevision = UNPUBLISHED_REVISION;
        private int _diedNotificationCount;

        [SyncVar(hook = nameof(OnDamageSyncChanged))]
        private PlayerDamageSync _sync;

        public event Action<DamageInfo> OnDamaged;
        public event Action<DamageInfo> OnDied;

        public float Health =>
            _sync.Health;

        public float MaxHealth =>
            _sync.MaxHealth;

        public bool IsDead =>
            _sync.IsDead;

        public int DiedNotificationCount =>
            _diedNotificationCount;

        [Inject]
        private void InjectDependencies(PlayerDamageConfiguration configuration, PlayerDamageableRegistry registry) {
            _configuration = configuration;
            _registry = registry;
        }

        public override void OnStartServer() {
            _health = new PlayerHealth(_configuration.MaxHealth);
            Commit(Capture(PlayerDamageSyncKind.None, default));
            _registry.Register(this);
        }

        public override void OnStopServer() {
            _registry.Unregister(this);
        }

        public void ServerApplyDamage(DamageInfo info) {
            if (NetworkServer.active == false || _health == null)
                return;

            if (_health.TryApply(info) == false)
                return;

            PlayerDamageSyncKind kind = _health.IsDead
                ? PlayerDamageSyncKind.Died
                : PlayerDamageSyncKind.Damaged;
            Commit(Capture(kind, info));
        }

        public void ServerKill(DamageType type) {
            if (NetworkServer.active == false || _health == null || _health.IsDead)
                return;

            ServerApplyDamage(new DamageInfo(_health.Health, type));
        }

        public void ServerRestore() {
            if (NetworkServer.active == false || _health == null)
                return;

            if (_health.IsDead == false && Mathf.Approximately(_health.Health, _health.MaxHealth))
                return;

            _health.Restore();
            Commit(Capture(PlayerDamageSyncKind.Restored, default));
        }

        private void Commit(PlayerDamageSync next) {
            _sync = next;
            if (isServer && isClient == false)
                Publish(next);
        }

        private PlayerDamageSync Capture(PlayerDamageSyncKind kind, DamageInfo info) {
            uint sourceNetId = NO_SOURCE_NET_ID;
            if (info.Source != null)
                sourceNetId = info.Source.netId;

            return new PlayerDamageSync {
                Health = _health.Health,
                MaxHealth = _health.MaxHealth,
                IsDead = _health.IsDead,
                LastType = info.Type,
                LastAmount = _health.LastAmount,
                SourceNetId = sourceNetId,
                Revision = _sync.Revision + 1,
                Kind = kind
            };
        }

        private void OnDamageSyncChanged(PlayerDamageSync previous, PlayerDamageSync current) =>
            Publish(current);

        private void Publish(PlayerDamageSync sync) {
            if (sync.Revision == _publishedRevision)
                return;

            _publishedRevision = sync.Revision;
            if (sync.Kind != PlayerDamageSyncKind.Damaged && sync.Kind != PlayerDamageSyncKind.Died)
                return;

            DamageInfo info = new DamageInfo(sync.LastAmount, sync.LastType, ResolveSource(sync.SourceNetId));
            OnDamaged?.Invoke(info);
            if (sync.Kind != PlayerDamageSyncKind.Died)
                return;

            _diedNotificationCount++;
            Debug.Log(
                "[PlayerDamageable] OnDied type=" + info.Type
                + " health=" + sync.Health
                + " isServer=" + isServer
                + " isClient=" + isClient
                + " count=" + _diedNotificationCount);
            OnDied?.Invoke(info);
        }

        private static NetworkIdentity ResolveSource(uint netId) {
            if (netId == NO_SOURCE_NET_ID)
                return null;

            if (NetworkServer.active && NetworkServer.spawned.TryGetValue(netId, out NetworkIdentity serverIdentity))
                return serverIdentity;

            if (NetworkClient.active && NetworkClient.spawned.TryGetValue(netId, out NetworkIdentity clientIdentity))
                return clientIdentity;

            return null;
        }
    }
}
