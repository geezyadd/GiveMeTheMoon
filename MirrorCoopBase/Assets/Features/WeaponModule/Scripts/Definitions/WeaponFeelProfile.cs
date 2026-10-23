using UnityEngine;

namespace Features.WeaponModule.Scripts.Definitions {
    // The look and feel numbers of one weapon. Data only: the client-side IWeaponFeel of the weapon presents them.
    [CreateAssetMenu(menuName = "Game/Weapon Feel Profile", fileName = "WeaponFeelProfile")]
    public sealed class WeaponFeelProfile : ScriptableObject {
        [Header("Muzzle flash")]
        [SerializeField] private Color _muzzleFlashColor = Color.white;
        [SerializeField, Min(0f)] private float _muzzleFlashSize;
        [SerializeField, Min(0f)] private float _muzzleLightIntensity;
        [SerializeField, Min(0f)] private float _muzzleLightRange;

        [Header("Casings")]
        [SerializeField, Min(0)] private int _casingPoolSize;
        [SerializeField, Min(0f)] private float _casingLifetimeSeconds;
        [SerializeField, Min(0f)] private float _casingFadeSeconds;
        [Tooltip("Eject direction in the weapon's local space (right and a bit up for the minigun).")]
        [SerializeField] private Vector3 _casingEjectDirection;
        [SerializeField, Min(0f)] private float _casingEjectSpeed;
        [SerializeField, Min(0f)] private float _casingSpinDegreesPerSecond;

        [Header("Recoil")]
        [SerializeField, Min(0f)] private float _recoilPositionNoiseMeters;
        [SerializeField, Min(0f)] private float _recoilRotationNoiseDegrees;
        [SerializeField, Min(0f)] private float _recoilNoiseFrequency;

        [Header("Camera shake")]
        [SerializeField, Min(0f)] private float _fireShakeAmplitude;
        [SerializeField, Min(0f)] private float _nearImpactShakeAmplitude;
        [SerializeField, Min(0f)] private float _nearImpactDistanceMeters;
        [SerializeField, Min(0f)] private float _targetDestroyedShakeAmplitude;

        [Header("Tracers")]
        [Tooltip("Every Nth shot draws a tracer (3 = every third shot).")]
        [SerializeField, Min(1)] private int _tracerEveryNthShot = 1;
        [SerializeField] private Color _tracerColor = Color.white;
        [SerializeField, Min(0f)] private float _tracerSpeedMetersPerSecond;
        [SerializeField, Min(0f)] private float _tracerWidthMeters;

        [Header("Impact")]
        [SerializeField, Min(0)] private int _impactSparkCount;
        [SerializeField, Min(0)] private int _impactDebrisCount;
        [SerializeField, Min(0f)] private float _impactDustSizeMeters;
        [SerializeField, Min(0f)] private float _targetFlashSeconds;

        public Color MuzzleFlashColor => _muzzleFlashColor;
        public float MuzzleFlashSize => _muzzleFlashSize;
        public float MuzzleLightIntensity => _muzzleLightIntensity;
        public float MuzzleLightRange => _muzzleLightRange;
        public int CasingPoolSize => _casingPoolSize;
        public float CasingLifetimeSeconds => _casingLifetimeSeconds;
        public float CasingFadeSeconds => _casingFadeSeconds;
        public Vector3 CasingEjectDirection => _casingEjectDirection;
        public float CasingEjectSpeed => _casingEjectSpeed;
        public float CasingSpinDegreesPerSecond => _casingSpinDegreesPerSecond;
        public float RecoilPositionNoiseMeters => _recoilPositionNoiseMeters;
        public float RecoilRotationNoiseDegrees => _recoilRotationNoiseDegrees;
        public float RecoilNoiseFrequency => _recoilNoiseFrequency;
        public float FireShakeAmplitude => _fireShakeAmplitude;
        public float NearImpactShakeAmplitude => _nearImpactShakeAmplitude;
        public float NearImpactDistanceMeters => _nearImpactDistanceMeters;
        public float TargetDestroyedShakeAmplitude => _targetDestroyedShakeAmplitude;
        public int TracerEveryNthShot => _tracerEveryNthShot;
        public Color TracerColor => _tracerColor;
        public float TracerSpeedMetersPerSecond => _tracerSpeedMetersPerSecond;
        public float TracerWidthMeters => _tracerWidthMeters;
        public int ImpactSparkCount => _impactSparkCount;
        public int ImpactDebrisCount => _impactDebrisCount;
        public float ImpactDustSizeMeters => _impactDustSizeMeters;
        public float TargetFlashSeconds => _targetFlashSeconds;
    }
}
