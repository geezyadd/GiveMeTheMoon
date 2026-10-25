using UnityEngine;

namespace Features.ShipDamageModule.Scripts.Damage {
    // One thing an impact may damage: an installed module (socket id) or a hull damage point (point id), where it is,
    // whether it can take this damage now and its chance relative to the configured one.
    public readonly struct ShipDamageCandidate {
        public string Id { get; }
        public Vector3 Position { get; }
        public bool IsDamageable { get; }
        public float ChanceMultiplier { get; }

        public ShipDamageCandidate(string id, Vector3 position, bool isDamageable, float chanceMultiplier) {
            Id = id;
            Position = position;
            IsDamageable = isDamageable;
            ChanceMultiplier = chanceMultiplier;
        }
    }
}
