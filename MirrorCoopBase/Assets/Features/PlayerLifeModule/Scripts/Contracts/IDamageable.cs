using System;
namespace Features.PlayerLifeModule.Scripts {
    public interface IDamageable {
        float Health { get; }
        float MaxHealth { get; }
        bool IsDead { get; }
        event Action<DamageInfo> OnDamaged;
        event Action<DamageInfo> OnDied;
        void ServerApplyDamage(DamageInfo info);   // server only
        void ServerKill(DamageType type);         // server only, lethal damage
        void ServerRestore();                      // server only, back to full health, not dead
    }
}
