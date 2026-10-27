using Features.CameraModule.Contracts;
using Features.WeaponModule.Scripts.Definitions;
using Features.WeaponModule.Scripts.Parts.Delivery;
using UnityEngine;
using Zenject;

namespace Features.WeaponModule.Scripts.Feel {
    // How a single-shot gun in a hand looks and feels on one peer, by its WeaponFeelProfile: a muzzle flash and a
    // recoil kick on every shot, a tracer to where it landed, an impact burst, the shot sound and, for the player who
    // holds it, a short view kick. Every effect comes from a pool made at Bind. The holder drives it every frame with
    // Advance.
    public sealed class HandGunFeel : MonoBehaviour, IWeaponFeelPresenter {
        private const string EFFECTS_ROOT_NAME = "WeaponFeelEffects";

        [SerializeField] private Transform _muzzle;
        [SerializeField] private Transform _recoilRoot;
        [SerializeField] private Transform _tracerTemplate;
        [SerializeField] private ParticleSystem _muzzleGlow;
        [SerializeField] private Light _muzzleLight;
        [SerializeField] private ParticleSystem _impactFlash;
        [SerializeField] private ParticleSystem _impactSparks;
        [SerializeField] private ParticleSystem _impactDebris;
        [SerializeField] private ParticleSystem _impactDust;
        [SerializeField] private AudioSource _loopAudio;
        [SerializeField] private AudioSource _oneShotAudio;

        private ICameraShakeService _cameraShakeService;

        private WeaponFeelProfile _profile;
        private Transform _effectsRoot;
        private MuzzleFlash _muzzleFlash;
        private RecoilKick _recoilKick;
        private TracerStream _tracers;
        private ImpactBursts _impacts;
        private WeaponFeelAudio _audio;
        private bool _isViewOwned;
        private bool _isFiring;
        private int _shots;

        public Transform Muzzle => _muzzle;

        [Inject]
        private void InjectDependencies(ICameraShakeService cameraShakeService) =>
            _cameraShakeService = cameraShakeService;

        private void OnDestroy() {
            if (_effectsRoot != null)
                Destroy(_effectsRoot.gameObject);
        }

        public void Bind(WeaponFeelProfile profile, IWeaponFeelFloor floor) {
            _profile = profile;
            _effectsRoot = new GameObject(EFFECTS_ROOT_NAME).transform;
            _effectsRoot.SetParent(floor.Frame, false);
            _tracerTemplate.gameObject.SetActive(false);
            _muzzleFlash = new MuzzleFlash(_muzzleGlow, _muzzleLight, profile);
            _recoilKick = new RecoilKick(_recoilRoot, profile);
            _tracers = new TracerStream(_effectsRoot, _tracerTemplate, floor.Frame, profile);
            _impacts = new ImpactBursts(_impactFlash, _impactSparks, _impactDebris, _impactDust, floor.Frame, profile);
            _audio = new WeaponFeelAudio(_loopAudio, _oneShotAudio, profile);
        }

        // Only the player who holds the gun feels its kick.
        public void SetViewOwned(bool isViewOwned) =>
            _isViewOwned = isViewOwned;

        public void PresentFiring(bool isFiring, float readiness) =>
            _isFiring = isFiring;

        public void PresentShot(in WeaponShotOutcome outcome) {
            _shots++;
            _recoilKick.Kick();
            _audio.PresentShot();
            if (_shots % _profile.TracerEveryNthShot == 0)
                _tracers.Launch(_muzzle.position, outcome.EndPoint);

            if (_isViewOwned)
                _cameraShakeService.AddShake(_profile.ShotShake, 1f);

            if (outcome.IsImpact)
                PresentImpact(outcome);
        }

        public void Advance(float deltaTime) {
            _muzzleFlash.Present(_isFiring);
            _recoilKick.Advance(deltaTime);
            _tracers.Advance(deltaTime);
            _audio.PresentFiring(_isFiring);
        }

        private void PresentImpact(in WeaponShotOutcome outcome) {
            _impacts.Emit(outcome.EndPoint, outcome.Normal);
            _audio.PresentImpact();
        }
    }
}
