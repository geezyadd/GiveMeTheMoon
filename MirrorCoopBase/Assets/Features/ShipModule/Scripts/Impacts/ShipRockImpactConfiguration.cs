using UnityEngine;

namespace Features.ShipModule.Scripts.Impacts {
    [CreateAssetMenu(
        fileName = nameof(ShipRockImpactConfiguration) + "_Default",
        menuName = "Configurations/ShipModule/" + nameof(ShipRockImpactConfiguration))]
    public sealed class ShipRockImpactConfiguration : ScriptableObject {
        [Header("Small rock: damage")]
        [Tooltip("Diameter (m) of the smallest rock the damage scale starts at.")]
        [SerializeField] private float _smallDamageMinDiameter = 10f;
        [Tooltip("Diameter (m) of the biggest small rock the damage scale ends at.")]
        [SerializeField] private float _smallDamageMaxDiameter = 35f;
        [Tooltip("Hull damage (before armor) of a rock of the smallest diameter hit at the reference speed.")]
        [SerializeField] private float _smallDamageAtMinDiameter = 3f;
        [Tooltip("Hull damage (before armor) of a rock of the biggest diameter hit at the reference speed.")]
        [SerializeField] private float _smallDamageAtMaxDiameter = 12f;
        [Tooltip("Ship speed (m/s) at which a hit deals the damage above and shakes at full strength: the cruise speed.")]
        [SerializeField] private float _referenceSpeed = 14f;
        [Tooltip("The damage factor never drops below this, however slow the ship is.")]
        [SerializeField] private float _minSpeedFactor = 0.25f;
        [Tooltip("The damage factor never rises above this, however fast the ship is.")]
        [SerializeField] private float _maxSpeedFactor = 2f;

        [Header("Small rock: push")]
        [Tooltip("Push away from the rock, m/s.")]
        [SerializeField] private float _smallAwaySpeed = 1.5f;
        [Tooltip("Push back against the heading, as a share of the ship's forward speed.")]
        [SerializeField] private float _smallBackSpeedShare = 0.12f;
        [Tooltip("How long the push takes to fade out, seconds.")]
        [SerializeField] private float _smallPushSeconds = 0.6f;
        [Tooltip("The largest acceleration the push gives the ship (and everyone on the deck), m/s2.")]
        [SerializeField] private float _smallMaxAcceleration = 6f;

        [Header("Large rock: knockback")]
        [Tooltip("Hull damage (before armor) of a hit on a large rock. 0 = knockback only.")]
        [SerializeField] private float _largeRockDamage;
        [Tooltip("Knockback away from the rock's centre, horizontally, m/s.")]
        [SerializeField] private float _largeAwaySpeed = 5f;
        [Tooltip("Knockback against the heading, as a share of the ship's forward speed: above 1 the ship drifts back for a moment.")]
        [SerializeField] private float _largeBackSpeedShare = 1.3f;
        [Tooltip("How long the knockback takes to fade out, seconds; the cruise speed comes back over this time.")]
        [SerializeField] private float _largeKnockbackSeconds = 1.4f;
        [Tooltip("The largest acceleration of the knockback (and of everyone on the deck), m/s2. The takeoff's start peaks at about 27 (350 m forward and 100 m up in 9 s).")]
        [SerializeField] private float _largeMaxAcceleration = 22f;
        [Tooltip("While the hull still touches the rock it moves out of it at least this fast, m/s.")]
        [SerializeField] private float _contactPushOutSpeed = 3f;
        [Tooltip("A large rock counts as touched this far (m) before the hull reaches it.")]
        [SerializeField] private float _largeContactMarginMeters = 1f;
        [Tooltip("Share of a large rock's data radius the hull bounces off. The data radius holds the turned mesh with room to spare; 1 = the ship stops at the outer sphere, visibly before the rock.")]
        [SerializeField] private float _largeContactRadiusShare = 0.8f;
        [Tooltip("Share of the braking distance (speed^2 / 2 x max acceleration) a large rock is felt ahead of the hull along its motion, so the ship stops at the rock, not inside it.")]
        [SerializeField] private float _brakingLookAheadShare = 0.8f;
        [Tooltip("The look-ahead never reaches further than this, metres (a fast takeoff would feel rocks far away).")]
        [SerializeField] private float _maxLookAheadMeters = 16f;
        [Tooltip("After the contact with a large rock ended it cannot hit again for this long, seconds.")]
        [SerializeField] private float _largeHitCooldownSeconds = 1f;

        [Header("Check")]
        [Tooltip("A move of the ship longer than this (m) between two checks is a teleport or a world shift, not flight: it is not swept.")]
        [SerializeField] private float _maxSweepMeters = 60f;

        [Header("Feedback")]
        [SerializeField] private ShipRockImpactFeel _smallFeel = new();
        [SerializeField] private ShipRockImpactFeel _largeFeel = new();
        [Tooltip("The pooled effect played at the hit point on every peer; its audio source is the impact sound's hook.")]
        [SerializeField] private ShipImpactEffect _impactEffect;

        public float SmallDamageMinDiameter => _smallDamageMinDiameter;
        public float SmallDamageMaxDiameter => _smallDamageMaxDiameter;
        public float SmallDamageAtMinDiameter => _smallDamageAtMinDiameter;
        public float SmallDamageAtMaxDiameter => _smallDamageAtMaxDiameter;
        public float ReferenceSpeed => _referenceSpeed;
        public float MinSpeedFactor => _minSpeedFactor;
        public float MaxSpeedFactor => _maxSpeedFactor;
        public float SmallAwaySpeed => _smallAwaySpeed;
        public float SmallBackSpeedShare => _smallBackSpeedShare;
        public float SmallPushSeconds => _smallPushSeconds;
        public float SmallMaxAcceleration => _smallMaxAcceleration;
        public float LargeRockDamage => _largeRockDamage;
        public float LargeAwaySpeed => _largeAwaySpeed;
        public float LargeBackSpeedShare => _largeBackSpeedShare;
        public float LargeKnockbackSeconds => _largeKnockbackSeconds;
        public float LargeMaxAcceleration => _largeMaxAcceleration;
        public float ContactPushOutSpeed => _contactPushOutSpeed;
        public float LargeContactMarginMeters => _largeContactMarginMeters;
        public float LargeContactRadiusShare => _largeContactRadiusShare;
        public float BrakingLookAheadShare => _brakingLookAheadShare;
        public float MaxLookAheadMeters => _maxLookAheadMeters;
        public float LargeHitCooldownSeconds => _largeHitCooldownSeconds;
        public float MaxSweepMeters => _maxSweepMeters;
        public ShipRockImpactFeel SmallFeel => _smallFeel;
        public ShipRockImpactFeel LargeFeel => _largeFeel;
        public ShipImpactEffect ImpactEffect => _impactEffect;
    }
}
