using UnityEngine;

namespace Features.WeaponModule.Scripts.Definitions {
    // The look and feel numbers of one weapon. Data only: the client-side IWeaponFeel of the weapon presents them.
    [CreateAssetMenu(menuName = "Game/Weapon Feel Profile", fileName = "WeaponFeelProfile")]
    public sealed class WeaponFeelProfile : ScriptableObject {
        [Header("Barrels")]
        [Tooltip("Barrel spin at full readiness; it follows the fire mode's spin-up and spin-down.")]
        [SerializeField, Min(0f)] private float _barrelSpinDegreesPerSecond;

        [Header("Muzzle flash")]
        [SerializeField] private Color _muzzleFlashColor = Color.white;
        [SerializeField, Min(0f)] private float _muzzleFlashSize;
        [Tooltip("Random share of the flash size and light that changes every frame of fire (0 = steady).")]
        [SerializeField, Range(0f, 1f)] private float _muzzleFlashFlicker;
        [SerializeField, Min(0f)] private float _muzzleFlashSeconds;
        [SerializeField, Min(0f)] private float _muzzleLightIntensity;
        [SerializeField, Min(0f)] private float _muzzleLightRange;

        [Header("Casings")]
        [SerializeField, Min(0)] private int _casingPoolSize;
        [SerializeField, Min(0f)] private float _casingLifetimeSeconds;
        [SerializeField, Min(0f)] private float _casingFadeSeconds;
        [Tooltip("Eject direction in the weapon's local space (right and a bit up for the minigun).")]
        [SerializeField] private Vector3 _casingEjectDirection;
        [SerializeField, Min(0f)] private float _casingEjectSpeed;
        [SerializeField, Range(0f, 1f)] private float _casingEjectSpeedVariation;
        [SerializeField, Min(0f)] private float _casingEjectSpreadDegrees;
        [SerializeField, Min(0f)] private float _casingSpinDegreesPerSecond;
        [Tooltip("Each casing spins at a random share of the spin speed between these two.")]
        [SerializeField, Min(0f)] private float _casingSpinMinShare;
        [SerializeField, Min(0f)] private float _casingSpinMaxShare;
        [Tooltip("Share of the vertical speed a casing keeps when it bounces off the floor.")]
        [SerializeField, Range(0f, 1f)] private float _casingBounciness;
        [Tooltip("Share of the sliding speed a casing keeps on every bounce.")]
        [SerializeField, Range(0f, 1f)] private float _casingFloorFriction;
        [Tooltip("Below this bounce speed a casing comes to rest on the floor.")]
        [SerializeField, Min(0f)] private float _casingRestSpeed;
        [Tooltip("Half the thickness of a casing: how high its middle rests above the floor.")]
        [SerializeField, Min(0f)] private float _casingRestHeight;
        [Tooltip("Random extra rest height, so resting casings lie on each other as a pile.")]
        [SerializeField, Min(0f)] private float _casingPileJitter;

        [Header("Recoil")]
        [SerializeField, Min(0f)] private float _recoilPositionNoiseMeters;
        [SerializeField, Min(0f)] private float _recoilRotationNoiseDegrees;
        [SerializeField, Min(0f)] private float _recoilNoiseFrequency;
        [Tooltip("Seconds the recoil takes to settle after the fire stops.")]
        [SerializeField, Min(0f)] private float _recoilSettleSeconds;

        [Header("Camera shake")]
        [Tooltip("Shake amplitudes are view turn noise in degrees.")]
        [SerializeField, Min(0f)] private float _fireShakeAmplitude;
        [SerializeField, Min(0f)] private float _nearImpactShakeAmplitude;
        [SerializeField, Min(0f)] private float _nearImpactDistanceMeters;
        [SerializeField, Min(0f)] private float _targetDestroyedShakeAmplitude;
        [SerializeField, Min(0f)] private float _shakeFrequency;
        [Tooltip("Seconds an impact shake takes to fade out.")]
        [SerializeField, Min(0f)] private float _shakeFadeSeconds;
        [Tooltip("Roll reads as a sea-sick tilt: its share of the shake on the other axes.")]
        [SerializeField, Range(0f, 1f)] private float _shakeRollShare;

        [Header("Tracers")]
        [Tooltip("Every Nth shot draws a tracer (3 = every third shot).")]
        [SerializeField, Min(1)] private int _tracerEveryNthShot = 1;
        [SerializeField] private Color _tracerColor = Color.white;
        [SerializeField, Min(0f)] private float _tracerSpeedMetersPerSecond;
        [SerializeField, Min(0f)] private float _tracerWidthMeters;
        [SerializeField, Min(0f)] private float _tracerLengthMeters;
        [Tooltip("Tracers in flight at once; the oldest is reused when they run out.")]
        [SerializeField, Min(0)] private int _tracerPoolSize;

        [Header("Impact")]
        [SerializeField, Min(0)] private int _impactSparkCount;
        [SerializeField, Min(0f)] private float _impactSparkSpeed;
        [SerializeField, Min(0)] private int _impactDebrisCount;
        [SerializeField, Min(0f)] private float _impactDebrisSpeed;
        [Tooltip("How far a spray direction may lean away from the surface normal.")]
        [SerializeField, Min(0f)] private float _impactSprayLean;
        [Tooltip("Slowest spark or chip, as a share of its speed.")]
        [SerializeField, Range(0f, 1f)] private float _impactMinSpeedShare;
        [SerializeField, Min(0f)] private float _impactDustDriftSpeed;
        [SerializeField, Min(0f)] private float _impactDustSizeMeters;
        [Tooltip("A short bright flash on the struck point, so a far hit still reads.")]
        [SerializeField, Min(0f)] private float _impactFlashSizeMeters;
        [SerializeField, Min(0f)] private float _impactFlashSeconds;
        [Tooltip("Smallest impact flash, as a share of its size.")]
        [SerializeField, Range(0f, 1f)] private float _impactFlashMinSizeShare;
        [SerializeField, Min(0f)] private float _targetFlashSeconds;

        [Header("Sound (an empty clip is a silent hook)")]
        [SerializeField] private AudioClip _fireLoopClip;
        [SerializeField] private AudioClip _fireTailClip;
        [SerializeField] private AudioClip _casingClip;
        [Tooltip("Share of the casings that clink when they land.")]
        [SerializeField, Range(0f, 1f)] private float _casingClinkShare;
        [SerializeField] private AudioClip _impactClip;
        [Tooltip("Every Nth impact plays the impact sound.")]
        [SerializeField, Min(1)] private int _impactSoundEveryNth = 1;

        public float BarrelSpinDegreesPerSecond => _barrelSpinDegreesPerSecond;
        public Color MuzzleFlashColor => _muzzleFlashColor;
        public float MuzzleFlashSize => _muzzleFlashSize;
        public float MuzzleFlashFlicker => _muzzleFlashFlicker;
        public float MuzzleFlashSeconds => _muzzleFlashSeconds;
        public float MuzzleLightIntensity => _muzzleLightIntensity;
        public float MuzzleLightRange => _muzzleLightRange;
        public int CasingPoolSize => _casingPoolSize;
        public float CasingLifetimeSeconds => _casingLifetimeSeconds;
        public float CasingFadeSeconds => _casingFadeSeconds;
        public Vector3 CasingEjectDirection => _casingEjectDirection;
        public float CasingEjectSpeed => _casingEjectSpeed;
        public float CasingEjectSpeedVariation => _casingEjectSpeedVariation;
        public float CasingEjectSpreadDegrees => _casingEjectSpreadDegrees;
        public float CasingSpinDegreesPerSecond => _casingSpinDegreesPerSecond;
        public float CasingSpinMinShare => _casingSpinMinShare;
        public float CasingSpinMaxShare => _casingSpinMaxShare;
        public float CasingBounciness => _casingBounciness;
        public float CasingFloorFriction => _casingFloorFriction;
        public float CasingRestSpeed => _casingRestSpeed;
        public float CasingRestHeight => _casingRestHeight;
        public float CasingPileJitter => _casingPileJitter;
        public float RecoilPositionNoiseMeters => _recoilPositionNoiseMeters;
        public float RecoilRotationNoiseDegrees => _recoilRotationNoiseDegrees;
        public float RecoilNoiseFrequency => _recoilNoiseFrequency;
        public float RecoilSettleSeconds => _recoilSettleSeconds;
        public float FireShakeAmplitude => _fireShakeAmplitude;
        public float NearImpactShakeAmplitude => _nearImpactShakeAmplitude;
        public float NearImpactDistanceMeters => _nearImpactDistanceMeters;
        public float TargetDestroyedShakeAmplitude => _targetDestroyedShakeAmplitude;
        public float ShakeFrequency => _shakeFrequency;
        public float ShakeFadeSeconds => _shakeFadeSeconds;
        public float ShakeRollShare => _shakeRollShare;
        public int TracerEveryNthShot => _tracerEveryNthShot;
        public Color TracerColor => _tracerColor;
        public float TracerSpeedMetersPerSecond => _tracerSpeedMetersPerSecond;
        public float TracerWidthMeters => _tracerWidthMeters;
        public float TracerLengthMeters => _tracerLengthMeters;
        public int TracerPoolSize => _tracerPoolSize;
        public int ImpactSparkCount => _impactSparkCount;
        public float ImpactSparkSpeed => _impactSparkSpeed;
        public int ImpactDebrisCount => _impactDebrisCount;
        public float ImpactDebrisSpeed => _impactDebrisSpeed;
        public float ImpactSprayLean => _impactSprayLean;
        public float ImpactMinSpeedShare => _impactMinSpeedShare;
        public float ImpactDustDriftSpeed => _impactDustDriftSpeed;
        public float ImpactDustSizeMeters => _impactDustSizeMeters;
        public float ImpactFlashSizeMeters => _impactFlashSizeMeters;
        public float ImpactFlashSeconds => _impactFlashSeconds;
        public float ImpactFlashMinSizeShare => _impactFlashMinSizeShare;
        public float TargetFlashSeconds => _targetFlashSeconds;
        public AudioClip FireLoopClip => _fireLoopClip;
        public AudioClip FireTailClip => _fireTailClip;
        public AudioClip CasingClip => _casingClip;
        public float CasingClinkShare => _casingClinkShare;
        public AudioClip ImpactClip => _impactClip;
        public int ImpactSoundEveryNth => _impactSoundEveryNth;
    }
}
