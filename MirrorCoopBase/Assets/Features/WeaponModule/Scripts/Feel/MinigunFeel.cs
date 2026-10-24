using Features.WeaponModule.Scripts.Definitions;
using Features.WeaponModule.Scripts.Parts.Delivery;
using Features.WeaponModule.Scripts.Parts.Feel;
using UnityEngine;

namespace Features.WeaponModule.Scripts.Feel {
    // How a barrel-spinning machine gun looks and feels on one peer, by its WeaponFeelProfile: barrel spin, muzzle
    // glow, a stream of casings, recoil jitter, view shake, tracers, impact bursts and sound hooks. Every effect comes
    // from a pool made at Bind, so continuous fire allocates nothing. The holder drives it every frame with Advance.
    public sealed class MinigunFeel : MonoBehaviour, IWeaponFeel {
        private const int TRACER_POOL_SIZE = 16;
        private const string EFFECTS_ROOT_NAME = "WeaponFeelEffects";

        [SerializeField] private Transform _barrels;
        [SerializeField] private Transform _muzzle;
        [SerializeField] private Transform _casingEjectPoint;
        [SerializeField] private Transform _recoilRoot;
        [SerializeField] private Transform _viewShakeRoot;
        [SerializeField] private Transform _casingTemplate;
        [SerializeField] private Transform _tracerTemplate;
        [SerializeField] private ParticleSystem _muzzleGlow;
        [SerializeField] private Light _muzzleLight;
        [SerializeField] private ParticleSystem _impactFlash;
        [SerializeField] private ParticleSystem _impactSparks;
        [SerializeField] private ParticleSystem _impactDebris;
        [SerializeField] private ParticleSystem _impactDust;
        [SerializeField] private AudioSource _loopAudio;
        [SerializeField] private AudioSource _oneShotAudio;

        private WeaponFeelProfile _profile;
        private Transform _effectsRoot;
        private BarrelSpin _barrelSpin;
        private MuzzleFlash _muzzleFlash;
        private RecoilJitter _recoilJitter;
        private ViewShake _viewShake;
        private CasingStream _casings;
        private TracerStream _tracers;
        private ImpactBursts _impacts;
        private WeaponFeelAudio _audio;
        private bool _isFiring;
        private float _readiness;
        private bool _isViewOwned;
        private int _shots;

        public Transform Muzzle => _muzzle;

        private void OnDestroy() {
            if (_effectsRoot != null)
                Destroy(_effectsRoot.gameObject);
        }

        public void Bind(WeaponFeelProfile profile, IWeaponFeelFloor floor) {
            _profile = profile;
            _effectsRoot = new GameObject(EFFECTS_ROOT_NAME).transform;
            _effectsRoot.SetParent(floor.Frame, false);
            _casingTemplate.gameObject.SetActive(false);
            _tracerTemplate.gameObject.SetActive(false);
            _barrelSpin = new BarrelSpin(_barrels, profile.BarrelSpinDegreesPerSecond);
            _muzzleFlash = new MuzzleFlash(_muzzleGlow, _muzzleLight, profile);
            _recoilJitter = new RecoilJitter(_recoilRoot, profile);
            _viewShake = new ViewShake(_viewShakeRoot, profile);
            _casings = new CasingStream(_effectsRoot, _casingTemplate, floor, profile);
            _tracers = new TracerStream(_effectsRoot, _tracerTemplate, floor.Frame, profile, TRACER_POOL_SIZE);
            _impacts = new ImpactBursts(_impactFlash, _impactSparks, _impactDebris, _impactDust, floor.Frame, profile);
            _audio = new WeaponFeelAudio(_loopAudio, _oneShotAudio, profile);
        }

        public void SetViewOwned(bool isViewOwned) =>
            _isViewOwned = isViewOwned;

        public void PresentFiring(bool isFiring, float readiness) {
            _isFiring = isFiring;
            _readiness = readiness;
        }

        public void PresentShot(in WeaponShotOutcome outcome) {
            _shots++;
            _casings.Eject(_casingEjectPoint.position, CreateCasingVelocity());
            if (_shots % _profile.TracerEveryNthShot == 0)
                _tracers.Launch(_muzzle.position, outcome.EndPoint);

            if (outcome.IsImpact)
                PresentImpact(outcome);
        }

        public void Advance(float deltaTime) {
            _barrelSpin.Advance(_readiness, deltaTime);
            _muzzleFlash.Present(_isFiring);
            _recoilJitter.Advance(_isFiring, deltaTime);
            _viewShake.Advance(_isFiring, _isViewOwned, deltaTime);
            _casings.Advance(deltaTime);
            _tracers.Advance(deltaTime);
            _audio.PresentFiring(_isFiring);
            _audio.PresentCasingsLanded(_casings.LandedThisFrame);
        }

        private void PresentImpact(in WeaponShotOutcome outcome) {
            _impacts.Emit(outcome.EndPoint, outcome.Normal);
            _audio.PresentImpact();
            float distance = Vector3.Distance(_viewShakeRoot.position, outcome.EndPoint);
            if (distance < _profile.NearImpactDistanceMeters)
                _viewShake.AddImpulse(_profile.NearImpactShakeAmplitude * (1f - distance / _profile.NearImpactDistanceMeters));
        }

        private Vector3 CreateCasingVelocity() {
            float spread = _profile.CasingEjectSpreadDegrees;
            Quaternion scatter = Quaternion.Euler(Random.Range(-spread, spread), Random.Range(-spread, spread), 0f);
            Vector3 direction = _casingEjectPoint.TransformDirection(scatter * _profile.CasingEjectDirection.normalized);
            float variation = _profile.CasingEjectSpeedVariation;
            return direction * (_profile.CasingEjectSpeed * Random.Range(1f - variation, 1f + variation));
        }
    }
}
