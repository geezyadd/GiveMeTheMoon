using UnityEngine;

namespace Features.WeaponModule.Scripts.Definitions {
    // How much slack the server gives a reported hit for network jitter and frame timing.
    [CreateAssetMenu(menuName = "Game/Weapon Hit Rule Configuration", fileName = "WeaponHitRuleConfiguration_Default")]
    public sealed class WeaponHitRuleConfiguration : ScriptableObject {
        [Tooltip("Share above the weapon's hit rate the server still accepts over time (0.25 = 25 % faster).")]
        [SerializeField, Min(0f)] private float _fireRateTolerance;
        [Tooltip("Seconds of hits at the tolerated rate that may arrive at once, e.g. a batch after a network hitch.")]
        [SerializeField, Min(0f)] private float _burstSeconds;
        [Tooltip("Meters a hit may lie beyond the weapon's range, measured from the shooter's server position.")]
        [SerializeField, Min(0f)] private float _rangeToleranceMeters;

        public float FireRateTolerance => _fireRateTolerance;
        public float BurstSeconds => _burstSeconds;
        public float RangeToleranceMeters => _rangeToleranceMeters;
    }
}
