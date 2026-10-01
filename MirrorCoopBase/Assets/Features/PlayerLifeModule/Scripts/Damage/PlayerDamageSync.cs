using System;

namespace Features.PlayerLifeModule.Scripts {
    [Serializable]
    public struct PlayerDamageSync {
        public float Health;
        public float MaxHealth;
        public bool IsDead;
        public DamageType LastType;
        public float LastAmount;
        public uint SourceNetId;
        public int Revision;
        public PlayerDamageSyncKind Kind;
    }
}
