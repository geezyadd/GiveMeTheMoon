using Features.CameraModule.Contracts;
using Features.WeaponModule.Scripts.Definitions;
using Features.WeaponModule.Scripts.Parts.Delivery;
using UnityEngine;
using Zenject;

namespace Features.WeaponModule.Scripts.Feel {
    // How a barrel-spinning machine gun looks and feels on one peer, by its WeaponFeelProfile: barrel spin, a muzzle
    // flash per shot, a stream of casings, recoil jitter, the holder's camera shake, tracers and impact bursts. Every
    // effect comes from a pool made at Bind, so continuous fire allocates nothing. The holder drives it every frame with
    // Advance.
    public sealed class MinigunFeel : MonoBehaviour, IWeaponFeelPresenter {
        private const string EFFECTS_ROOT_NAME = "WeaponFeelEffects";

        [SerializeField] private Transform _barrels;
        [SerializeField] private Transform _muzzle;
        [SerializeField] private Transform _casingEjectPoint;
        [SerializeField] private Transform _recoilRoot;
        [SerializeField] private Transform _viewPoint;
        [SerializeField] private Transform _casingTemplate;
        [SerializeField] private Transform _tracerTemplate;
        [SerializeField] private ParticleSystem _muzzleGlow;
        [SerializeField] private Light _muzzleLight;
        [SerializeField] private ParticleSystem _impactFlash;
        [SerializeField] private ParticleSystem _impactSparks;
        [SerializeField] private ParticleSystem _impactDebris;
        [SerializeField] private ParticleSystem _impactDust;

        private ICameraShakeService _cameraShakeService;

        private WeaponFeelProfile _profile;
        private Transform _effectsRoot;
        private BarrelSpin _barrelSpin;
        private MuzzleFlash _muzzleFlash;
        private RecoilJitter _recoilJitter;
        private CasingStream _casings;
        private TracerStream _tracers;
        private ImpactBursts _impacts;
        private ICameraShakeHandle _fireShake;
        private bool _isFiring;
        private float _readiness;
        private int _shots;

        public Transform Muzzle => _muzzle;
        private bool IsViewOwned => _fireShake != null;

        [Inject]
        private void InjectDependencies(ICameraShakeService cameraShakeService) =>
            _cameraShakeService = cameraShakeService;

        private void OnDestroy() {
            StopFireShake();
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
            _casings = new CasingStream(_effectsRoot, _casingTemplate, floor, profile);
            _tracers = new TracerStream(_effectsRoot, _tracerTemplate, floor.Frame, profile);
            _impacts = new ImpactBursts(_impactFlash, _impactSparks, _impactDebris, _impactDust, floor.Frame, profile);
        }

        // Only the player who looks through the gun feels its shake.
        public void SetViewOwned(bool isViewOwned) {
            if (isViewOwned == false)
                StopFireShake();
            else if (IsViewOwned == false)
                _fireShake = _cameraShakeService.BeginShake(_profile.FireShake);
        }

        public void PresentFiring(bool isFiring, float readiness) {
            _isFiring = isFiring;
            _readiness = readiness;
        }

        public void PresentShot(in WeaponShotOutcome outcome) {
            _shots++;
            _muzzleFlash.Flash();
            _casings.Eject(_casingEjectPoint.position, CreateCasingVelocity());
            if (_shots % _profile.TracerEveryNthShot == 0)
                _tracers.Launch(_muzzle.position, outcome.EndPoint);

            if (outcome.IsImpact)
                PresentImpact(outcome);
        }

        public void Advance(float deltaTime) {
            _barrelSpin.Advance(_readiness, deltaTime);
            _muzzleFlash.Advance(deltaTime);
            _recoilJitter.Advance(_isFiring, deltaTime);
            if (IsViewOwned)
                _fireShake.SetIntensity(_isFiring ? 1f : 0f);

            _casings.Advance(deltaTime);
            _tracers.Advance(deltaTime);
        }

        private void PresentImpact(in WeaponShotOutcome outcome) {
            _impacts.Emit(outcome.EndPoint, outcome.Normal);
            float distance = Vector3.Distance(_viewPoint.position, outcome.EndPoint);
            if (IsViewOwned && distance < _profile.NearImpactDistanceMeters)
                _cameraShakeService.AddShake(_profile.NearImpactShake, 1f - distance / _profile.NearImpactDistanceMeters);
        }

        private void StopFireShake() {
            if (IsViewOwned == false)
                return;

            _fireShake.Stop();
            _fireShake = null;
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
