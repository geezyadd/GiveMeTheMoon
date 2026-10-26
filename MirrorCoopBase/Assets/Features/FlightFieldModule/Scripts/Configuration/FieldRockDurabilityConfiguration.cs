using Features.FlightFieldModule.Scripts.Rocks;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Configuration {
    // How the rocks of the flight field break: which count as small (the rest and every rock of a recipe marked
    // always large count as large), how many hit points a breakable rock has, how a weapon hit on a rock looks and what
    // a break looks like. Whether a rock breaks at all is its recipe's flag.
    [CreateAssetMenu(menuName = "Game/Flight Field/Rock Durability Configuration", fileName = "FieldRockDurabilityConfiguration_Default")]
    public sealed class FieldRockDurabilityConfiguration : ScriptableObject {
        [Tooltip("A rock up to this diameter, meters, is small; a bigger one is large.")]
        [SerializeField, Min(0f)] private float _smallMaxDiameterMeters = 35f;
        [Tooltip("Played where a rock breaks, on every peer that has the rock built; authored for a rock of 1 m and scaled by the rock's diameter.")]
        [SerializeField] private FieldRockBreakEffect _breakEffect;
        [Tooltip("Hit points of a breakable rock by its diameter, meters (time = key / damage per second; the minigun deals 20 per second). Past the last key the last value holds.")]
        [SerializeField] private AnimationCurve _hitPointsByDiameter = new(new Keyframe(0f, 8f), new Keyframe(35f, 12f), new Keyframe(60f, 40f), new Keyframe(150f, 100f));
        [Tooltip("How far, meters, a reported hit may lie outside the rock's bounding sphere and still count (aim and network delay).")]
        [SerializeField, Min(0f)] private float _hitPointToleranceMeters = 3f;
        [Tooltip("How long a struck rock flashes and shakes after a hit, seconds.")]
        [SerializeField, Min(0.01f)] private float _hitFeedbackSeconds = 0.09f;
        [Tooltip("The emission a struck rock flashes with; it fades back over the feedback time.")]
        [SerializeField, ColorUsage(false, true)] private Color _hitFlashEmission = new(0.9f, 0.55f, 0.3f);
        [Tooltip("How far a struck rock shakes at the start of the feedback, as a share of its size.")]
        [SerializeField, Range(0f, 0.2f)] private float _hitShakeShare = 0.02f;

        public float SmallMaxDiameterMeters => _smallMaxDiameterMeters;
        public FieldRockBreakEffect BreakEffect => _breakEffect;
        public AnimationCurve HitPointsByDiameter => _hitPointsByDiameter;
        public float HitPointToleranceMeters => _hitPointToleranceMeters;
        public float HitFeedbackSeconds => _hitFeedbackSeconds;
        public Color HitFlashEmission => _hitFlashEmission;
        public float HitShakeShare => _hitShakeShare;
    }
}
