using System;
using UnityEngine;

namespace Features.PlayerLifeModule.Scripts {
    public sealed class PlayerHealth {
        public float Health { get; private set; }
        public float MaxHealth { get; }
        public bool IsDead { get; private set; }
        public float LastAmount { get; private set; }

        public event Action<DamageInfo> OnDamaged;
        public event Action<DamageInfo> OnDied;

        public PlayerHealth(float maxHealth) {
            if (maxHealth <= 0f)
                throw new ArgumentOutOfRangeException(nameof(maxHealth), maxHealth, null);

            MaxHealth = maxHealth;
            Health = maxHealth;
        }

        public bool TryApply(DamageInfo info) {
            if (IsDead)
                return false;

            float amount = info.Amount;
            if (amount < 0f)
                amount = 0f;

            LastAmount = amount;
            Health = Mathf.Max(0f, Health - amount);
            DamageInfo applied = new DamageInfo(amount, info.Type, info.Source);
            OnDamaged?.Invoke(applied);
            if (Health > 0f)
                return true;

            IsDead = true;
            OnDied?.Invoke(applied);
            return true;
        }

        public bool TryKill(DamageType type) {
            if (IsDead)
                return false;

            return TryApply(new DamageInfo(Health, type));
        }

        public void Restore() {
            Health = MaxHealth;
            IsDead = false;
            LastAmount = 0f;
        }
    }
}
