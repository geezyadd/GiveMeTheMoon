using Mirror;
namespace Features.PlayerLifeModule.Scripts {
    public readonly struct DamageInfo {
        public readonly float Amount;
        public readonly DamageType Type;
        public readonly NetworkIdentity Source; // may be null
        public DamageInfo(float amount, DamageType type, NetworkIdentity source = null) {
            Amount = amount; Type = type; Source = source;
        }
    }
}
