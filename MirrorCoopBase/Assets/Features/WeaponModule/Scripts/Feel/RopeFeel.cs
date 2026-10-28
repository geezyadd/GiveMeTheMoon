using Features.CameraModule.Contracts;
using Features.WeaponModule.Scripts.Definitions;
using Features.WeaponModule.Scripts.Parts.Delivery;
using UnityEngine;
using Zenject;

namespace Features.WeaponModule.Scripts.Feel {
    // How a rope weapon in a hand (the harpoon) looks and feels on one peer, by its WeaponFeelProfile: every shot
    // throws a rope (a plain line) from the muzzle to where the shot ended and reels it back, with a recoil kick and,
    // for the player who holds it, a short view kick. A target the rope hooked draws its own rope while it is pulled.
    // The holder drives it every frame with Advance.
    public sealed class RopeFeel : MonoBehaviour, IWeaponFeelPresenter {
        private const int ROPE_POINT_COUNT = 2;

        [SerializeField] private Transform _muzzle;
        [SerializeField] private Transform _recoilRoot;
        [SerializeField] private LineRenderer _rope;

        private ICameraShakeService _cameraShakeService;

        private WeaponFeelProfile _profile;
        private RecoilKick _recoilKick;
        private RopeCast _ropeCast;
        private bool _isViewOwned;

        public Transform Muzzle => _muzzle;

        [Inject]
        private void InjectDependencies(ICameraShakeService cameraShakeService) =>
            _cameraShakeService = cameraShakeService;

        // The rope is drawn in the world: the line must not swing with the hand between the frames it is set.
        public void Bind(WeaponFeelProfile profile, IWeaponFeelFloor floor) {
            _profile = profile;
            _recoilKick = new RecoilKick(_recoilRoot, profile);
            _ropeCast = new RopeCast(profile);
            _rope.useWorldSpace = true;
            _rope.positionCount = ROPE_POINT_COUNT;
            _rope.enabled = false;
        }

        // Only the player who holds the weapon feels its kick.
        public void SetViewOwned(bool isViewOwned) =>
            _isViewOwned = isViewOwned;

        public void PresentShot(in WeaponShotOutcome outcome) {
            _recoilKick.Kick();
            _ropeCast.Throw(_muzzle.position, outcome.EndPoint, outcome.IsTargetHit);
            if (_isViewOwned)
                _cameraShakeService.AddShake(_profile.ShotShake, 1f);
        }

        public void Advance(float deltaTime) {
            _recoilKick.Advance(deltaTime);
            _ropeCast.Advance(deltaTime);
            DrawRope();
        }

        private void DrawRope() {
            _rope.enabled = _ropeCast.IsVisible;
            if (!_ropeCast.IsVisible)
                return;

            Vector3 origin = _muzzle.position;
            _rope.SetPosition(0, origin);
            _rope.SetPosition(1, _ropeCast.GetTip(origin));
        }
    }
}
